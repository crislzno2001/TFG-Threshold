using UnityEngine;

namespace Sprout.SceneFlow
{
    /// <summary>
    /// Punto de aparición dentro de una escena. Cuando se carga una escena, el SceneTransitionManager
    /// busca el SpawnPoint cuyo id coincide con el pedido por la puerta y coloca ahí al jugador.
    ///
    /// Ejemplo: la puerta de la casa de Mochi (en el pueblo) lleva a la escena "Interior_Mochi" con
    /// spawnId "Entrada". En "Interior_Mochi" pones un SpawnPoint con id "Entrada" justo dentro.
    /// La puerta de salida (dentro) lleva al "Pueblo" con spawnId "Puerta_Mochi", y en el pueblo pones
    /// un SpawnPoint con id "Puerta_Mochi" delante de la casa.
    /// </summary>
    public sealed class SpawnPoint : MonoBehaviour
    {
        [Tooltip("Identificador único dentro de la escena. La puerta pide este id.")]
        public string id = "Entrada";

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.9f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.9f, 0.35f);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 1.2f);
        }
    }
}
