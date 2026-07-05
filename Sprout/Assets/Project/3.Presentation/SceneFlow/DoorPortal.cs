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
        [SerializeField] private string targetScene;
        [SerializeField] private string targetSpawnId;

        [Header("Activación por distancia")]
        [SerializeField] private string playerTag = "Player";

        [Tooltip("Radio alrededor de la puerta para activarse.")]
        [SerializeField] private float triggerRadius = 10f;

        [Tooltip("Si está marcado, entra solo al acercarse.")]
        [SerializeField] private bool autoEnter = true;

        [Tooltip("Tecla para entrar si autoEnter está desmarcado.")]
        [SerializeField] private Key interactKey = Key.E;

        private bool _busy;
        private bool _wasInside;

        private void Update()
        {
            if (_busy)
                return;

            // Busca al player MÁS CERCANO entre todos los que tengan el tag. Así, aunque
            // (temporalmente) haya un duplicado, se usa el que de verdad se acerca a la puerta.
            float dist;
            Transform player = NearestPlayer(out dist);
            if (player == null)
                return;

            bool inside = dist <= triggerRadius;
            bool ePressed = WasInteractPressed();

            if (inside && !_wasInside && autoEnter)
                Enter();
            else if (inside && !autoEnter && ePressed)
                Enter();

            _wasInside = inside;
        }

        private Transform NearestPlayer(out float dist)
        {
            dist = float.MaxValue;
            Transform best = null;
            foreach (var go in GameObject.FindGameObjectsWithTag(playerTag))
            {
                if (go == null || !go.activeInHierarchy) continue;
                float d = Vector3.Distance(go.transform.position, transform.position);
                if (d < dist) { dist = d; best = go.transform; }
            }
            return best;
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