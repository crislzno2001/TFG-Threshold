using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThresholdGame.Core.GameFlow
{

    // ─────────────────────────────────────────────────────────────────────────
    // ENDING STATE — se superpone al Game
    // ─────────────────────────────────────────────────────────────────────────
    public sealed class GameEndingState : IGameState
    {
        private readonly GameStateMachine _sm;

        public GameEndingState(GameStateMachine sm) => _sm = sm;

        public void Enter()
        {
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            _sm.RaiseOnEnterEnding();
        }

        public void Tick() { }
        public void Exit() { }
    }
}