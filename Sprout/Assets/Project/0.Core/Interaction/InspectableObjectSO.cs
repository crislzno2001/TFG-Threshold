using UnityEngine;

namespace ThresholdGame.Core.Interaction
{
    public enum InteractionType { Inspect, Pickup, Activate }

    /// <summary>
    /// Datos de un objeto interactuable del mundo.
    /// Crea instancias desde Assets → Create → Threshold → Interaction → Interactable Object.
    /// </summary>
    [CreateAssetMenu(
        fileName = "Obj_New",
        menuName = "Threshold/Interaction/Interactable Object")]
    public class InspectableObjectSO : ScriptableObject
    {
        [Header("Tipo de interacción")]
        public InteractionType interactionType = InteractionType.Inspect;

        [Header("Contenido (usado en Inspect)")]
        public string title = "Objeto desconocido";

        [TextArea(3, 8)]
        public string description = "Sin descripción.";

        public Sprite image;
    }
}