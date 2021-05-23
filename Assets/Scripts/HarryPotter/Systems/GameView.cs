using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using HarryPotter.Data;
using HarryPotter.Data.Cards;
using HarryPotter.Enums;
using HarryPotter.GameActions;
using HarryPotter.GameActions.GameFlow;
using HarryPotter.Input.Controllers;
using HarryPotter.Systems.Core;
using HarryPotter.Utils;
using HarryPotter.Views;
using HarryPotter.Views.UI.Cursor;
using HarryPotter.Views.UI.ParticleSystemUtils;
using HarryPotter.Views.UI.Tooltips;
using UnityEngine;

namespace HarryPotter.Systems
{
    [RequireComponent(typeof(BoardView))]
    [RequireComponent(typeof(HandView))]
    public class GameView : MonoBehaviour, IGameSystem
    {
        public MatchData Match;
        public GameSettings Settings;
        public CardView CardPrefab;
        
        // TODO: Store this parameter into a ScriptableObject so it can be configured by the user when we build the options menu
        public float TweenTimescale = 4f;
        
        private ParticleSystemController _particlesController;
        private ActionSystem _actionSystem;
        
        private Dictionary<(int PlayerIndex, Zones Zone), ZoneView> _zoneViews;
        
        //NOTE: We may want to use a different kind of input system in the future, extract interface?
        public InputSystem Input { get; private set; }

        private IContainer _container;
        public IContainer Container
        {
            get
            {
                if (_container == null)
                {
                    _container = GameFactory.Create(Match, Settings);
                    _container.AddSystem(this);
                }

                return _container;
            }
            
            set => _container = value;
        }
        
        public bool IsIdle => !_actionSystem.IsActive && !Container.IsGameOver();

        private void Awake()
        {
            DOTween.Init().SetCapacity(50, 10);
            DOTween.timeScale = TweenTimescale;
            
            Input = GetComponent<InputSystem>();

            _zoneViews = GetComponentsInChildren<ZoneView>()
                .GroupBy(z => (z.Owner.Index, z.Zone))
                .ToDictionary(g => g.Key, g => g.Single());

            _particlesController = GetComponentInChildren<ParticleSystemController>();

            _actionSystem = Container.GetSystem<ActionSystem>();
            
            if (Match == null || Input == null || _particlesController == null)
            {
                Debug.LogError("ERROR: GameView is missing some dependencies!");
                return;
            }
            
            Container.Awake();
        }

        private void Start()
        {
            SetupSinglePlayer();
        }
        
        private void SetupSinglePlayer() 
        {
            Debug.Log("*** BEGIN GAME ***");
            
            var beginGame = new BeginGameAction();
            _container.Perform(beginGame);
        }

        private void Update()
        {
            _actionSystem.Update();
        }

        private void OnDestroy()
        {
            Container.Destroy();
        }
        
        public ZoneView FindZoneView(Player player, Zones zone) => _zoneViews[(player.Index, zone)];
        
        // TODO: This is called a lot, possible to optimize?
        public CardView FindCardView(Card card) => _zoneViews.Values
                                                        .Where(z => z.Owner == card.Owner)
                                                        .SelectMany(z => z.Cards)
                                                        .Single(cv => cv.Card == card);
        
        public List<CardView> FindCardViews(List<Card> cards) => _zoneViews.Values
                                                                    .SelectMany(z => z.Cards)
                                                                    .Where(cv => cards.Contains(cv.Card))
                                                                    .ToList();

        public Sequence GetParticleSequence(GameAction action, Card target)
        {
            return GetParticleSequence(action, new List<Card> {target});
        }
        
