using UnityEngine;
using UnityEngine.InputSystem;

namespace ThresholdGame.Core.GameFlow
{
    /// <summary>
    /// Escucha el input de pausa (Escape) y delega en GameStateMachine.
    /// Col�calo en el mismo GameObject que GameStateMachine.
    /// Separado para respetar SRP: la FSM no sabe nada de input.
    /// </summary>
    public sealed class PauseInputHandler : MonoBehaviour
    {
        private void Update()
        {
            if (GameStateMachine.Instance == null) return;

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                // If a conversation is open, Esc closes it instead of pausing.
                if (OpenAI.Dialogue.DialogueUI.Active != null)
                {
                    OpenAI.Dialogue.DialogueUI.Active.RequestClose();
                    return;
                }

                if (GameStateMachine.Instance.IsPaused)
                    GameStateMachine.Instance.Resume();
                else if (GameStateMachine.Instance.IsPlaying)
                    GameStateMachine.Instance.Pause();
            }
        }
    }
}