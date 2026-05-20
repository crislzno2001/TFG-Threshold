using UnityEngine;
using ThresholdGame.Presentation.Player.Locomotion;

namespace ThresholdGame.Presentation.Player.Audio
{
    /// <summary>
    /// Reproduce sonidos de pasos y aterrizaje del jugador.
    /// 
    /// Se invoca desde Animation Events ya existentes en los clips del
    /// modelo (OnFootstep, OnLand) — los mismos que usaban los Starter Assets.
    /// 
    /// Lee el estado de suelo desde ILocomotionProvider para evitar
    /// reproducir pasos cuando el personaje está en el aire (defensa
    /// frente a Animation Events disparados durante caídas).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerFootstepAudio : MonoBehaviour
    {
        [Header("Dependencias")]
        [Tooltip("Componente que implementa ILocomotionProvider. " +
                 "Se usa para validar que el personaje está en el suelo.")]
        [SerializeField] private MonoBehaviour locomotionSource;

        [Header("Footsteps")]
        [Tooltip("Clips que se reproducen aleatoriamente al pisar.")]
        [SerializeField] private AudioClip[] footstepClips;

        [Range(0f, 1f)]
        [SerializeField] private float footstepVolume = 0.5f;

        [Header("Landing")]
        [Tooltip("Clip que se reproduce al aterrizar tras un salto/caída.")]
        [SerializeField] private AudioClip landingClip;

        [Range(0f, 1f)]
        [SerializeField] private float landingVolume = 0.5f;

        private CharacterController _characterController;
        private ILocomotionProvider _locomotion;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _locomotion = locomotionSource as ILocomotionProvider;

            if (_locomotion == null)
            {
                Debug.LogWarning(
                    "[PlayerFootstepAudio] LocomotionSource no implementa " +
                    "ILocomotionProvider. Los pasos se reproducirán sin " +
                    "validación de suelo.",
                    this
                );
            }
        }

        // ── Animation Events ─────────────────────────────────────────────
        // Estos métodos se llaman desde los clips del Animator.
        // El parámetro AnimationEvent es el que envía Unity automáticamente.

        /// <summary>
        /// Invocado por Animation Event "OnFootstep" en los clips de movimiento.
        /// </summary>
        private void OnFootstep(AnimationEvent animationEvent)
        {
            // Filtros de seguridad:
            // 1. Solo reproducir si el clip tiene peso significativo (evita
            //    pasos fantasma cuando hay blends entre animaciones).
            if (animationEvent.animatorClipInfo.weight <= 0.5f) return;

            // 2. Validar que estamos en el suelo (defensa extra).
            if (_locomotion != null && !_locomotion.Grounded) return;

            if (footstepClips == null || footstepClips.Length == 0) return;

            int index = Random.Range(0, footstepClips.Length);
            AudioClip clip = footstepClips[index];
            if (clip == null) return;

            AudioSource.PlayClipAtPoint(
                clip,
                transform.TransformPoint(_characterController.center),
                footstepVolume
            );
        }

        /// <summary>
        /// Invocado por Animation Event "OnLand" en el clip de aterrizaje.
        /// </summary>
        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight <= 0.5f) return;
            if (landingClip == null) return;

            AudioSource.PlayClipAtPoint(
                landingClip,
                transform.TransformPoint(_characterController.center),
                landingVolume
            );
        }
    }
}