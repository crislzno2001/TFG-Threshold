using UnityEngine;
using ThresholdGame.Architecture.Events;
using ThresholdGame.Core.Interaction;

namespace ThresholdGame.Presentation.Interaction
{
    /// <summary>
    /// Implementación de IInteractable para objetos físicos del mundo.
    /// No conoce PlayerStateMachine — habla solo con IPlayerController.
    /// No necesita referencia directa a InspectUI: la busca en escena una sola vez
    /// y la cachea. Esto evita tener que arrastrarla en el Inspector de cada objeto.
    /// </summary>
    [RequireComponent(typeof(GameEventRaiser))]
    public class ObjectInteractionTrigger : MonoBehaviour, IInteractable
    {
        [SerializeField] private InspectableObjectSO objectData;
        [SerializeField] private bool onlyOnce = true;

        private GameEventRaiser _eventRaiser;
        private UI.InspectUI _inspectUI;
        private bool _used;

        // ── IInteractable ──────────────────────────────────────────────────────

        public string InteractionLabel => objectData != null
            ? objectData.interactionType switch
            {
                InteractionType.Inspect => "Inspeccionar",
                InteractionType.Pickup => "Recoger",
                InteractionType.Activate => "Activar",
                _ => "Interactuar"
            }
            : "Interactuar";

        public void Interact(IPlayerController player)
        {
            if (_used && onlyOnce) return;
            if (objectData == null)
            {
                Debug.LogWarning("[ObjectTrigger] Falta InspectableObjectSO.", this);
                return;
            }

            _eventRaiser.Raise();

            switch (objectData.interactionType)
            {
                case InteractionType.Inspect:
                    player.EnterInspect();
                    _inspectUI?.Open(objectData, player.EnterFreeRoam);
                    break;

                case InteractionType.Pickup:
                    Debug.Log($"[ObjectTrigger] Recogido: {objectData.title}", this);
                    gameObject.SetActive(false);
                    break;

                case InteractionType.Activate:
                    Debug.Log($"[ObjectTrigger] Activado: {objectData.title}", this);
                    break;
            }

            if (onlyOnce) _used = true;
        }

        public void CancelInteraction() { }

        // ── Unity ──────────────────────────────────────────────────────────────

        private void Awake()
        {
            _eventRaiser = GetComponent<GameEventRaiser>();

            // Buscar InspectUI una sola vez. Es un objeto global de escena.
            // Si tuviéramos un ServiceLocator o un SceneContext lo inyectaríamos,
            // pero FindAnyObjectByType en Awake es aceptable para un objeto singleton de UI.
            _inspectUI = FindAnyObjectByType<UI.InspectUI>();

            if (_inspectUI == null)
                Debug.LogWarning("[ObjectTrigger] No se encontró InspectUI en la escena.", this);
        }
    }
}