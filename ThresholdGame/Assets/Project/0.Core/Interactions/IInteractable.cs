using UnityEngine;

namespace ThresholdGame.Core.Interactions
{
    /// <summary>
    /// Contrato para cualquier objeto del mundo con el que el jugador puede interactuar.
    /// Implementar en NPCBrain, puertas, objetos recogibles, etc.
    /// El InteractionRaycaster detecta esta interfaz en la capa 'Interactable'.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Texto que se muestra en el prompt de UI cuando el jugador apunta a este objeto.</summary>
        string InteractionPrompt { get; }

        /// <summary>
        /// Se llama cuando el jugador presiona la tecla de interacción.
        /// </summary>
        /// <param name="interactor">El GameObject del jugador que inicia la interacción.</param>
        void Interact(GameObject interactor);
    }
}
