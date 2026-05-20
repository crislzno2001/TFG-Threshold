using CharacterControls;
using ThresholdGame.Presentation.Player.Camera;
using UnityEngine;

namespace ThresholdGame.Presentation.Player.Locomotion
{
    /// <summary>
    /// Controlador de movimiento estilo Animal Crossing:
    /// - Movimiento relativo a la cámara fija (W va "arriba" en pantalla).
    /// - Rotación suave del personaje hacia su dirección de movimiento.
    /// - Detección de suelo y gravedad simples.
    /// - NO controla la cámara (responsabilidad de FixedAngleCameraController).
    /// 
    /// Expone su estado a través de ILocomotionProvider para que el
    /// PlayerAnimationDriver pueda leerlo sin acoplamiento concreto.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class AnimalCrossingLocomotion : MonoBehaviour, ILocomotionProvider
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 2.0f;
        [SerializeField] private float sprintSpeed = 5.335f;
        [SerializeField] private float speedChangeRate = 10f;

        [Range(0f, 0.3f)]
        [SerializeField] private float rotationSmoothTime = 0.12f;

        [Header("Gravity")]
        [SerializeField] private float gravity = -15f;

        [Header("Grounded Check")]
        [SerializeField] private float groundedOffset = -0.14f;
        [SerializeField] private float groundedRadius = 0.28f;
        [SerializeField] private LayerMask groundLayers;

        [Header("Camera Reference")]
        [Tooltip("Necesario para calcular movimiento relativo a la cámara.")]
        [SerializeField] private FixedAngleCameraController cameraController;

        [Header("Input Source")]
        [Tooltip("Componente que provee los inputs del jugador.")]
        [SerializeField] private CharacterInputs characterInputs;

        // Estado interno
        private CharacterController _controller;
        private float _speed;
        private float _animationBlend;
        private float _inputMagnitude;
        private float _verticalVelocity;
        private float _rotationVelocity;
        private float _targetRotation;
        private bool _movementEnabled = true;

        // ── ILocomotionProvider ───────────────────────────────────────────

        public float CurrentSpeed => _speed;
        public float AnimationBlend => _animationBlend;
        public float InputMagnitude => _inputMagnitude;
        public float VerticalVelocity => _verticalVelocity;
        public bool Grounded { get; private set; }

        public void SetControlEnabled(bool enabled)
        {
            _movementEnabled = enabled;
            if (!enabled) ForceStop();
        }

        public void ForceStop()
        {
            if (characterInputs != null)
                characterInputs.Clear();

            _speed = 0f;
            _animationBlend = 0f;
            _inputMagnitude = 0f;
            _rotationVelocity = 0f;

            if (Grounded)
                _verticalVelocity = -2f;
        }

        // ── Unity ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            GroundedCheck();
            ApplyGravity();
            Move();
        }

        // ── Movimiento ────────────────────────────────────────────────────

        private void Move()
        {
            Vector2 moveInput = _movementEnabled && characterInputs != null
                ? characterInputs.move
                : Vector2.zero;

            bool sprintPressed = _movementEnabled && characterInputs != null && characterInputs.sprint;

            float targetSpeed = sprintPressed ? sprintSpeed : moveSpeed;
            if (moveInput == Vector2.zero) targetSpeed = 0f;

            _inputMagnitude = moveInput == Vector2.zero ? 0f : 1f;

            // Suavizado de velocidad horizontal
            float currentHorizontalSpeed = new Vector3(
                _controller.velocity.x, 0f, _controller.velocity.z
            ).magnitude;

            const float speedOffset = 0.1f;
            if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                _speed = Mathf.Lerp(
                    currentHorizontalSpeed,
                    targetSpeed * _inputMagnitude,
                    Time.deltaTime * speedChangeRate
                );
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            // Blend de animación
            _animationBlend = Mathf.Lerp(
                _animationBlend,
                targetSpeed * _inputMagnitude,
                Time.deltaTime * speedChangeRate
            );
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            // Dirección relativa a la cámara fija
            Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
            float cameraYaw = cameraController != null ? cameraController.CurrentYaw : 0f;

            if (moveInput != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg
                                  + cameraYaw;

                float rotation = Mathf.SmoothDampAngle(
                    transform.eulerAngles.y,
                    _targetRotation,
                    ref _rotationVelocity,
                    rotationSmoothTime
                );
                transform.rotation = Quaternion.Euler(0f, rotation, 0f);
            }

            Vector3 targetDirection = Quaternion.Euler(0f, _targetRotation, 0f) * Vector3.forward;

            _controller.Move(
                targetDirection.normalized * (_speed * Time.deltaTime) +
                new Vector3(0f, _verticalVelocity, 0f) * Time.deltaTime
            );
        }

        private void ApplyGravity()
        {
            if (Grounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f;
            else
                _verticalVelocity += gravity * Time.deltaTime;
        }

        private void GroundedCheck()
        {
            Vector3 spherePosition = new Vector3(
                transform.position.x,
                transform.position.y - groundedOffset,
                transform.position.z
            );

            Grounded = Physics.CheckSphere(
                spherePosition, groundedRadius, groundLayers,
                QueryTriggerInteraction.Ignore
            );
        }

        private void OnDrawGizmosSelected()
        {
            Color green = new Color(0f, 1f, 0f, 0.35f);
            Color red = new Color(1f, 0f, 0f, 0.35f);
            Gizmos.color = Grounded ? green : red;
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - groundedOffset, transform.position.z),
                groundedRadius
            );
        }
    }
}