using UnityEngine;
using ThresholdGame.Core.Interaction;
using ThresholdGame.Presentation.NPC;
using OpenAI.Dialogue;

namespace ThresholdGame.Presentation.Interaction
{
    /// <summary>
    /// Implementación de IInteractable para NPCs con diálogo conversacional.
    /// No lee input. No cambia estados directamente.
    /// Habla con IPlayerController para las transiciones de estado.
    /// </summary>
    public class NPCInteractionTrigger : MonoBehaviour, IInteractable
    {
        [SerializeField] private DialogueUI dialogueUI;
        [SerializeField] private NPCBrain npcBrain;

        private IPlayerController _player;

        private void Awake()
        {
            // Si no se asignaron en el inspector, intentamos resolverlos solos.
            if (npcBrain == null) npcBrain = GetComponent<NPCBrain>();
            if (dialogueUI == null)
                dialogueUI = FindFirstObjectByType<DialogueUI>(FindObjectsInactive.Include);
        }

        // ── IInteractable ──────────────────────────────────────────────────────

        public string InteractionLabel => "Hablar";

        public void Interact(IPlayerController player)
        {
            _player = player;

            if (npcBrain == null) npcBrain = GetComponent<NPCBrain>();
            if (dialogueUI == null)
                dialogueUI = FindFirstObjectByType<DialogueUI>(FindObjectsInactive.Include);

            if (npcBrain == null)
            {
                Debug.LogError($"[NPCInteractionTrigger] '{name}' no tiene NPCBrain. Asígnalo en el inspector.", this);
                return;
            }
            if (dialogueUI == null)
            {
                Debug.LogError($"[NPCInteractionTrigger] No encuentro ningún DialogueUI en la escena para '{name}'. " +
                               "Asigna el campo 'Dialogue UI' (el del canvas de diálogo).", this);
                return;
            }

            npcBrain.isInteracting = true;
            GetComponent<NPCStateMachine>()?.SuspendAutonomy(); // que se pare mientras hablas
            dialogueUI.Open(npcBrain);

            player.EnterDialogue();
        }

        public void CancelInteraction()
        {
            // NO cerramos el diálogo solo por salir del trigger: durante la conversación el jugador está
            // bloqueado, y cerrarlo aquí (en un callback de física) hace que TMP pete al desactivar el panel.
            // El diálogo se cierra con el botón de cerrar o al despedirse.
            if (dialogueUI != null && dialogueUI.IsOpen) return;
            CloseDialogue();
        }

        // ── API pública llamada por DialogueUI cuando el jugador cierra el panel ─

        public void CloseDialogue()
        {
            if (npcBrain != null) npcBrain.isInteracting = false;
            GetComponent<NPCStateMachine>()?.ResumeAutonomy(); // vuelve a pasear al cerrar

            // Progreso de fase (modo simple): hablar cuenta como hito, si está activado en DayCycleService.
            if (npcBrain != null)
                FindFirstObjectByType<Sprout.Application.DayCycleService>()?.RegisterPhaseGoalFromTalk(npcBrain.npcName);
            dialogueUI?.Close();
            _player?.EnterFreeRoam();
            _player = null;
        }
    }
}