using HarryPotter.Data;
using HarryPotter.StateManagement;
using HarryPotter.StateManagement.GameStates;

namespace HarryPotter.Systems.Core
{
    public static class GameFactory
    {
        public static Container Create(GameState gameState)
        {
            var game = new Container(gameState);

            game.AddSystem<ActionSystem>();
            game.AddSystem<TurnSystem>();
            game.AddSystem<PlayerSystem>();
            game.AddSystem<VictorySystem>();
            game.AddSystem<PlayerActionSystem>();
            
            // TODO: Only add AISystem when in Single Player Mode, otherwise, add NetworkSystem for multiplayer
            game.AddSystem<AISystem>();
            
            game.AddSystem<StateMachine>();
            game.AddSystem<GlobalGameStateSystem>();

            return game;
        }
    }
}