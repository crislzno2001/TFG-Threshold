using UnityEngine;
using UnityEngine.InputSystem;

namespace Sprout.SceneFlow
{
    /// <summary>
    /// Puerta que lleva a otra escena estilo Animal Crossing.
    /// Detecta al jugador por distancia usando una esfera virtual, no necesita Collider.
    ///
    /// Ponlo en un objeto vacío colocado delante de la puerta.
    /// Configura targetScene, targetSpawnId y triggerRadius.
    /// </summary>
    public sealed class DoorPortal : MonoBehaviour
    {
        [Header("Destino")]
        [SerializeField] private string targetScene = "FlowerShop";
        [SerializeField] private string targetSpawnId = "Entrada";

        [Header("Activación por distancia")]
        [SerializeField] private string playerTag = "Player";

        [Tooltip("Radio alrededor de la puerta para activarse.")]
        [SerializeField] private float triggerRadius = 2.5f;

        [Tooltip("Si está marcado, entra solo al acercarse.")]
        [SerializeField] private bool autoEnter = true;

        [Tooltip("Tecla para entrar si autoEnter está desmarcado.")]
        [SerializeField] private Key interactKey = Key.E;

        private Transform _player;
        private bool _busy;
        private bool _wasInside;

        private void Update()
        {
            if (_busy)
                return;

            CachePlayerIfNeeded();

            if (_player == null)
                return;

            bool inside = Vector3.Distance(_player.position, transform.position) <= triggerRadius;

            if (inside && !_wasInside && autoEnter)
            {
                Enter();
            }
            else if (inside && !autoEnter && WasInteractPressed())
            {
                Enter();
            }

            _wasInside = inside;
        }

        private void CachePlayerIfNeeded()
        {
            if (_player != null)
                return;

            GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);

            if (playerObject != null)
                _player = playerObject.transform;
        }

        private bool WasInteractPressed()
        {
            return interactKey != Key.None
                   && Keyboard.current != null
                   && Keyboard.current[interactKey].wasPressedThisFrame;
        }

        private void Enter()
        {
            if (_busy)
                return;

            _busy = true;
            SceneTransitionManager.GetOrCreate().Go(targetScene, targetSpawnId);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.85f, 0.3f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, triggerRadius);
        }
    }
}