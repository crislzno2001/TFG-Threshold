using System;
using System.Collections;

namespace ThresholdGame.Core.GameFlow
{
    /// <summary>
    /// Abstracción para carga de escenas.
    /// Permite cambiar la implementación (SceneManager, Addressables, fades...)
    /// sin tocar los estados de la HFSM.
    /// </summary>
    public interface ISceneLoader
    {
        /// <summary>Carga síncrona inmediata. Usar solo para escenas muy ligeras.</summary>
        void LoadScene(string sceneName);

        /// <summary>
        /// Carga asíncrona con callback de progreso.
        /// Devuelve un IEnumerator para poder ejecutarlo como coroutine desde un MonoBehaviour.
        /// </summary>
        IEnumerator LoadSceneAsync(
            string sceneName,
            Action<float> onProgress,
            Action onCompleted);
    }
}