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
        [SerializeField] private string playerTag = "Player";

        private Transform _player;

        private Transform Player()
        {
            if (_player == null) { var p = GameObject.FindGameObjectWithTag(playerTag); if (p != null) _player = p.transform; }
            return _player;
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (dialogueUI != null && dialogueUI.IsOpen) return;

            // C abre/cierra el crafteo. Si hay mesas de crafteo en la escena, solo cerca de una.
            if (kb.cKey.wasPressedThisFrame && craftingPanel != null)
            {
                bool allowed = !CraftingStation.Any || CraftingStation.AnyNear(Player());
                if (allowed) craftingPanel.SetActive(!craftingPanel.activeSelf);
            }

            // Si te alejas de la mesa con el panel abierto, se cierra solo.
            if (craftingPanel != null && craftingPanel.activeSelf && CraftingStation.Any && !CraftingStation.AnyNear(Player()))
                craftingPanel.SetActive(false);

            if (kb.rKey.wasPressedThisFrame && dayCycle != null)
                dayCycle.AdvancePhase();
        }
    }
}