        public Sequence GetParticleSequence(GameAction action, List<Card> targets)
        {
            var particleSequence = DOTween.Sequence();

            if (action.SourceCard == null)
            {
                Debug.LogWarning("Called GetParticleSequence with action.SourceCard == null");
            }
            
            foreach (var target in targets)
            {
                // NOTE: This check needs to be done so that spell cards that discard themselves don't do the particle animation
                if (target == action.SourceCard)
                {
                    continue;
                }
                
                var particleType = action.SourceCard?.GetLessonType() ?? LessonType.None;
                
                var sequence = action.SourceCard?.Data.Type.IsHorizontal() == true
                    ? GetParticleSequence(action.SourceCard, target)
                    : GetParticleSequence(action.Player, target, particleType);

                particleSequence.Append(sequence);
            }
            
            return particleSequence;
        }
        
        
        private Sequence GetParticleSequence(Player source, Card target, LessonType particleColorType)
        {
            var targetView = FindCardView(target);
            
            var startPosLocal = new Vector3(0f, -18.5f, 50f); // For targeting enemy
            var startPosEnemy = new Vector3(0f, 21.5f, 50f); // For targeting local

            var startPos = source == Match.LocalPlayer
                ? startPosLocal
                : startPosEnemy;

            var targetPos = targetView.transform.position + 0.5f * Vector3.back;

            var endPosDiscard = new Vector3(67f, 13f, 0f);

            if (target.Zone == Zones.Discard)
            {
                targetPos = endPosDiscard;
            }

            return GetParticleSequence(startPos, targetPos, particleColorType);
        }
        
        private Sequence GetParticleSequence(Card source, Card target)
        {
            var sourceView = FindCardView(source);
            var targetView = FindCardView(target);

            
            var startPos  = sourceView.transform.position + 0.5f * Vector3.back;
            var targetPos = targetView.transform.position + 0.5f * Vector3.back;

            var endPosDiscard = new Vector3(-34f, -11f, 80f);
            
            // TODO: endPosDeck when targeting deck
            // TODO: What happens when targeting enemy hand/discard?

            if (target.Zone == Zones.Discard)
            {
                targetPos = endPosDiscard;
            }
            
            var particleColorType = sourceView.Card.GetLessonType();

            return GetParticleSequence(startPos, targetPos, particleColorType);

        }

        private Sequence GetParticleSequence(Vector3 startPos, Vector3 endPos, LessonType particleColorType)
        {
            _particlesController.SetParticleColor(particleColorType);

            return DOTween.Sequence()
                .AppendCallback(() => _particlesController.Play())
                .Append(_particlesController.transform.DOMove(startPos, 0f))
                .Append(_particlesController.transform.DOMove(endPos, 1.5f).SetEase(Ease.OutQuint))
                .AppendCallback(() => _particlesController.Stop());
        }
        
        public Sequence GetMoveToZoneSequence(CardView cardView, Zones to, Zones from)
        {
            var pairs = new List<(CardView, Zones)>
            {
                (cardView, to)
            };
            
            return GetMoveToZoneSequence(pairs, from);
        }

        public Sequence GetMoveToZoneSequence(List<(CardView, Zones)> cardViewPairs, Zones from)
        {
            var affectedZones = new HashSet<ZoneView>();
            
            foreach (var (card, zone) in cardViewPairs)
            {
                if (zone == Zones.None)
                {
                    break;
                }
                
                var affected = ChangeZoneView(card, zone, from);

                foreach (var zoneView in affected)
                {
                    affectedZones.Add(zoneView);
                }
            }
            
            // TODO: We might not want to rely on GetZoneLayoutSequence to move cards between zones.
            //       It makes it difficult to do more custom animations from one zone to the other.
            var sequence = DOTween.Sequence();
            
            foreach (var zoneView in affectedZones)
            {
                sequence = sequence.Join(zoneView.GetZoneLayoutSequence());
            }

            return sequence;
        }

        public List<ZoneView> ChangeZoneView(CardView card, Zones to, Zones from)
        {
            var result = new List<ZoneView>();
            var actualFrom = from != Zones.None ? from : card.Card.Zone;

            if (actualFrom != Zones.None)
            {
                var fromZone = FindZoneView(card.Card.Owner, actualFrom);
                if (!fromZone.Cards.Remove(card))
                {
                    Debug.LogWarning($"{card.Card.Data.CardName} was not removed from zone {actualFrom}");
                    Debug.Break();
                }
                result.Add(fromZone);
            }

            var toZone = FindZoneView(card.Card.Owner, to);
            if (!toZone.Cards.Contains(card))
            {
                toZone.Cards.Add(card);
            }
            
            result.Add(toZone);
            card.transform.SetParent(toZone.transform);
            
            return result;
        }
    }
}