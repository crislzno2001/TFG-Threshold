using UnityEngine;

namespace ThresholdGame.Presentation.Player
{
    /// <summary>
    /// FSM de animación del jugador, desacoplada de ThirdPersonController.
    ///
    /// Lee únicamente la velocidad del CharacterController y el flag Grounded
    /// para transicionar entre estados. No conoce inputs ni lógica de movimiento.
    ///
    /// Requisitos del Animator:
    ///   Parámetros float : Speed, MotionSpeed
    ///   Parámetros bool  : Grounded, Jump, FreeFall
    ///   (mismos nombres que el Animator generado por los Starter Assets)
    ///
    /// Para usar:
    ///   1. Añadir este componente al GameObject del jugador.
    ///   2. ThirdPersonController puede seguir existiendo; este componente es aditivo.
    ///   3. Si quieres migrar completamente, desactiva la lógica de animator en TPC.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerAnimatorController : MonoBehaviour
    {
        // ── Configuración pública ─────────────────────────────────────────────

        [Header("Velocidades de referencia")]
        [Tooltip("Velocidad a partir de la cual se considera sprint (m/s)")]
        [SerializeField] private float _sprintThreshold = 4f;

        [Tooltip("Velocidad mínima para considerar que hay movimiento")]
        [SerializeField] private float _movementThreshold = 0.1f;

        [Tooltip("Suavizado del parámetro Speed hacia el Animator")]
        [SerializeField] private float _animationDamping = 10f;

        [Header("Ground check")]
        [Tooltip("Offset vertical del centro de la esfera de grounded check")]
        [SerializeField] private float _groundedOffset = -0.14f;

        [SerializeField] private float _groundedRadius = 0.28f;

        [SerializeField] private LayerMask _groundLayers;

        [Header("Caída libre")]
        [Tooltip("Segundos en el aire antes de activar FreeFall")]
        [SerializeField] private float _freeFallTimeout = 0.15f;

        // ── FSM interna ───────────────────────────────────────────────────────

        private enum AnimState
        {
            Idle,
            Walking,
            Running,
            Jumping,
            FreeFalling,
            Landing
        }

        private AnimState _currentState = AnimState.Idle;

        // ── Referencias ───────────────────────────────────────────────────────

        private Animator          _animator;
        private CharacterController _cc;

        // ── IDs de parámetros (cache para evitar string lookup en Update) ──────

        private static readonly int SpeedId       = Animator.StringToHash("Speed");
        private static readonly int MotionSpeedId = Animator.StringToHash("MotionSpeed");
        private static readonly int GroundedId    = Animator.StringToHash("Grounded");
        private static readonly int JumpId        = Animator.StringToHash("Jump");
        private static readonly int FreeFallId    = Animator.StringToHash("FreeFall");

        // ── Estado runtime ────────────────────────────────────────────────────

        private bool  _isGrounded;
        private float _airTime;
        private float _animatedSpeed;   // valor suavizado enviado al Animator

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _cc       = GetComponent<CharacterController>();
        }

        private void Update()
        {
            UpdateGroundedCheck();
            UpdateFSM();
            PushParametersToAnimator();
        }

        // ── Ground check ──────────────────────────────────────────────────────

        private void UpdateGroundedCheck()
        {
            var spherePos = new Vector3(
                transform.position.x,
                transform.position.y - _groundedOffset,
                transform.position.z);

            _isGrounded = Physics.CheckSphere(
                spherePos,
                _groundedRadius,
                _groundLayers,
                QueryTriggerInteraction.Ignore);
        }

        // ── FSM principal ─────────────────────────────────────────────────────

        private void UpdateFSM()
        {
            float horizontalSpeed = new Vector3(_cc.velocity.x, 0f, _cc.velocity.z).magnitude;
            float verticalVelocity = _cc.velocity.y;

            switch (_currentState)
            {
                case AnimState.Idle:
                    if (!_isGrounded)
                    {
                        Transition(verticalVelocity > 0f ? AnimState.Jumping : AnimState.FreeFalling);
                        break;
                    }
                    if (horizontalSpeed > _movementThreshold)
                        Transition(horizontalSpeed >= _sprintThreshold ? AnimState.Running : AnimState.Walking);
                    break;

                case AnimState.Walking:
                    if (!_isGrounded)
                    {
                        Transition(verticalVelocity > 0f ? AnimState.Jumping : AnimState.FreeFalling);
                        break;
                    }
                    if (horizontalSpeed < _movementThreshold)       Transition(AnimState.Idle);
                    else if (horizontalSpeed >= _sprintThreshold)   Transition(AnimState.Running);
                    break;

                case AnimState.Running:
                    if (!_isGrounded)
                    {
                        Transition(verticalVelocity > 0f ? AnimState.Jumping : AnimState.FreeFalling);
                        break;
                    }
                    if (horizontalSpeed < _movementThreshold)       Transition(AnimState.Idle);
                    else if (horizontalSpeed < _sprintThreshold)    Transition(AnimState.Walking);
                    break;

                case AnimState.Jumping:
                    if (_isGrounded)
                    {
                        Transition(AnimState.Landing);
                        break;
                    }
                    if (verticalVelocity < 0f)
                        Transition(AnimState.FreeFalling);
                    break;

                case AnimState.FreeFalling:
                    _airTime += Time.deltaTime;
                    if (_isGrounded)
                        Transition(AnimState.Landing);
                    break;

                case AnimState.Landing:
                    // Un frame de landing para que el Animator dispare la transición
                    Transition(horizontalSpeed > _movementThreshold
                        ? (horizontalSpeed >= _sprintThreshold ? AnimState.Running : AnimState.Walking)
                        : AnimState.Idle);
                    break;
            }

            // Suavizado del speed para el blend tree
            float targetAnimSpeed = _isGrounded ? horizontalSpeed : 0f;
            _animatedSpeed = Mathf.Lerp(_animatedSpeed, targetAnimSpeed, Time.deltaTime * _animationDamping);
            if (_animatedSpeed < 0.01f) _animatedSpeed = 0f;
        }

        private void Transition(AnimState nextState)
        {
            if (_currentState == nextState) return;

            // Reset air timer al aterrizar
            if (nextState == AnimState.Idle ||
                nextState == AnimState.Walking ||
                nextState == AnimState.Running ||
                nextState == AnimState.Landing)
            {
                _airTime = 0f;
            }

            _currentState = nextState;
        }

        // ── Push a Animator ───────────────────────────────────────────────────

        private void PushParametersToAnimator()
        {
            bool isJumping   = _currentState == AnimState.Jumping;
            bool isFreeFall  = _currentState == AnimState.FreeFalling && _airTime >= _freeFallTimeout;

            // MotionSpeed: 1 si hay input (aproximado por velocidad horizontal)
            float motionSpeed = _animatedSpeed > _movementThreshold ? 1f : 0f;

            _animator.SetFloat(SpeedId,       _animatedSpeed);
            _animator.SetFloat(MotionSpeedId, motionSpeed);
            _animator.SetBool(GroundedId,    _isGrounded);
            _animator.SetBool(JumpId,        isJumping);
            _animator.SetBool(FreeFallId,    isFreeFall);
        }

        // ── Gizmos ────────────────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = _isGrounded
                ? new Color(0f, 1f, 0f, 0.35f)
                : new Color(1f, 0f, 0f, 0.35f);

            Gizmos.DrawSphere(
                new Vector3(transform.position.x,
                            transform.position.y - _groundedOffset,
                            transform.position.z),
                _groundedRadius);
        }
#endif
    }
}
