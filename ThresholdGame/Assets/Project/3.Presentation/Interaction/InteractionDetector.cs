using System;
using UnityEngine;
using ThresholdGame.Core.Interaction;

namespace ThresholdGame.Presentation.Interaction
{
    public class InteractionDetector : MonoBehaviour
    {
        [SerializeField] private float detectionRadius = 2.5f;

        public IInteractable Current { get; private set; }

        public event Action<IInteractable> OnInteractableChanged;

        private void Start()
        {
            var col = GetComponent<SphereCollider>();

            if (col == null)
                col = gameObject.AddComponent<SphereCollider>();

            col.isTrigger = true;
            col.radius = detectionRadius;

            Debug.Log($"[InteractionDetector] Inicializado en {gameObject.name}. Radius = {detectionRadius}", this);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.transform.root == transform.root)
                return;
            Debug.Log($"[InteractionDetector] Ha entrado algo: {other.name}", other);

            if (Current != null)
            {
                Debug.Log($"[InteractionDetector] Ya hay un interactuable actual, ignoro {other.name}", other);
                return;
            }

            var interactable = other.GetComponentInParent<IInteractable>();

            if (interactable == null)
            {
                Debug.Log($"[InteractionDetector] {other.name} NO tiene IInteractable en él ni en sus padres.", other);
                return;
            }

            Current = interactable;
            OnInteractableChanged?.Invoke(Current);

            Debug.Log($"[InteractionDetector] Interactable detectado: {other.name} / Label: {Current.InteractionLabel}", other);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.transform.root == transform.root)
                return;

            var interactable = other.GetComponentInParent<IInteractable>();

            if (interactable == null || interactable != Current)
                return;

            Debug.Log($"[InteractionDetector] Sale del rango: {other.name}", other);

            Current.CancelInteraction();
            Current = null;
            OnInteractableChanged?.Invoke(null);
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