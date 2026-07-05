using UnityEngine;
using ThresholdGame.Core.Interaction;

namespace Sprout.SceneFlow
{
    /// <summary>
    /// Portal/salida que usa el MISMO sistema de interacción que los NPCs (pulsar E). Ponlo en el objeto
    /// que tenga el collider-trigger de la salida (p. ej. 'ExitTrigger'). Cuando el jugador está dentro del
    /// trigger y pulsa E, carga la escena destino en el SpawnPoint indicado.
    ///
    /// A diferencia del DoorPortal, NO depende de distancias ni de la cámara: reutiliza el detector de
    /// interacción que ya funciona para hablar con los vecinos. El objeto necesita un Collider marcado
    /// como 'Is Trigger'.
    /// </summary>
    public sealed class PortalInteractable : MonoBehaviour, IInteractable
    {
        [Header("Destino")]
        [Tooltip("Nombre EXACTO de la escena (debe estar en Build Settings).")]
        [SerializeField] private string targetScene = "GameScene";
        [Tooltip("Id del SpawnPoint en la escena destino donde aparece el jugador.")]
        [SerializeField] private string targetSpawnId = "Entrada";
        [Tooltip("Texto del cartelito ('Salir', 'Entrar'…).")]
        [SerializeField] private string label = "Salir";

        // ── IInteractable ──────────────────────────────────────────────────────

        public string InteractionLabel => label;

        public void Interact(IPlayerController player)
        {
            SceneTransitionManager.GetOrCreate().Go(targetScene, targetSpawnId);
        }

        public void CancelInteraction() { }
    }
}
