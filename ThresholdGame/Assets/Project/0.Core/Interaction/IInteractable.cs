using UnityEngine;

namespace ThresholdGame.Core.Interaction
{
    /// <summary>
    /// Contrato que implementa cualquier objeto interactuable del mundo.
    /// PlayerFreeRoamState solo conoce esta interfaz, nunca los tipos concretos.
    /// Vive en 0.Core para que no dependa de ninguna capa superior.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Texto del prompt flotante: "Inspeccionar", "Hablar", "Recoger"...</summary>
        string InteractionLabel { get; }

        /// <summary>
        /// El jugador ha pulsado E. El interactuable recibe IPlayerController
        /// para poder cambiar el estado del jugador sin acoplar a PlayerStateMachine.
        /// </summary>
        void Interact(IPlayerController player);

        /// <summary>El jugador salió del rango. Limpia lo que sea necesario.</summary>
        void CancelInteraction();
    }
}