using UnityEngine;
using UnityEngine.InputSystem;

namespace ThresholdGame.Presentation.Player
{
    /// <summary>
    /// Estado activo mientras el jugador inspecciona un objeto.
    /// E/ESC cierran la InspectUI, cuyo callback llama EnterFreeRoam().
    /// </summary>
    public sealed class PlayerInspectState : PlayerBaseState
    {
        public PlayerInspectState(PlayerStateMachine sm) : base(sm) { }

        public override void Enter()
        {
            StateMachine.Locomotion?.SetControlEnabled(false);
            StateMachine.AnimationDriver?.ForceIdle();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public override void Update()
        {
            if (Keyboard.current == null) return;

            if (Keyboard.current.eKey.wasPressedThisFrame ||
                Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                // InspectUI.Close() llama el callback player.EnterFreeRoam()
                StateMachine.InspectUI?.Close();
            }
        }

        public override void Exit() { }
    }
}