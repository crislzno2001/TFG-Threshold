using UnityEngine;
using UnityEngine.InputSystem;

namespace Sprout.Presentation
{
    /// <summary>
    /// Permite que un personaje (la florista o un NPC) se SIENTE o se TUMBE. Activa el bool "Sit"/"Lie"
    /// de su Animator, lo coloca en el punto del asiento y bloquea su movimiento mientras está sentado.
    ///
    /// Ponlo en la RAÍZ del personaje (donde está su CharacterController / su script de movimiento). El
    /// Animator lo busca en los hijos. Requiere que el Animator tenga los bools 'Sit' y 'Lie'.
    /// </summary>
    public sealed class CharacterSitController : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private CharacterController controller;
        [Tooltip("Script de movimiento con SetControlEnabled(bool) (AnimalCrossingLocomotion). Se autodetecta.")]
        [SerializeField] private MonoBehaviour locomotion;

        [Header("Sentarse al arrancar (para NPCs SIEMPRE sentados, p. ej. el conductor)")]
        [Tooltip("Si está activo, este personaje se sienta solo al empezar (y NO se levanta con WASD).")]
        [SerializeField] private bool sitOnStart = false;
        [SerializeField] private bool sitOnStartLying = false;
        [Tooltip("Opcional: dónde se sienta al arrancar. Vacío = donde ya está posado.")]
        [SerializeField] private Transform sitOnStartAnchor;

        private static readonly int SitId = Animator.StringToHash("Sit");
        private static readonly int LieId = Animator.StringToHash("Lie");

        private bool _seated;
        private bool _lying;
        private bool _lockStand;   // sentado "bloqueado": no se levanta con WASD (conductor, viaje en coche)

        public bool IsSeated => _seated;

        private void Start()
        {
            if (sitOnStart)
                SitAt(sitOnStartAnchor != null ? sitOnStartAnchor : transform, sitOnStartLying, true);
        }

        private void Update()
        {
            // Si está sentada/tumbada y la jugadora pulsa una dirección (WASD/flechas), se levanta y anda.
            // Salvo si es un sentado bloqueado (conductor / viaje en coche).
            if (!_seated || _lockStand) return;
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.wKey.isPressed || kb.aKey.isPressed || kb.sKey.isPressed || kb.dKey.isPressed ||
                kb.upArrowKey.isPressed || kb.downArrowKey.isPressed ||
                kb.leftArrowKey.isPressed || kb.rightArrowKey.isPressed)
                StandUp();
        }

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (controller == null) controller = GetComponent<CharacterController>();
            if (locomotion == null)
                foreach (var mb in GetComponents<MonoBehaviour>())
                    if (mb != null && mb.GetType().GetMethod("SetControlEnabled", new[] { typeof(bool) }) != null)
                    { locomotion = mb; break; }
        }

        /// <summary>Sienta (lie=false) o tumba (lie=true) al personaje en el punto 'anchor'.
        /// lockStand = true → no se levantará con WASD (para el conductor o el viaje en coche).</summary>
        public void SitAt(Transform anchor, bool lie, bool lockStand = false)
        {
            if (_seated || animator == null) return;
            _seated = true;
            _lying = lie;
            _lockStand = lockStand;

            SetControl(false);                                   // bloquea el movimiento
            // Solo desactiva el CharacterController si es el de ESTE objeto (no el del player si está mal asignado).
            if (controller != null && controller.gameObject == gameObject) controller.enabled = false;
            if (anchor != null) transform.SetPositionAndRotation(anchor.position, anchor.rotation);

            // Exactamente UNO en true: si están los dos, el Animator se queda tieso.
            animator.SetBool(SitId, !lie);
            animator.SetBool(LieId, lie);
        }

        public void StandUp()
        {
            if (!_seated) return;
            if (animator != null)
            {
                animator.SetBool(SitId, false);   // los dos a false al levantarse
                animator.SetBool(LieId, false);
            }
            if (controller != null && controller.gameObject == gameObject) controller.enabled = true;
            SetControl(true);
            _seated = false;
            _lockStand = false;
        }

        /// <summary>Sienta si está de pie; levanta si ya está sentado (para usar con la misma tecla).</summary>
        public void Toggle(Transform anchor, bool lie)
        {
            if (_seated) StandUp();
            else SitAt(anchor, lie);
        }

        private void SetControl(bool enabled)
        {
            if (locomotion == null) return;
            locomotion.GetType().GetMethod("SetControlEnabled", new[] { typeof(bool) })
                     ?.Invoke(locomotion, new object[] { enabled });
        }
    }
}
