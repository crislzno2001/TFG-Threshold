using UnityEngine;

namespace ThresholdGame.Presentation.Player
{
    /// <summary>
    /// Estado del jugador cuando el juego está pausado.
    /// Bloquea movimiento Y cámara (a diferencia del DialogueState
    /// que mantiene la cámara para mirar al NPC).
    /// </summary>
    public sealed class PlayerPausedState : PlayerBaseState
    {
        public PlayerPausedState(PlayerStateMachine sm) : base(sm) { }

        public override void Enter()
        {
            // Bloquea movimiento + cámara + limpia inputs
            StateMachine.Locomotion?.SetControlEnabled(false);
            StateMachine.AnimationDriver?.ForceIdle();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public override void Update() { }
        public override void Exit() { }
    }
}