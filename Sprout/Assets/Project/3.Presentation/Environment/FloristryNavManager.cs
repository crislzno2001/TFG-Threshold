using UnityEngine;
using UnityEngine.AI;

namespace Sprout.World
{
    /// <summary>
    /// Gestor de NavMesh y puntos de navegación de la floristería.
    /// 
    /// Define los waypoints que usan los NPCs (Mochi, Aster, Moth, Rix)
    /// cuando visitan la tienda. Los puntos de NavMesh se definen aquí
    /// como Transform hijos del GameObject para que sean fáciles de mover
    /// en el editor sin tocar código.
    /// 
    /// También expone una API para que el DayManager (futuro) sepa
    /// en qué zona está el jugador (interior / exterior).
    /// 
    /// MonoBehaviour — colocarlo en el GameObject raíz de la floristería.
    /// </summary>
    public sealed class FloristryNavManager : MonoBehaviour
    {
        // ── Waypoints ──────────────────────────────────────────────────────────

        [Header("Puntos de llegada de los NPCs")]
        [Tooltip("Punto delante del mostrador (donde se para el NPC a hablar).")]
        [SerializeField] private Transform counterPoint;

        [Tooltip("Punto cerca de las estanterías (NPC mirando flores).")]
        [SerializeField] private Transform shelvesPoint;

        [Tooltip("Punto cerca de la puerta (NPC que acaba de entrar o va a salir).")]
        [SerializeField] private Transform doorPoint;

        [Tooltip("Punto central de la tienda (NPC paseando).")]
        [SerializeField] private Transform centerPoint;

        [Header("Salida al exterior")]
        [Tooltip("Punto exterior al que se teletransporta el jugador al salir.")]
        [SerializeField] private Transform exitPoint;

        // ── Zona del jugador ───────────────────────────────────────────────────

        [Header("Detección de zona")]
        [Tooltip("Collider trigger que cubre el interior de la floristería.")]
        [SerializeField] private Collider interiorZone;

        private bool _playerIsInside = false;

        // ── Eventos ────────────────────────────────────────────────────────────

        /// <summary>
        /// Se dispara cuando el jugador entra o sale de la floristería.
        /// Parámetro: true = acaba de entrar, false = acaba de salir.
        /// </summary>
        public System.Action<bool> OnPlayerZoneChanged;

        // ── Propiedades públicas ───────────────────────────────────────────────

        public bool PlayerIsInside => _playerIsInside;

        public Transform CounterPoint   => counterPoint;
        public Transform ShelvesPoint   => shelvesPoint;
        public Transform DoorPoint      => doorPoint;
        public Transform CenterPoint    => centerPoint;
        public Transform ExitPoint      => exitPoint;

        // ── Waypoint aleatorio ─────────────────────────────────────────────────

        /// <summary>
        /// Devuelve un waypoint aleatorio de los disponibles.
        /// Los NPCs lo usan para deambular por la tienda de forma natural.
        /// </summary>
        public Transform GetRandomWaypoint()
        {
            Transform[] points = { counterPoint, shelvesPoint, centerPoint };

            // Filtrar nulls
            var valid = System.Array.FindAll(points, p => p != null);

            if (valid.Length == 0)
            {
                Debug.LogWarning("[FloristryNavManager] No hay waypoints configurados.");
                return transform;
            }

            return valid[Random.Range(0, valid.Length)];
        }

        /// <summary>
        /// Devuelve la posición NavMesh más cercana a un punto dado.
        /// Útil para asegurarse de que un NPC no se quede colgado fuera del NavMesh.
        /// </summary>
        public bool TryGetNavMeshPosition(Vector3 worldPosition, out Vector3 navPosition, float maxDistance = 2f)
        {
            if (NavMesh.SamplePosition(worldPosition, out NavMeshHit hit, maxDistance, NavMesh.AllAreas))
            {
                navPosition = hit.position;
                return true;
            }

            navPosition = worldPosition;
            return false;
        }

        // ── Detección de zona (trigger) ────────────────────────────────────────

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            _playerIsInside = true;
            OnPlayerZoneChanged?.Invoke(true);

            Debug.Log("[FloristryNavManager] Jugador entró en la floristería.");
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            _playerIsInside = false;
            OnPlayerZoneChanged?.Invoke(false);

            Debug.Log("[FloristryNavManager] Jugador salió de la floristería.");
        }

        // ── Gizmos ─────────────────────────────────────────────────────────────

        private void OnDrawGizmos()
        {
            DrawWaypoint(counterPoint,  new Color(0.2f, 0.8f, 0.2f));
            DrawWaypoint(shelvesPoint,  new Color(0.2f, 0.6f, 1.0f));
            DrawWaypoint(doorPoint,     new Color(1.0f, 0.8f, 0.2f));
            DrawWaypoint(centerPoint,   new Color(0.8f, 0.4f, 0.8f));
            DrawWaypoint(exitPoint,     new Color(1.0f, 0.3f, 0.3f));
        }

        private void DrawWaypoint(Transform point, Color color)
        {
            if (point == null) return;

            Gizmos.color = color;
            Gizmos.DrawSphere(point.position, 0.2f);
            Gizmos.DrawLine(transform.position, point.position);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(point.position + Vector3.up * 0.4f, point.name);
#endif
        }
    }
}
