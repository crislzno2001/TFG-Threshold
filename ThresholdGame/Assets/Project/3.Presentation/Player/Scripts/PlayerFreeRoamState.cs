using UnityEngine;

namespace ThresholdGame.Presentation.Player
{
    public sealed class PlayerFreeRoamState : PlayerBaseState
    {
        public PlayerFreeRoamState(PlayerStateMachine sm) : base(sm) { }

        public override void Enter()
        {
            StateMachine.Locomotion?.SetControlEnabled(true);
            StateMachine.AnimationDriver?.ResumeAutomaticAnimation();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public override void Update() { }
        public override void Exit() { }
    }
}