using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThresholdGame.Core.GameFlow
{
    /// <summary>
    /// Implementación con SceneManager.
    /// Garantiza una duración mínima visible para la pantalla de carga,
    /// evitando flashes en escenas que se cargan en milisegundos.
    /// </summary>
    public sealed class SceneLoader : ISceneLoader
    {
        // Tiempo mínimo que la pantalla de carga estará visible (en segundos).
        // Suficiente para que el usuario vea la transición sin que sea molesto.
        private const float MinDisplayTime = 1.2f;

        public void LoadScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }

        public IEnumerator LoadSceneAsync(
            string sceneName,
            Action<float> onProgress,
            Action onCompleted)
        {
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

            // Fase 2: esperar a que pase el tiempo mínimo de display
            // y simular el último 10% del progreso suavemente
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

            onCompleted?.Invoke();
        }
    }
}