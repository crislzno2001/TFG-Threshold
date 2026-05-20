using UnityEngine;
using UnityEngine.InputSystem;
using ThresholdGame.Presentation.Player;

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
        [SerializeField] private GameObject interactPrompt;

        private bool playerInRange = false;
        private bool dialogueOpen  = false;
        private Transform playerTransform;

        private void Start()
        {
            if (interactPrompt != null)
                interactPrompt.SetActive(false);

            var col = GetComponent<SphereCollider>();
            if (col == null)
                col = gameObject.AddComponent<SphereCollider>();

            col.isTrigger = true;
            col.radius = interactionRadius;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;
            playerTransform = other.transform;
            playerInRange   = true;
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
            if (playerInRange && !dialogueOpen && Keyboard.current.eKey.wasPressedThisFrame)
                OpenDialogue();

            if (dialogueOpen && Keyboard.current.escapeKey.wasPressedThisFrame)
                CloseDialogue();
        }

        private void OpenDialogue()
        {
            dialogueOpen           = true;
            npcBrain.isInteracting = true;
            dialogueUI.Open(npcBrain);

            var sm = playerTransform.GetComponent<PlayerStateMachine>();
            if (sm != null) sm.EnterDialogue();
        }

        public void CloseDialogue()
        {
            dialogueOpen           = false;
            npcBrain.isInteracting = false;
            dialogueUI.Close();

            if (playerTransform != null)
            {
                var sm = playerTransform.GetComponent<PlayerStateMachine>();
                if (sm != null) sm.EnterFreeRoam();
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactionRadius);
        }
    }
}
