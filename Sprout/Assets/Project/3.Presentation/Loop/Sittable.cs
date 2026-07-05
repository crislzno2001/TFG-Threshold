using UnityEngine;
using ThresholdGame.Core.Interaction;

namespace Sprout.Presentation
{
    /// <summary>
    /// Mueble en el que la florista se puede SENTAR (silla) o TUMBAR (cama). Usa el MISMO sistema de
    /// interacción que los NPCs (pulsar E): al pulsar E se sienta/tumba; al volver a pulsar E se levanta.
    ///
    /// Requiere un Collider marcado como 'Is Trigger' (para que el detector de interacción lo vea).
    /// Coloca un objeto vacío 'Anchor' en el sitio exacto del asiento (posición + rotación mirando bien)
    /// y asígnalo abajo; si lo dejas vacío, usa la posición de este objeto.
    /// </summary>
    public sealed class Sittable : MonoBehaviour, IInteractable
    {
        [Tooltip("false = sentarse (silla) · true = tumbarse (cama).")]
        [SerializeField] private bool lie = false;

        [Tooltip("Punto donde se coloca el personaje (posición + rotación). Vacío = este objeto.")]
        [SerializeField] private Transform anchor;

        [SerializeField] private string playerTag = "Player";

        private CharacterSitController _sitter;

        public string InteractionLabel =>
            (_sitter != null && _sitter.IsSeated) ? "Levantarse" : (lie ? "Tumbarse" : "Sentarse");

        public void Interact(IPlayerController player)
        {
            if (_sitter == null)
            {
                var p = GameObject.FindGameObjectWithTag(playerTag);
                if (p != null) _sitter = p.GetComponentInChildren<CharacterSitController>();
            }
            if (_sitter == null)
            {
                Debug.LogWarning("[Sittable] El player no tiene 'CharacterSitController'. Añádeselo a la raíz del player.", this);
                return;
            }
            _sitter.Toggle(anchor != null ? anchor : transform, lie);
        }

        public void CancelInteraction() { }
    }
}
