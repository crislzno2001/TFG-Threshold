using UnityEngine;

namespace Sprout.SceneFlow
{
    /// <summary>
    /// Puerta que lleva a otra escena (estilo Animal Crossing). Ponlo en un objeto con un Collider
    /// marcado como "Is Trigger" delante de la puerta. Cuando el jugador está dentro del trigger y pulsa
    /// la tecla (E por defecto) — o automáticamente si marcas autoEnter — hace el fundido y cambia de escena.
    ///
    /// Configura:
    ///   - targetScene : nombre EXACTO de la escena destino (debe estar en Build Settings).
    ///   - targetSpawnId : id del SpawnPoint donde aparecer en esa escena.
    ///
    /// Ejemplo (entrar a casa de Mochi): targetScene="Interior_Mochi", targetSpawnId="Entrada".
    /// Ejemplo (salir al pueblo):        targetScene="Pueblo",        targetSpawnId="Puerta_Mochi".
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class DoorPortal : MonoBehaviour
    {
        [Header("Destino")]
        [Tooltip("Nombre exacto de la escena destino (File > Build Settings).")]
        public string targetScene = "Interior_Mochi";
        [Tooltip("Id del SpawnPoint donde aparecer en la escena destino.")]
        public string targetSpawnId = "Entrada";

        [Header("Activación")]
        [Tooltip("Tag del jugador.")]
        public string playerTag = "Player";
        [Tooltip("Si está marcado, entra solo al pisar el trigger (sin pulsar tecla).")]
        public bool autoEnter = false;
        [Tooltip("Tecla para entrar si autoEnter está desmarcado.")]
        public KeyCode interactKey = KeyCode.E;

        private bool _playerInside;

        private void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;
            _playerInside = true;
            if (autoEnter) Enter();
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(playerTag)) _playerInside = false;
        }

        private void Update()
        {
            if (_playerInside && !autoEnter && Input.GetKeyDown(interactKey))
                Enter();
        }

        private void Enter()
        {
            _playerInside = false;
            SceneTransitionManager.GetOrCreate().Go(targetScene, targetSpawnId);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.85f, 0.3f, 0.8f);
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.6f);
        }
    }
}
