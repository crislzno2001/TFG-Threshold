using System;
using UnityEngine;
using ThresholdGame.Core.Interaction;

namespace ThresholdGame.Presentation.Interaction
{
    /// <summary>
    /// Detecta el interactuable más cercano en rango mediante un ESCANEO ACTIVO por física
    /// (OverlapSphere) cada FixedUpdate. Antes usaba OnTriggerEnter/Exit, pero eso fallaba
    /// cuando el jugador aparecía teletransportado junto a un objeto (portales, intro del coche):
    /// el "enter" no se dispara si ya estás encima. El escaneo activo es inmune a eso.
    /// </summary>
    public class InteractionDetector : MonoBehaviour
    {
        [SerializeField] private float detectionRadius = 2.5f;

        public IInteractable Current { get; private set; }
        public event Action<IInteractable> OnInteractableChanged;

        private void FixedUpdate()
        {
            IInteractable best = FindNearest();
            if (!ReferenceEquals(best, Current))
            {
                if (Current != null) Current.CancelInteraction();
                Current = best;
                OnInteractableChanged?.Invoke(Current);
            }
        }

        private IInteractable FindNearest()
        {
            // Sin límite de resultados: en escenas densas (floristería con muchas flores/props)
            // el portal podía quedar fuera de un buffer pequeño y no detectarse.
            var hits = Physics.OverlapSphere(transform.position, detectionRadius, ~0, QueryTriggerInteraction.Collide);

            IInteractable best = null;
            float bestSq = float.MaxValue;
            foreach (var c in hits)
            {
                if (c == null || c.transform.root == transform.root) continue; // no me detecto a mí misma
                var it = c.GetComponentInParent<IInteractable>();
                if (it == null) continue;
                float d = (c.transform.position - transform.position).sqrMagnitude;
                if (d < bestSq) { bestSq = d; best = it; }
            }
            return best;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.2f);
            Gizmos.DrawSphere(transform.position, detectionRadius);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }
    }
}
