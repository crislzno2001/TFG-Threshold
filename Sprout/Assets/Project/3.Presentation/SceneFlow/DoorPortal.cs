using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Sprout.SceneFlow
{
    /// <summary>
    /// Puerta que lleva a otra escena (estilo Animal Crossing). Detecta al jugador por DISTANCIA (no por
    /// collider, así no falla): cuando te acercas, suena la puerta y cambia de escena con el iris circular.
    ///
    /// Ponlo en un objeto vacío en la puerta. NO necesita Collider. Si el objeto tenía un Box Collider
    /// sólido que te bloqueaba, quítalo o márcalo Is Trigger.
    ///
    /// Configura: targetScene, targetSpawnId, doorSound (Door_Squeak_1). autoEnter = entrar al acercarse.
    /// </summary>
    public sealed class DoorPortal : MonoBehaviour
    {
        [Header("Destino")]
        public string targetScene = "FlowerShop";
        public string targetSpawnId = "Entrada";

        [Header("Activación (por distancia, fiable)")]
        public string playerTag = "Player";
        [Tooltip("Radio alrededor de la puerta para activarse.")]
        public float triggerRadius = 2.5f;
        [Tooltip("Si está marcado, entra solo al acercarse (sin pulsar E). Recomendado.")]
        public bool autoEnter = true;
        [Tooltip("Tecla para entrar si autoEnter está desmarcado.")]
        public Key interactKey = Key.E;

        [Header("Sonido + animación")]
        [Tooltip("Sonido de puerta (Suntail: Door_Squeak_1..4). Suena siempre al entrar.")]
        public AudioClip doorSound;
        [Range(0f, 1f)] public float volume = 0.9f;
        [Tooltip("Opcional: componente Door de Suntail (se llamará a PlayDoorAnimation).")]
        public MonoBehaviour suntailDoor;
        [Tooltip("Espera tras abrir antes de cambiar de escena.")]
        public float waitAfterOpen = 0.45f;

        private Transform _player;
        private bool _busy;
        private bool _wasInside;

        private void Update()
        {
            if (_busy) return;

            if (_player == null)
            {
                var p = GameObject.FindGameObjectWithTag(playerTag);
                if (p == null) return;
                _player = p.transform;
            }

            bool inside = Vector3.Distance(_player.position, transform.position) <= triggerRadius;

            if (inside && !_wasInside && autoEnter)
                Enter();
            else if (inside && !autoEnter && interactKey != Key.None &&
                     Keyboard.current != null && Keyboard.current[interactKey].wasPressedThisFrame)
                Enter();

            _wasInside = inside;
        }

        private void Enter()
        {
            if (_busy) return;
            StartCoroutine(OpenAndGo());
        }

        private IEnumerator OpenAndGo()
        {
            _busy = true;

            // Sonido SIEMPRE (fiable, no depende del Animator de Suntail).
            if (doorSound != null)
                AudioSource.PlayClipAtPoint(doorSound, transform.position, volume);

            // Animación de Suntail si la has asignado.
            if (suntailDoor != null)
            {
                var m = suntailDoor.GetType().GetMethod("PlayDoorAnimation");
                if (m != null) m.Invoke(suntailDoor, null);
            }

            if (waitAfterOpen > 0f) yield return new WaitForSeconds(waitAfterOpen);

            SceneTransitionManager.GetOrCreate().Go(targetScene, targetSpawnId);
            _busy = false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.85f, 0.3f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, triggerRadius);
        }
    }
}
