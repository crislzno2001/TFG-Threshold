using UnityEngine;
using CharacterControls;

namespace ThresholdGame.Presentation.Player
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(ThirdPersonController))]
    public class PlayerAnimationDriver : MonoBehaviour
    {
        [SerializeField] private ThirdPersonController locomotion;
        [SerializeField] private Animator animator;

        private int animIDSpeed;
        private int animIDGrounded;
        private int animIDJump;
        private int animIDFreeFall;
        private int animIDMotionSpeed;

        private bool forceIdle;

        private void Reset()
        {
            if (locomotion == null)
                locomotion = GetComponent<ThirdPersonController>();

            if (animator == null)
                animator = GetComponent<Animator>();
        }

        private void Awake()
        {
            if (locomotion == null)
                locomotion = GetComponent<ThirdPersonController>();

            if (animator == null)
                animator = GetComponent<Animator>();

            animIDSpeed = Animator.StringToHash("Speed");
            animIDGrounded = Animator.StringToHash("Grounded");
            animIDJump = Animator.StringToHash("Jump");
            animIDFreeFall = Animator.StringToHash("FreeFall");
            animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        private void Update()
        {
            if (animator == null || locomotion == null)
                return;

            if (forceIdle)
            {
                animator.SetBool(animIDGrounded, locomotion.Grounded);
                animator.SetFloat(animIDSpeed, 0f);
                animator.SetFloat(animIDMotionSpeed, 0f);
                animator.SetBool(animIDJump, false);
                animator.SetBool(animIDFreeFall, false);
                return;
            }

            animator.SetBool(animIDGrounded, locomotion.Grounded);
            animator.SetFloat(animIDSpeed, locomotion.AnimationBlend);
            animator.SetFloat(animIDMotionSpeed, locomotion.InputMagnitude);

            bool isJumping = !locomotion.Grounded && locomotion.VerticalVelocity > 0.1f;
            bool isFalling = !locomotion.Grounded && locomotion.VerticalVelocity < -0.1f;

            animator.SetBool(animIDJump, isJumping);
            animator.SetBool(animIDFreeFall, isFalling);
        }

        public void ForceIdle()
        {
            forceIdle = true;

            if (animator == null) return;

            animator.SetFloat(animIDSpeed, 0f);
            animator.SetFloat(animIDMotionSpeed, 0f);
            animator.SetBool(animIDJump, false);
            animator.SetBool(animIDFreeFall, false);
        }

        public void ResumeAutomaticAnimation()
        {
            forceIdle = false;
        }
    }
}