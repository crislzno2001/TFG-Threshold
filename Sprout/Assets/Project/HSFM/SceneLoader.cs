using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThresholdGame.Core.GameFlow
{
    /// <summary>
    /// Implementaci�n con SceneManager.
    /// Garantiza una duraci�n m�nima visible para la pantalla de carga,
    /// evitando flashes en escenas que se cargan en milisegundos.
    /// </summary>
    public sealed class SceneLoader : ISceneLoader
    {
        // Tiempo m�nimo que la pantalla de carga estar� visible (en segundos).
        // Suficiente para que el usuario vea la transici�n sin que sea molesto.
        private const float MinDisplayTime = 3.0f;

        public void LoadScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }

        public IEnumerator LoadSceneAsync(
            string sceneName,
            Action<float> onProgress,
            Action onCompleted)
        {
            // Cargar con prioridad BAJA reparte el trabajo de carga en más frames,
            // dejando la animación de la pantalla de carga fluida (sin tirones).
            ThreadPriority prevPriority = UnityEngine.Application.backgroundLoadingPriority;
            UnityEngine.Application.backgroundLoadingPriority = ThreadPriority.Low;

            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            op.allowSceneActivation = false;

            float startTime = Time.unscaledTime;

            // Fase 1: cargar hasta 0.9 (Unity reserva 0.9-1.0 para activar)
            while (op.progress < 0.9f)
            {
                float loadProgress = Mathf.Clamp01(op.progress / 0.9f);
                onProgress?.Invoke(loadProgress * 0.9f);
                yield return null;
            }

            // Fase 2: esperar a que pase el tiempo m�nimo de display
            // y simular el �ltimo 10% del progreso suavemente
            float elapsed;
            while ((elapsed = Time.unscaledTime - startTime) < MinDisplayTime)
            {
                float t = Mathf.Clamp01(elapsed / MinDisplayTime);
                onProgress?.Invoke(0.9f + 0.1f * t);
                yield return null;
            }

            onProgress?.Invoke(1f);
            yield return new WaitForSecondsRealtime(0.15f);

            op.allowSceneActivation = true;
            yield return op;

            // Tras activar, la escena ejecuta todos sus Awake/Start (el parón). Mantenemos
            // la pantalla de carga tapando unos frames más para esconder ese tirón inicial
            // y dejar que la escena nueva se estabilice antes de mostrarla.
            for (int i = 0; i < 5; i++) yield return null;
            yield return new WaitForSecondsRealtime(0.1f);

            // Restaurar la prioridad normal una vez cargada la escena.
            UnityEngine.Application.backgroundLoadingPriority = prevPriority;

            onCompleted?.Invoke();
        }
    }
}