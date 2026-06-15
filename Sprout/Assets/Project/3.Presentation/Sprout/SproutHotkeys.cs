using UnityEngine;
using UnityEngine.InputSystem;
using OpenAI.Dialogue;
using Sprout.Application;

namespace Sprout.Presentation
{
    /// <summary>
    /// Global gameplay hotkeys that work with the project's own player (which only
    /// handles movement/interaction): C toggles the bouquet crafting panel, R rests
    /// (advances the day phase). Suppressed while a dialogue is open. Lives on the
    /// SproutGame hub so it doesn't depend on the player implementation.
    /// </summary>
    public class SproutHotkeys : MonoBehaviour
    {
        [SerializeField] private GameObject craftingPanel;
        [SerializeField] private DayCycleService dayCycle;
        [Tooltip("If set, hotkeys are ignored while this dialogue is open.")]
        [SerializeField] private DialogueUI dialogueUI;

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (dialogueUI != null && dialogueUI.IsOpen) return;

            if (kb.cKey.wasPressedThisFrame && craftingPanel != null)
                craftingPanel.SetActive(!craftingPanel.activeSelf);

            if (kb.rKey.wasPressedThisFrame && dayCycle != null)
                dayCycle.AdvancePhase();
        }
    }
}
