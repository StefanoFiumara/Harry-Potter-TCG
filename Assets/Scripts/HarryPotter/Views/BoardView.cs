using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using HarryPotter.Data.Cards.CardAttributes;
using HarryPotter.Enums;
using HarryPotter.GameActions;
using HarryPotter.GameActions.Actions;
using HarryPotter.Systems;
using HarryPotter.Systems.Core;
using HarryPotter.Utils;
using UnityEngine;

namespace HarryPotter.Views
{
    public class BoardView : MonoBehaviour
    {
        private GameViewSystem _gameView;

        private void Awake()
        {
            
            Global.Events.Subscribe(Notification.Prepare<PlayToBoardAction>(), OnPreparePlayToBoard);
            Global.Events.Subscribe(Notification.Prepare<DamagePlayerAction>(), OnPrepareDamagePlayer);
            Global.Events.Subscribe(Notification.Prepare<DiscardAction>(), OnPrepareDiscard);
            Global.Events.Subscribe(Notification.Prepare<DamageCreatureAction>(), OnPrepareDamageCreature);
            Global.Events.Subscribe(Notification.Prepare<ShuffleDeckAction>(), OnPrepareShuffleDeck);
            
            _gameView = GetComponent<GameViewSystem>();

            if (_gameView == null)
            {
                Debug.LogError("BoardView could not find GameView");
            }
        }

        private void OnPreparePlayToBoard(object sender, object args)
        {
            var action = (PlayToBoardAction) args;
            action.PerformPhase.Viewer = PlayToBoardAnimation;
        }

        private void OnPrepareDamagePlayer(object sender, object args)
        {
            var action = (DamagePlayerAction) args;
            action.PerformPhase.Viewer = DamagePlayerAnimation;
        }

        private void OnPrepareDiscard(object sender, object args)
        {
            var action = (DiscardAction) args;
            action.PerformPhase.Viewer  = DiscardAnimation;
        }
        
        private void OnPrepareDamageCreature(object sender, object args)
        {
            var action = (DamageCreatureAction) args;
            action.PerformPhase.Viewer  = DamageCreatureAnimation;
        }


        private void OnPrepareShuffleDeck(object sender, object args)
        {
            var action = (ShuffleDeckAction) args;
            action.PerformPhase.Viewer  = ShuffleDeckAnimation;
        }

        private IEnumerator ShuffleDeckAnimation(IContainer container, GameAction action)
        {
            yield return true;
            
            var shuffleAction = (ShuffleDeckAction) action;
            var sequence = DOTween.Sequence().SetEase(Ease.InSine);
            
            foreach (var target in shuffleAction.Targets)
            {
                var zoneView = _gameView.FindZoneView(target, Zones.Deck);
                var cardViews = _gameView.FindCardViews(target.Deck);
            
                var startDelay = 0f;
                var targetRot = zoneView.GetRotation();
                foreach (var cardView in cardViews)
                {
                    var offsetX = Random.Range(0f, 1f) < 0.5f ? 3f : -3f;
                    var offsetPos = cardView.transform.position + offsetX * Vector3.right;
                    sequence.Join(cardView.Move(offsetPos, targetRot, 0.3f, startDelay));
                    startDelay += 0.05f;
                }
            }
            
            while (sequence.IsPlaying())
            {
                yield return null;
            }

            sequence = DOTween.Sequence().SetEase(Ease.InSine);
            
            foreach (var target in shuffleAction.Targets)
            {
                var zoneView = _gameView.FindZoneView(target, Zones.Deck);
                var targetRot = zoneView.GetRotation();

                var startDelay = 0f;
                for (var i = 0; i < target.Deck.Count; i++)
                {
                    var card = target.Deck[i];
                    var cardView = _gameView.FindCardView(card);
                    
                    
                    var finalPos = zoneView.GetPosition(i);

                    sequence.Join(cardView.Move(finalPos, targetRot, 0.3f, startDelay));
                    startDelay += 0.05f;
                }
            }

            while (sequence.IsPlaying())
            {
                yield return null;
            } 
        }

        private IEnumerator DamageCreatureAnimation(IContainer container, GameAction action)
        {
            var damageAction = (DamageCreatureAction) action;
            
            // TODO: Neutral cards could damage creatures - handle this case here since there's a potential Null Reference to the LessonCost
            var particleType = damageAction.Source.GetAttribute<LessonCost>().Type; 
            var particleSequence = _gameView.GetParticleSequence(damageAction.Source.Owner, damageAction.Target, particleType);

            while (particleSequence.IsPlaying())
            {
                yield return null;
            }
        }
        
