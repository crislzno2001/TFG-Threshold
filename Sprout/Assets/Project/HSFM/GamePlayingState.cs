using System.Collections;
using UnityEngine;
using OpenAI;

namespace ThresholdGame.Core.GameFlow
{
    /// <summary>
    /// El juego está activo. Antes de cargar la escena Game, comprueba que la
    /// conexión con la IA funciona (la IA es obligatoria en Sprout). Si la IA no
    /// está disponible, NO carga el pueblo y vuelve al menú principal. Si todo va
    /// bien, carga la escena Game de forma asíncrona reportando progreso.
    /// </summary>
    public sealed class GamePlayingState : IGameState
    {
        private readonly GameStateMachine _sm;
        private readonly ISceneLoader _sceneLoader;

        // Última razón de fallo de IA, por si el menú quiere mostrarla.
        public static string LastAIError { get; private set; }

        public GamePlayingState(GameStateMachine sm, ISceneLoader sceneLoader)
        {
            _sm = sm;
            _sceneLoader = sceneLoader;
        }

        public void Enter()
        {
            Time.timeScale = 1f;
            Debug.Log("[GamePlayingState] Enter — comprobando IA antes de cargar el pueblo");

            // La UI de carga escucha este evento (mostramos 'cargando' mientras
            // se verifica la IA y luego mientras se carga la escena).
            _sm.RaiseOnEnterLoading();

            _sm.RunCoroutine(CheckAiThenLoad());
        }

        private IEnumerator CheckAiThenLoad()
        {
            LastAIError = null;

            if (!AIConfig.HasKey)
            {
                LastAIError = "No hay clave de IA configurada (~/.openai/auth.json).";
                Debug.LogError($"[GamePlayingState] {LastAIError} Volviendo al menú.");
                _sm.GoToMainMenu();
                yield break;
            }

            var api = new OpenAIApi();
            var task = api.TestConnection("gpt-4o-mini", 15f);
            while (!task.IsCompleted) yield return null;

            if (!task.Result.IsOk)
            {
                LastAIError = task.Result.Message;
                Debug.LogError($"[GamePlayingState] IA no disponible: {LastAIError}. Volviendo al menú.");
                _sm.GoToMainMenu();
                yield break;
            }

            Debug.Log("[GamePlayingState] IA verificada — cargando el pueblo");
            yield return _sceneLoader.LoadSceneAsync(
                SceneNames.Game,
                onProgress: p => _sm.ReportLoadProgress(p),
                onCompleted: () => _sm.RaiseOnEnterPlaying()
            );
        }

        public void Tick() { }
        public void Exit() { }
    }
}
