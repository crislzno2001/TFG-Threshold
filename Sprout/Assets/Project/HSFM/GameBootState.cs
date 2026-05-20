using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThresholdGame.Core.GameFlow
{
    // ─────────────────────────────────────────────────────────────────────────
    // BOOT STATE
    // ─────────────────────────────────────────────────────────────────────────
    public sealed class GameBootState : IGameState
    {
        private readonly GameStateMachine _sm;

        public GameBootState(GameStateMachine sm) => _sm = sm;

        public void Enter()
        {
            Debug.Log("[Boot] Inicializando...");
            _sm.GoToMainMenu();
        }

        public void Tick() { }
        public void Exit() { }
    }
}