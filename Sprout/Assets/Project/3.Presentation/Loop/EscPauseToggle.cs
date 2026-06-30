using UnityEngine;
using UnityEngine.InputSystem;
using ThresholdGame.Core.GameFlow;
using ThresholdGame.Presentation.UI.Pause;

namespace Sprout.Presentation
{
    /// <summary>
    /// Arregla la pausa con ESC (Input System nuevo). Si existe el GameStateMachine, usa su Pause()/Resume()
    /// (así el botón "Resume" del menú sigue funcionando). Si no existe ---por ejemplo, al probar la
    /// GameScene suelta sin pasar por el Bootstrap---, abre/cierra el PauseMenu directamente como plan B.
    ///
    /// Si hay un diálogo abierto, ESC no pausa. Ponlo en cualquier objeto de la escena de juego.
    /// </summary>
    public sealed class EscPauseToggle : MonoBehaviour
    {
        private bool _fallbackPaused;

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || !kb.escapeKey.wasPressedThisFrame) return;
            if (OpenAI.Dialogue.DialogueUI.Active != null) return;

            var gsm = GameStateMachine.Instance;
            if (gsm != null)
            {
                if (gsm.IsPaused) gsm.Resume();
                else gsm.Pause();
                return;
            }

            // Plan B (sin GameStateMachine): abrir/cerrar el menú de pausa directamente.
            var pause = FindFirstObjectByType<PauseMenuController>();
            if (pause == null) return;
            _fallbackPaused = !_fallbackPaused;
            if (_fallbackPaused)
            {
                pause.Show(); Time.timeScale = 0f;
                Cursor.lockState = CursorLockMode.None; Cursor.visible = true;   // poder clicar el menú
            }
            else
            {
                pause.Hide(); Time.timeScale = 1f;
                Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
            }
        }
    }
}
