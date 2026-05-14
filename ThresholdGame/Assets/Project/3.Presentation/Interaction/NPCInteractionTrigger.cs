using UnityEngine;
using ThresholdGame.Core.Interaction;
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

        // ── IInteractable ──────────────────────────────────────────────────────

        public string InteractionLabel => "Hablar";

        public void Interact(IPlayerController player)
        {
            _player = player;

            npcBrain.isInteracting = true;
            dialogueUI.Open(npcBrain);

            player.EnterDialogue();
        }

        public void CancelInteraction()
        {
            CloseDialogue();
        }

        // ── API pública llamada por DialogueUI cuando el jugador cierra el panel ─

        public void CloseDialogue()
        {
            if (npcBrain != null) npcBrain.isInteracting = false;
            dialogueUI?.Close();
            _player?.EnterFreeRoam();
            _player = null;
        }
    }
}