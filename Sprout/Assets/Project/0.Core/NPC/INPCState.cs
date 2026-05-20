using UnityEngine.AI;
using UnityEngine;

namespace ThresholdGame.Core.NPC
{
    /// <summary>
    /// Contrato que implementa cada estado de comportamiento de un NPC.
    /// Los estados son clases puras de C#, no MonoBehaviours.
    /// Reciben directamente el agente y el transform para mantenerlo simple.
    /// </summary>
    public interface INPCState
    {
        void Enter(NavMeshAgent agent, Transform npcTransform, Transform player);
        void Tick(NavMeshAgent agent, Transform npcTransform, Transform player);
        void Exit(NavMeshAgent agent);
    }
}