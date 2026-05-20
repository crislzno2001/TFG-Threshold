using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThresholdGame.Core.GameFlow
{
    // ─────────────────────────────────────────────────────────────────────────
    // PAUSED STATE — no carga escena, se superpone al Game
    // ─────────────────────────────────────────────────────────────────────────
    public sealed class GamePausedState : IGameState
    {
        private readonly GameStateMachine _sm;

        public GamePausedState(GameStateMachine sm) => _sm = sm;

        public void Enter()
        {
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            _sm.RaiseOnEnterPaused();
        }

        public void Tick() { }

        public void Exit()
        {
            Time.timeScale = 1f;
        }
    }

}