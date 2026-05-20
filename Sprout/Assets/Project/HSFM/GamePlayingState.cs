using UnityEngine;

namespace ThresholdGame.Core.GameFlow
{
    /// <summary>
    /// El juego está activo. Carga la escena Game de forma asíncrona y
    /// va reportando progreso para que la pantalla de carga lo muestre.
    /// </summary>
    public sealed class GamePlayingState : IGameState
    {
        private readonly GameStateMachine _sm;
        private readonly ISceneLoader _sceneLoader;

        public GamePlayingState(GameStateMachine sm, ISceneLoader sceneLoader)
        {
            _sm = sm;
            _sceneLoader = sceneLoader;
        }

        public void Enter()
        {
            Time.timeScale = 1f;
            Debug.Log("[GamePlayingState] Enter — disparando Ev_EnterLoading");

            // Notifica que arranca la carga (la UI escucha este evento para
            // mostrar la pantalla de loading).
            _sm.RaiseOnEnterLoading();

            // Lanza la coroutine de carga asíncrona a través de la FSM
            // (los estados son clases puras y no pueden lanzar coroutines).
            _sm.RunCoroutine(
                _sceneLoader.LoadSceneAsync(
                    SceneNames.Game,
                    onProgress: p => _sm.ReportLoadProgress(p),
                    onCompleted: () => _sm.RaiseOnEnterPlaying()
                )
            );
        }

        public void Tick() { }
        public void Exit() { }
    }
}