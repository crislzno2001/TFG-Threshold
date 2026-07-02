using UnityEngine;
using UnityEngine.AI;

namespace ThresholdGame.Presentation.NPC
{
    /// <summary>
    /// Conduce el Animator de un NPC a partir de su NavMeshAgent, usando LOS MISMOS parámetros
    /// que el Animator Controller de la florista (Speed, Grounded, MotionSpeed). Así, al pasear con
    /// el NavMeshAgent, el NPC reproduce idle (quieto) o andar (moviéndose) automáticamente.
    ///
    /// Puedes ponerlo en el objeto RAÍZ o en el hijo con la malla: busca solo el Animator que
    /// realmente tiene un Controller (ignora Animators vacíos) y el NavMeshAgent hacia arriba.
    /// </summary>
    public sealed class NpcAnimatorDriver : MonoBehaviour
    {
        [Tooltip("Opcional: si lo dejas vacío, busca el Animator con Controller en los hijos.")]
        [SerializeField] private Animator animator;
        [Tooltip("Opcional: si lo dejas vacío, busca el NavMeshAgent en este objeto o en los padres.")]
        [SerializeField] private NavMeshAgent agent;

        [Tooltip("Suavizado del parámetro Speed (evita cambios bruscos).")]
        [SerializeField] private float speedDamp = 0.12f;

        private int _idSpeed, _idGrounded, _idMotionSpeed;

        private void Reset() => ResolveRefs();

        private void Awake()
        {
            ResolveRefs();
            _idSpeed = Animator.StringToHash("Speed");
            _idGrounded = Animator.StringToHash("Grounded");
            _idMotionSpeed = Animator.StringToHash("MotionSpeed");

            if (animator == null)
                Debug.LogWarning($"[NpcAnimatorDriver] '{name}': no encuentro ningún Animator con Controller. " +
                                 "Asigna el Player_Anim.controller al Animator del NPC.", this);
        }

        /// <summary>Busca el Animator que SÍ tiene Controller (no uno vacío) y el NavMeshAgent.</summary>
        private void ResolveRefs()
        {
            if (agent == null) agent = GetComponentInParent<NavMeshAgent>();

            // Si el Animator asignado no tiene controller (o no hay), busca uno que sí lo tenga.
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                foreach (var a in GetComponentsInChildren<Animator>(true))
                    if (a.runtimeAnimatorController != null) { animator = a; break; }
            }
        }

        private void Update()
        {
            if (animator == null || agent == null) return;

            float speed = agent.velocity.magnitude;                 // lo rápido que se mueve de verdad
            animator.SetFloat(_idSpeed, speed, speedDamp, Time.deltaTime);
            animator.SetBool(_idGrounded, true);                    // los NPC no saltan ni caen
            animator.SetFloat(_idMotionSpeed, 1f);
        }
    }
}
