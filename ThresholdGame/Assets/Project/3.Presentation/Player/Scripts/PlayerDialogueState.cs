using UnityEngine;

namespace ThresholdGame.Presentation.Player
{
    /// <summary>
    /// Estado activo durante el diálogo con un NPC.
    /// La salida la gestiona NPCInteractionTrigger.CloseDialogue().
    /// </summary>
    public sealed class PlayerDialogueState : PlayerBaseState
    {
        public PlayerDialogueState(PlayerStateMachine sm) : base(sm) { }

        public override void Enter()
        {
            StateMachine.Locomotion?.SetControlEnabled(false);
            StateMachine.AnimationDriver?.ForceIdle();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public override void Update() { }
        public override void Exit() { }
    }
}