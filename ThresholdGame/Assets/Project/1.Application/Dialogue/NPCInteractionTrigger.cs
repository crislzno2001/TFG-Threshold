using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;


namespace OpenAI.Dialogue
{
    /// <summary>
    /// Añade este script al GameObject del NPC.
    /// Detecta cuando el jugador entra en rango y muestra "Pulsa E para hablar".
    /// </summary>
    public class NPCInteractionTrigger : MonoBehaviour
    {
        [Header("Configuración")]
        [SerializeField] private float interactionRadius = 2.5f;
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private KeyCode interactKey = KeyCode.E;

        [Header("Referencias")]
        [SerializeField] private DialogueUI dialogueUI;
        [SerializeField] private NPCBrain npcBrain;

        [Header("Prompt flotante (opcional)")]
        [SerializeField] private GameObject interactPrompt; // UI "Pulsa E" sobre el NPC

        private bool playerInRange = false;
        private bool dialogueOpen = false;
        private Transform playerTransform;

        private void Start()
        {
            if (interactPrompt != null)
                interactPrompt.SetActive(false);

            // Buscar jugador automáticamente si no está asignado
            var player = GameObject.FindGameObjectWithTag(playerTag);
            if (player != null) playerTransform = player.transform;
        }

        private void Update()
        {
            if (playerTransform == null) return;

            float dist = Vector3.Distance(transform.position, playerTransform.position);
            playerInRange = dist <= interactionRadius;

            // Mostrar/ocultar prompt flotante
            if (interactPrompt != null)
                interactPrompt.SetActive(playerInRange && !dialogueOpen);

            // Abrir diálogo
            if (playerInRange && !dialogueOpen && Keyboard.current.eKey.wasPressedThisFrame)
            {
                OpenDialogue();
            }

            // Cerrar con Escape
            if (dialogueOpen && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CloseDialogue();
            }
        }

        private void OpenDialogue()
        {
            dialogueOpen = true;
            dialogueUI.Open(npcBrain);

            // Bloquear movimiento del jugador
            var playerController = playerTransform.GetComponent<PlayerDialogueLock>();
            if (playerController != null) playerController.Lock();
        }

        public void CloseDialogue()
        {
            dialogueOpen = false;
            dialogueUI.Close();

            // Devolver control al jugador
            if (playerTransform != null)
            {
                var playerController = playerTransform.GetComponent<PlayerDialogueLock>();
                if (playerController != null) playerController.Unlock();
            }
        }

        // Dibuja el radio en el editor para visualizarlo
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactionRadius);
        }
    }
}