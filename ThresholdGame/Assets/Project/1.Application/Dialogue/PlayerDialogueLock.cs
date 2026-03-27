using UnityEngine;

namespace OpenAI.Dialogue
{
    /// <summary>
    /// Añade este script al GameObject del jugador.
    /// Bloquea/desbloquea el movimiento durante los diálogos.
    /// 
    /// Compatible con CharacterController, Rigidbody y cualquier
    /// script de movimiento que uses — ajusta según tu caso.
    /// </summary>
    public class PlayerDialogueLock : MonoBehaviour
    {
        // Referencia a tu script de movimiento
        // Cambia el tipo según el que uses en tu proyecto
        [SerializeField] private MonoBehaviour movementScript;
        [SerializeField] private bool hideCursorWhenUnlocked = true;

        private bool isLocked = false;

        private void Start()
        {
            if (hideCursorWhenUnlocked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        public void Lock()
        {
            isLocked = true;

            // Desactivar movimiento
            if (movementScript != null)
                movementScript.enabled = false;

            // Mostrar cursor para escribir
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Parar rigidbody si lo tiene
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        public void Unlock()
        {
            isLocked = false;

            // Reactivar movimiento
            if (movementScript != null)
                movementScript.enabled = true;

            // Recuperar cursor
            if (hideCursorWhenUnlocked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        public bool IsLocked => isLocked;
    }
}