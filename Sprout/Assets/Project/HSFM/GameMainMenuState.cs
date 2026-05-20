using UnityEngine;
using UnityEngine.SceneManagement;



namespace ThresholdGame.Core.GameFlow
{
    // ─────────────────────────────────────────────────────────────────────────
    // MAIN MENU STATE
    // ─────────────────────────────────────────────────────────────────────────
    public sealed class GameMainMenuState : IGameState
    {
        private readonly GameStateMachine _sm;
        private readonly ISceneLoader _sceneLoader;

        public GameMainMenuState(GameStateMachine sm, ISceneLoader sceneLoader)
        {
            _sm = sm;
            _sceneLoader = sceneLoader;
        }

        public void Enter()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            _sceneLoader.LoadScene(SceneNames.MainMenu);
            _sm.RaiseOnEnterMainMenu();
        }

        public void Tick() { }
        public void Exit() { }
    }
}