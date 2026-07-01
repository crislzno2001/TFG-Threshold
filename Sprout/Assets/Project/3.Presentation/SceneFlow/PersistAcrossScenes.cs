using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sprout.SceneFlow
{
    /// <summary>
    /// Ponlo en el objeto RAÍZ que debe sobrevivir a los cambios de escena: el jugador (florista), su
    /// cámara, los managers y la UI. Así, al pasar del pueblo al interior, NO se reinician ni se duplican.
    ///
    /// Regla del sistema estilo Animal Crossing:
    ///   - El jugador/cámara/UI viven en este objeto persistente (solo en la escena inicial, p. ej. el
    ///     pueblo). Las escenas de interior NO llevan jugador, solo el decorado + SpawnPoints + puertas.
    ///
    /// Usa un 'key' para que solo exista una copia: si cargas otra escena que también lo tuviera, la
    /// segunda se autodestruye.
    /// </summary>
    public sealed class PersistAcrossScenes : MonoBehaviour
    {
        [Tooltip("Identificador del grupo persistente. Déjalo igual en todas las copias (p. ej. 'PlayerRig').")]
        public string key = "PlayerRig";

        [Tooltip("Al cargar una escena con uno de estos nombres (el menú), el rig se AUTODESTRUYE. Así el " +
                 "jugador no se queda colgado en el menú (ni su AudioListener). Pon el nombre EXACTO de tu escena de menú.")]
        [SerializeField] private string[] destroyOnScenes = { "MainMenu" };

        private static readonly System.Collections.Generic.HashSet<string> _alive = new();

        private void Awake()
        {
            if (_alive.Contains(key))
            {
                // Ya hay uno vivo de este grupo -> esta copia sobra.
                Destroy(gameObject);
                return;
            }
            _alive.Add(key);
            transform.SetParent(null); // DontDestroyOnLoad solo funciona en objetos raíz
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Si volvemos al menú, este rig de juego ya no pinta nada -> se destruye solo.
            foreach (var name in destroyOnScenes)
                if (!string.IsNullOrEmpty(name) && scene.name == name)
                {
                    Destroy(gameObject);
                    return;
                }
        }

        private void OnDestroy()
        {
            _alive.Remove(key);
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }
}
