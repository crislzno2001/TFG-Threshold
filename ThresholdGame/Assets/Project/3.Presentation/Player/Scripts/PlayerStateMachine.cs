using UnityEngine;
using CharacterControls;

namespace ThresholdGame.Presentation.Player
{
    public sealed class PlayerStateMachine : MonoBehaviour
    {
        [SerializeField] private ThirdPersonController _locomotion;
        [SerializeField] private PlayerAnimationDriver _animationDriver;

        public PlayerFreeRoamState FreeRoamState { get; private set; }
        public PlayerDialogueState DialogueState { get; private set; }

        public ThirdPersonController Locomotion => _locomotion;
        public PlayerAnimationDriver AnimationDriver => _animationDriver;

        private PlayerBaseState _currentState;

        private void Reset()
        {
            if (_locomotion == null)
                _locomotion = GetComponent<ThirdPersonController>();

            if (_animationDriver == null)
                _animationDriver = GetComponent<PlayerAnimationDriver>();
        }

        private void Awake()
        {
            if (_locomotion == null)
                _locomotion = GetComponent<ThirdPersonController>();

            if (_animationDriver == null)
                _animationDriver = GetComponent<PlayerAnimationDriver>();

            FreeRoamState = new PlayerFreeRoamState(this);
            DialogueState = new PlayerDialogueState(this);
        }

        private void Start()
        {
            TransitionTo(FreeRoamState);
        }

        private void Update()
        {
            _currentState?.Update();
        }

        public void EnterFreeRoam() => TransitionTo(FreeRoamState);
        public void EnterDialogue() => TransitionTo(DialogueState);

        public void TransitionTo(PlayerBaseState newState)
        {
            if (newState == null || _currentState == newState)
                return;

            _currentState?.Exit();
            _currentState = newState;
            _currentState.Enter();
        }
    }
}