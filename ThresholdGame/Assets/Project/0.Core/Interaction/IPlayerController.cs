using UnityEngine;

namespace ThresholdGame.Core.Interaction
{
    /// <summary>
    /// Abstracción del jugador que los IInteractable pueden usar sin conocer
    /// la implementación concreta (PlayerStateMachine).
    /// Esto permite que 0.Core no dependa de 3.Presentation.
    /// </summary>
    public interface IPlayerController
    {
        void EnterFreeRoam();
        void EnterDialogue();
        void EnterInspect();
    }
}
