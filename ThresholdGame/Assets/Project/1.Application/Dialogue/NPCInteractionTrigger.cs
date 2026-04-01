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

        [Header("Referencias")]
        [SerializeField] private DialogueUI dialogueUI;
        [SerializeField] private NPCBrain npcBrain;

        [Header("Prompt flotante (opcional)")]
        [SerializeField] private GameObject interactPrompt; // UI "Pulsa E" sobre el NPC

        private bool playerInRange = false;
        private bool dialogueOpen = false;
        private Transform playerTransform;


        // En NPCInteractionTrigger.cs — sustituye Update() y Start() por esto:

        private void Start()
        {
            if (interactPrompt != null)
                interactPrompt.SetActive(false);

            // Crear collider trigger automáticamente
            var col = gameObject.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = interactionRadius;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;
            playerTransform = other.transform;
            playerInRange = true;
            if (interactPrompt != null) interactPrompt.SetActive(true);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;
            playerInRange = false;
            if (interactPrompt != null && !dialogueOpen)
                interactPrompt.SetActive(false);
        }

        private void Update()
        {
            // Ahora Update solo maneja input, no distancia
            if (playerInRange && !dialogueOpen && Keyboard.current.eKey.wasPressedThisFrame)
                OpenDialogue();

            if (dialogueOpen && Keyboard.current.escapeKey.wasPressedThisFrame)
                CloseDialogue();
        }

        private void OpenDialogue()
        {
            dialogueOpen = true;
            npcBrain.isInteracting = true;

            dialogueUI.Open(npcBrain);

            // Bloquear movimiento del jugador
            var playerController = playerTransform.GetComponent<PlayerDialogueLock>();
            if (playerController != null) playerController.Lock();
        }

        public void CloseDialogue()
        {
            dialogueOpen = false;
            npcBrain.isInteracting = false;

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