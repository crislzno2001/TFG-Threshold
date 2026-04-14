using UnityEngine;

namespace ThresholdGame.Presentation.Player
{
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