        private IEnumerator DamagePlayerAnimation(IContainer container, GameAction action)
        {
            yield return true;
            var damageAction = (DamagePlayerAction) action;

            // TODO: Consolidate these cases
            if (damageAction.Source.Data.Type == CardType.Spell)
            {
                var target = damageAction.Target[Zones.Characters].First(); // NOTE: Should always be the starting character.
                // TODO: Neutral cards could damage players - handle this case here since there's a potential Null Reference to the LessonCost
                var particleType = damageAction.Source.GetAttribute<LessonCost>().Type;
                var particleSequence = _gameView.GetParticleSequence(damageAction.Player, target, particleType);

                while (particleSequence.IsPlaying())
                {
                    yield return null;
                }                    
            }
            else if (damageAction.Source.Data.Type == CardType.Creature)
            {
                var target = damageAction.Target[Zones.Characters].First(); // NOTE: Should always be the starting character.
                var particleSequence = _gameView.GetParticleSequence(damageAction.Source, target);

                while (particleSequence.IsPlaying())
                {
                    yield return null;
                }    
            }
            
            var discardedCards = _gameView.FindCardViews(damageAction.DiscardedCards);
            
            for (var i = discardedCards.Count - 1; i >= 0; i--)
            {
                var cardView = discardedCards[i];
                var sequence = _gameView.GetMoveToZoneSequence(cardView, Zones.Discard, Zones.Deck);
                while (sequence.IsPlaying())
                {
                    yield return null;
                }
            }
        }

        private IEnumerator DiscardAnimation(IContainer container, GameAction action)
        {
            var discardAction = (DiscardAction) action;

            // TODO: Clean  this up somehow...
            if (discardAction.Source.Data.Type == CardType.Spell && !(discardAction.SourceAction is DamageCreatureAction))
            {
                var sequence = DOTween.Sequence();

                foreach (var discardedCard in discardAction.DiscardedCards)
                {
                    // IMPORTANT: This check needs to be done so that spell cards that discard themselves don't do the particle animation 
                    if (discardAction.Source == discardedCard)
                    {
                        continue;
                    }

                    // TODO: Neutral cards could discard cards - handle this case here since there's a potential Null Reference to the LessonCost
                    var particleType = discardAction.Source.GetAttribute<LessonCost>().Type;
                    var particleSequence = _gameView.GetParticleSequence(discardAction.Player, discardedCard, particleType);
                    sequence.Append(particleSequence);
                }
                
                while (sequence.IsPlaying())
                {
                    yield return null;
                }
            }

            var cardViews = _gameView.FindCardViews(discardAction.DiscardedCards);

            // TODO: Cards could come from multiple zones, but we need to capture the before zones for each card for the animation.
            var fromZone = discardAction.DiscardedCards.Select(c => c.Zone).Distinct().Single(); 
            
            yield return true;
            
            foreach (var cardView in cardViews)
            {
                var sequence = _gameView.GetMoveToZoneSequence(cardView, Zones.Discard, fromZone);
                while (sequence.IsPlaying())
                {
                    yield return null;
                }
            }
        }

        private IEnumerator PlayToBoardAnimation(IContainer container, GameAction action)
        {
            var playAction = (PlayToBoardAction) action;
            var beforeZone = playAction.Cards.Select(c => c.Zone).Distinct();
            yield return true;
            
            var cardViewPairs = _gameView.FindCardViews(playAction.Cards)
                .Select(view => (view, view.Card.Data.Type.ToTargetZone()))
                .ToList();

            foreach (var card in playAction.Cards)
            {
                
            }
            
            var sequence = _gameView.GetMoveToZoneSequence(cardViewPairs, Zones.Hand); 
            while (sequence.IsPlaying())
            {
                yield return null;
            }
        }
        
        public void OnDestroy()
        {
            Global.Events.Unsubscribe(Notification.Prepare<PlayToBoardAction>(), OnPreparePlayToBoard);
            Global.Events.Unsubscribe(Notification.Prepare<DamagePlayerAction>(), OnPrepareDamagePlayer);
            Global.Events.Unsubscribe(Notification.Prepare<DamageCreatureAction>(), OnPrepareDamageCreature);
            Global.Events.Unsubscribe(Notification.Prepare<DiscardAction>(), OnPrepareDiscard);
            Global.Events.Unsubscribe(Notification.Prepare<ShuffleDeckAction>(), OnPrepareShuffleDeck);
        }
    }
}
