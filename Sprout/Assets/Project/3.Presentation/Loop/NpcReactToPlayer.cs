using UnityEngine;

namespace Sprout.Presentation
{
    /// <summary>
    /// Hace que el NPC "note" al jugador: cuando te acercas, se gira suavemente a mirarte (estilo Animal
    /// Crossing). Opcionalmente dispara un trigger del Animator (p. ej. "Wave"/"Notice"). Ponlo en cada NPC.
    /// </summary>
    public sealed class NpcReactToPlayer : MonoBehaviour
    {
        [SerializeField] private string playerTag = "Player";
        [Tooltip("Distancia a la que el NPC se gira a mirarte.")]
        [SerializeField] private float noticeRadius = 4f;
        [SerializeField] private float turnSpeed = 6f;

        [Header("Animación opcional al notar (deja vacío si no usas)")]
        [SerializeField] private Animator animator;
        [SerializeField] private string noticeTrigger = "";

        private Transform _player;
        private bool _wasNear;

        private void Update()
        {
            if (_player == null)
            {
                var p = GameObject.FindGameObjectWithTag(playerTag);
                if (p == null) return;
                _player = p.transform;
            }

            Vector3 to = _player.position - transform.position;
            to.y = 0f;
            bool near = to.sqrMagnitude <= noticeRadius * noticeRadius;

            if (near && to.sqrMagnitude > 0.04f)
            {
                Quaternion target = Quaternion.LookRotation(to);
                transform.rotation = Quaternion.Slerp(transform.rotation, target, Time.deltaTime * turnSpeed);
            }

            if (near && !_wasNear && animator != null && !string.IsNullOrEmpty(noticeTrigger))
                animator.SetTrigger(noticeTrigger);

            _wasNear = near;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, noticeRadius);
        }
    }
}
