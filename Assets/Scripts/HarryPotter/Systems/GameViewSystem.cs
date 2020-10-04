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
using HarryPotter.UI;
using HarryPotter.UI.Cursor;
using HarryPotter.UI.Tooltips;
using HarryPotter.Views;
using UnityEngine;
using UnityEngine.Serialization;

namespace HarryPotter.Systems
{
    public class GameViewSystem : MonoBehaviour, IGameSystem
    { 
        [FormerlySerializedAs("Game")] 
        public MatchData Match;
        
        public CardView CardPrefab;
        public float TweenTimescale = 4f;
        public TooltipController Tooltip { get; private set; }
        
        public CursorController Cursor { get; private set; }
        
        //NOTE: We may want to use a different kind of input controller in the future
        public ClickToPlayCardController Input { get; set; }
        

        public BoardView Board { get; set; }
        private ActionSystem _actionSystem;

        private IContainer _container;
        public IContainer Container
        {
            get
            {
                if (_container == null)
                {
                    _container = GameFactory.Create(Match);
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
            
            Tooltip = GetComponentInChildren<TooltipController>();
            Cursor = GetComponentInChildren<CursorController>();
            Input = GetComponent<ClickToPlayCardController>();
            Board = GetComponent<BoardView>();
            if (Match == null)
            {
                Debug.LogError("GameView does not have GameData attached.");
                return;
            }

            if (Tooltip == null)
            {
                Debug.LogError("GameView could not find TooltipController.");
                return;
            }

            if (Cursor == null)
            {
                Debug.LogError("GameView could not find CursorController.");
                return;
            }
            if (Input == null)
            {
                Debug.LogError("GameView could not find InputController.");
                return;
            }

            if (Board == null)
            {
                Debug.LogError("GameView could not find BoardView.");
                return;
            }

            Container.Awake();
            _actionSystem = Container.GetSystem<ActionSystem>();
        }

        private void Start()
        {
            SetupSinglePlayer();
        }
        
        private void SetupSinglePlayer() 
        {
            Match.Players[0].ControlMode = ControlMode.Local;
            Match.Players[1].ControlMode = ControlMode.Computer;
            
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
    }
}