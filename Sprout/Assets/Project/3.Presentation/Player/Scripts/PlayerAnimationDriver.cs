using UnityEngine;
using ThresholdGame.Presentation.Player.Locomotion;

namespace ThresholdGame.Presentation.Player
{
    /// <summary>
    /// Lee el estado de locomoción a través de ILocomotionProvider
    /// y lo traduce a parámetros del Animator.
    /// 
    /// Al depender de la interfaz y no de un controller concreto,
    /// este driver funciona con cualquier implementación de movimiento
    /// (ThirdPerson, AnimalCrossing, futuro Top-Down, etc.) sin cambios.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public sealed class PlayerAnimationDriver : MonoBehaviour
    {
        [Header("Dependencias")]
        [Tooltip("Componente que implementa ILocomotionProvider. " +
                 "Arrastra aquí el script de movimiento del Player.")]
        [SerializeField] private MonoBehaviour locomotionSource;
        [SerializeField] private Animator animator;

        private ILocomotionProvider _locomotion;

        // IDs de parámetros del Animator (cacheados para rendimiento)
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

        private bool _forceIdle;

        private void Reset()
        {
            if (animator == null) animator = GetComponent<Animator>();
        }

        private void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();

            // Validación + cast de la dependencia
            _locomotion = locomotionSource as ILocomotionProvider;
            if (_locomotion == null)
            {
                Debug.LogError(
                    $"[PlayerAnimationDriver] El campo 'locomotionSource' debe " +
                    $"implementar ILocomotionProvider. Asignado: {locomotionSource}",
                    this
                );
            }

            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        private void Update()
        {
            if (animator == null || _locomotion == null) return;

            if (_forceIdle)
            {
                animator.SetBool(_animIDGrounded, _locomotion.Grounded);
                animator.SetFloat(_animIDSpeed, 0f);
                animator.SetFloat(_animIDMotionSpeed, 0f);
                animator.SetBool(_animIDJump, false);
                animator.SetBool(_animIDFreeFall, false);
                return;
            }

            animator.SetBool(_animIDGrounded, _locomotion.Grounded);
            animator.SetFloat(_animIDSpeed, _locomotion.AnimationBlend);
            animator.SetFloat(_animIDMotionSpeed, _locomotion.InputMagnitude);

            bool isJumping = !_locomotion.Grounded && _locomotion.VerticalVelocity > 0.1f;
            bool isFalling = !_locomotion.Grounded && _locomotion.VerticalVelocity < -0.1f;

            animator.SetBool(_animIDJump, isJumping);
            animator.SetBool(_animIDFreeFall, isFalling);
        }

        public void ForceIdle()
        {
            _forceIdle = true;
            if (animator == null) return;

            animator.SetFloat(_animIDSpeed, 0f);
            animator.SetFloat(_animIDMotionSpeed, 0f);
            animator.SetBool(_animIDJump, false);
            animator.SetBool(_animIDFreeFall, false);
        }

        public void ResumeAutomaticAnimation() => _forceIdle = false;
    }
}