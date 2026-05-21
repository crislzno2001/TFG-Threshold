using UnityEngine;
using ThresholdGame.Core.Interaction;
using ThresholdGame.Presentation.Player.Locomotion;
using ThresholdGame.Presentation.UI;

namespace ThresholdGame.Presentation.Player
{
    /// <summary>
    /// Máquina de estados del jugador.
    /// 
    /// Implementa IPlayerController para que los IInteractable puedan
    /// cambiar su estado sin conocer esta clase concreta.
    /// 
    /// Depende de ILocomotionProvider (no de un controller concreto),
    /// lo que permite intercambiar implementaciones de movimiento
    /// (AnimalCrossing, TopDown, etc.) sin tocar esta clase.
    /// </summary>
    public sealed class PlayerStateMachine : MonoBehaviour, IPlayerController
    {
        [Header("Componentes")]
        [Tooltip("Componente que implementa ILocomotionProvider.")]
        [SerializeField] private MonoBehaviour _locomotionSource;
        [SerializeField] private PlayerAnimationDriver _animationDriver;

        [Header("UI")]
        [SerializeField] private InspectUI _inspectUI;

        // Estados
        public PlayerFreeRoamState FreeRoamState { get; private set; }
        public PlayerDialogueState DialogueState { get; private set; }
        public PlayerInspectState InspectState { get; private set; }
        public PlayerPausedState PausedState { get; private set; }

        // Accesores expuestos a los estados
        public ILocomotionProvider Locomotion { get; private set; }
        public PlayerAnimationDriver AnimationDriver => _animationDriver;
        public InspectUI InspectUI => _inspectUI;

        private PlayerBaseState _currentState;
        private PlayerBaseState _stateBeforePause;

        // ── Unity ──────────────────────────────────────────────────────────

        private void Reset()
        {
            if (_animationDriver == null)
                _animationDriver = GetComponent<PlayerAnimationDriver>();
        }

        private void Awake()
        {
            // Resolución de la dependencia de locomoción vía interfaz
            Locomotion = _locomotionSource as ILocomotionProvider;
            if (Locomotion == null)
            {
                Debug.LogError(
                    $"[PlayerStateMachine] El campo 'LocomotionSource' debe " +
                    $"implementar ILocomotionProvider. Asignado: {_locomotionSource}",
                    this
                );
            }

            if (_animationDriver == null)
                _animationDriver = GetComponent<PlayerAnimationDriver>();

            // Instanciación de los estados
            FreeRoamState = new PlayerFreeRoamState(this);
            DialogueState = new PlayerDialogueState(this);
            InspectState = new PlayerInspectState(this);
            PausedState = new PlayerPausedState(this);
        }

        private void Start() => TransitionTo(FreeRoamState);

        private void Update() => _currentState?.Update();

        // ── IPlayerController ──────────────────────────────────────────────

        public void EnterFreeRoam() => TransitionTo(FreeRoamState);
        public void EnterDialogue() => TransitionTo(DialogueState);
        public void EnterInspect() => TransitionTo(InspectState);

        // ── Pausa (apilable: recuerda el estado anterior) ─────────────────

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
        /// Restaura el estado anterior a la pausa.
        /// Llamado por un GameEventListener escuchando Ev_ResumePlaying.
        /// </summary>
        public void ResumeFromPause()
        {
            if (_currentState != PausedState) return;
            TransitionTo(_stateBeforePause ?? FreeRoamState);
            _stateBeforePause = null;
        }

        // ── Transición interna ─────────────────────────────────────────────

        private void TransitionTo(PlayerBaseState newState)
        {
            if (newState == null) return;

            _currentState?.Exit();
            _currentState = newState;
            _currentState.Enter();
        }
    }
}