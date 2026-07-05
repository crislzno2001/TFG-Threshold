using UnityEngine;
using OpenAI.Dialogue;

namespace Sprout.Presentation
{
    /// <summary>
    /// Keeps the mouse cursor free and visible whenever a Sprout UI panel is open
    /// (dialogue, crafting, ending), every frame — overriding anything that tries to
    /// re-lock it (e.g. CharacterInputs locking the cursor on window focus, or the
    /// free-roam state). When the UI closes it restores the locked/hidden cursor once,
    /// so free-roam keeps the project's original behaviour.
    /// </summary>
    public class SproutCursorGuard : MonoBehaviour
    {
        [SerializeField] private GameObject craftingPanel;
        [SerializeField] private GameObject endingPanel;
        [SerializeField] private GameObject nightPanel;

        private bool _wasOpen;

        private void Update()
        {
            bool open =
                DialogueUI.Active != null ||
                ScriptedDialogue.AnyActive ||   // intro del coche / tutoriales guionizados con opciones
                (craftingPanel != null && craftingPanel.activeInHierarchy) ||
                (endingPanel != null && endingPanel.activeInHierarchy) ||
                (nightPanel != null && nightPanel.activeInHierarchy);

            if (open)
            {
                if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;
                if (!Cursor.visible) Cursor.visible = true;
            }
            else if (_wasOpen)
            {
                // Restore free-roam cursor once when the last UI panel closes.
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            _wasOpen = open;
        }
    }
}
