using UnityEngine;
using UnityEngine.InputSystem;
using ThresholdGame.Presentation.Interaction;

namespace ThresholdGame.Presentation.Player
{
    /// <summary>
    /// Estado normal del jugador en el mundo.
    /// �nico responsable de leer el input de interacci�n (E).
    /// No sabe qu� hay en rango � delega en IInteractable.Interact().
    /// </summary>
    public sealed class PlayerFreeRoamState : PlayerBaseState
    {
        private readonly InteractionDetector _detector;

        public PlayerFreeRoamState(PlayerStateMachine sm) : base(sm)
        {
            _detector = sm.GetComponent<InteractionDetector>();
        }

        public override void Enter()
        {
            StateMachine.SetMovementEnabled(true);   // reactiva TODO el movimiento al volver al mundo
            StateMachine.AnimationDriver?.ResumeAutomaticAnimation();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        public override void Update()
        {
            if (Keyboard.current == null) return;

            if (!Keyboard.current.eKey.wasPressedThisFrame) return;

            Debug.Log("[PlayerFreeRoamState] E pulsada");

            if (_detector == null)
            {
                Debug.LogWarning("[PlayerFreeRoamState] No hay InteractionDetector en el mismo GameObject que PlayerStateMachine.");
                return;
            }

            if (_detector.Current == null)
            {
                Debug.LogWarning("[PlayerFreeRoamState] E pulsada, pero no hay interactuable en rango.");
                return;
            }

            Debug.Log($"[PlayerFreeRoamState] Interactuando con: {_detector.Current.InteractionLabel}");

            _detector.Current.Interact(StateMachine);
        }

        public override void Exit() { }
    }
}