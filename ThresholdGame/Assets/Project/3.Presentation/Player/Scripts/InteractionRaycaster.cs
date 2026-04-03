using UnityEngine;
using UnityEngine.UIElements;
using ThresholdGame.Core.Interactions;

namespace ThresholdGame.Presentation.Player
{
    /// <summary>
    /// Lanza un raycast desde la cámara principal hacia adelante para detectar
    /// objetos que implementen IInteractable en la capa 'Interactable'.
    ///
    /// Cuando encuentra un target:
    ///   - Muestra un prompt en UI Toolkit (Label con nombre "interaction-prompt").
    ///   - Al presionar la tecla de interacción llama a IInteractable.Interact().
    ///
    /// Setup en escena:
    ///   1. Añadir este componente al GameObject del jugador (o a la cámara).
    ///   2. Asignar un UIDocument con un Label cuyo name sea "interaction-prompt".
    ///   3. Asignar la LayerMask "Interactable" en el Inspector.
    ///   4. Crear la capa "Interactable" en Project Settings → Tags &amp; Layers.
    ///   5. Los objetos interactuables deben estar en esa capa y tener un
    ///      MonoBehaviour que implemente IInteractable.
    /// </summary>
    public sealed class InteractionRaycaster : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Raycast")]
        [Tooltip("Distancia máxima de detección en metros")]
        [SerializeField] private float _interactionDistance = 3f;

        [Tooltip("Capa(s) en las que se buscan objetos interactuables")]
        [SerializeField] private LayerMask _interactableLayer;

        [Tooltip("Tecla de interacción (New Input System no requerido; se puede " +
                 "reemplazar por un InputActionReference si se usa Input System)")]
        [SerializeField] private KeyCode _interactKey = KeyCode.E;

        [Header("UI Toolkit")]
        [Tooltip("UIDocument que contiene el prompt de interacción")]
        [SerializeField] private UIDocument _uiDocument;

        [Tooltip("Nombre del elemento Label en el UXML (USS name selector)")]
        [SerializeField] private string _promptElementName = "interaction-prompt";

        // ── Runtime ───────────────────────────────────────────────────────────

        private Camera       _camera;
        private Label        _promptLabel;
        private IInteractable _currentTarget;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            _camera = Camera.main;

            if (_camera == null)
                Debug.LogError("[InteractionRaycaster] No se encontró la Main Camera. " +
                               "Asegúrate de que la cámara tenga el tag 'MainCamera'.");

            BindUIToolkit();
        }

        private void Update()
        {
            PerformRaycast();
            HandleInteractInput();
        }

        // ── UI Toolkit ────────────────────────────────────────────────────────

        private void BindUIToolkit()
        {
            if (_uiDocument == null)
            {
                Debug.LogWarning("[InteractionRaycaster] UIDocument no asignado. " +
                                 "El prompt de interacción no se mostrará.");
                return;
            }

            _promptLabel = _uiDocument.rootVisualElement.Q<Label>(_promptElementName);

            if (_promptLabel == null)
            {
                Debug.LogWarning($"[InteractionRaycaster] No se encontró un Label con " +
                                 $"name='{_promptElementName}' en el UIDocument.");
            }
            else
            {
                HidePrompt();   // oculto por defecto
            }
        }

        // ── Raycast ───────────────────────────────────────────────────────────

        private void PerformRaycast()
        {
            if (_camera == null) return;

            var ray = new Ray(_camera.transform.position, _camera.transform.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, _interactionDistance, _interactableLayer)
                && hit.collider.TryGetComponent<IInteractable>(out var interactable))
            {
                if (!ReferenceEquals(interactable, _currentTarget))
                {
                    _currentTarget = interactable;
                    ShowPrompt(interactable.InteractionPrompt);
                }
            }
            else
            {
                if (_currentTarget != null)
                {
                    _currentTarget = null;
                    HidePrompt();
                }
            }
        }

        // ── Input de interacción ──────────────────────────────────────────────

        private void HandleInteractInput()
        {
            if (_currentTarget == null) return;

            if (Input.GetKeyDown(_interactKey))
                _currentTarget.Interact(gameObject);
        }

        // ── Prompt helpers ────────────────────────────────────────────────────

        private void ShowPrompt(string text)
        {
            if (_promptLabel == null) return;

            _promptLabel.text    = text;
            _promptLabel.visible = true;
        }

        private void HidePrompt()
        {
            if (_promptLabel == null) return;

            _promptLabel.visible = false;
            _promptLabel.text    = string.Empty;
        }

        // ── Gizmos ────────────────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_camera == null) return;

            Gizmos.color = _currentTarget != null
                ? Color.green
                : new Color(1f, 0.5f, 0f);   // naranja si no hay target

            Gizmos.DrawRay(_camera.transform.position,
                           _camera.transform.forward * _interactionDistance);
        }
#endif
    }
}
