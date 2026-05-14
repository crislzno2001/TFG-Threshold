using UnityEngine;
using CharacterControls;
using ThresholdGame.Core.Interaction;
using ThresholdGame.Presentation.UI;

namespace ThresholdGame.Presentation.Player
{
    /// <summary>
    /// Máquina de estados del jugador.
    /// Implementa IPlayerController para que los IInteractable puedan cambiar
    /// su estado sin conocer esta clase concreta.
    /// </summary>
    public sealed class PlayerStateMachine : MonoBehaviour, IPlayerController
    {
        [Header("Componentes")]
        [SerializeField] private ThirdPersonController _locomotion;
        [SerializeField] private PlayerAnimationDriver _animationDriver;

        [Header("UI")]
        [SerializeField] private InspectUI _inspectUI;

        public PlayerFreeRoamState FreeRoamState { get; private set; }
        public PlayerDialogueState DialogueState { get; private set; }
        public PlayerInspectState InspectState { get; private set; }
        public PlayerPausedState PausedState { get; private set; }

        public ThirdPersonController Locomotion => _locomotion;
        public PlayerAnimationDriver AnimationDriver => _animationDriver;
        public InspectUI InspectUI => _inspectUI;

        private PlayerBaseState _currentState;
        private PlayerBaseState _stateBeforePause;

        // ── Unity ──────────────────────────────────────────────────────────────

        private void Reset()
        {
            if (_locomotion == null) _locomotion = GetComponent<ThirdPersonController>();
            if (_animationDriver == null) _animationDriver = GetComponent<PlayerAnimationDriver>();
        }

        private void Awake()
        {
            if (_locomotion == null) _locomotion = GetComponent<ThirdPersonController>();
            if (_animationDriver == null) _animationDriver = GetComponent<PlayerAnimationDriver>();

            FreeRoamState = new PlayerFreeRoamState(this);
            DialogueState = new PlayerDialogueState(this);
            InspectState = new PlayerInspectState(this);
            PausedState = new PlayerPausedState(this);
        }

        private void Start() => TransitionTo(FreeRoamState);

        private void Update() => _currentState?.Update();

        // ── IPlayerController ──────────────────────────────────────────────────

        public void EnterFreeRoam() => TransitionTo(FreeRoamState);
        public void EnterDialogue() => TransitionTo(DialogueState);
        public void EnterInspect() => TransitionTo(InspectState);

        // ── Pausa (apilable: recuerda el estado anterior) ─────────────────────

        /// <summary>
        /// Entra en pausa preservando el estado anterior para poder restaurarlo.
        /// Llamado por un GameEventListener escuchando Ev_EnterPaused.
        /// </summary>
        public void EnterPaused()
        {
            if (_currentState == PausedState) return;
            _stateBeforePause = _currentState;
            TransitionTo(PausedState);
        }

        /// <summary>
        /// Restaura el estado previo a la pausa (FreeRoam, Dialogue o Inspect).
        /// Llamado por un GameEventListener escuchando Ev_ResumePlaying.
        /// </summary>
        public void ResumeFromPause()
        {
            if (_stateBeforePause == null)
            {
                TransitionTo(FreeRoamState);
                return;
            }

            TransitionTo(_stateBeforePause);
            _stateBeforePause = null;
        }

        // ── API interna ────────────────────────────────────────────────────────

        public void TransitionTo(PlayerBaseState newState)
        {
            if (newState == null || _currentState == newState) return;

            _currentState?.Exit();
            _currentState = newState;
            _currentState.Enter();
        }
    }
}