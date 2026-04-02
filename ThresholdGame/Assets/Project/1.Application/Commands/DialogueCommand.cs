using UnityEngine;
using OpenAI.Dialogue;
using ThresholdGame.Core.Commands;

namespace ThresholdGame.Application.Commands
{
    /// <summary>
    /// Registra una transición de nodo en el grafo de diálogo.
    /// Execute: avanza al nodo destino.
    /// Undo:    vuelve al nodo anterior (permite al jugador retroceder en una conversación).
    /// </summary>
    public sealed class DialogueCommand : ICommand
    {
        private readonly DialogueRunner _runner;
        private readonly DialogueNodeSO _targetNode;
        private readonly DialogueNodeSO _previousNode;

        /// <param name="runner">El DialogueRunner del NPC.</param>
        /// <param name="targetNode">Nodo al que se quiere avanzar.</param>
        /// <param name="previousNode">Nodo actual antes de la transición (para Undo).</param>
        public DialogueCommand(DialogueRunner runner, DialogueNodeSO targetNode, DialogueNodeSO previousNode)
        {
            _runner       = runner;
            _targetNode   = targetNode;
            _previousNode = previousNode;
        }

        public void Execute() => _runner.ApplyNode(_targetNode);

        public void Undo()
        {
            if (_previousNode == null)
            {
                Debug.LogWarning("[DialogueCommand] No hay nodo anterior al que volver.");
                return;
            }
            _runner.ApplyNode(_previousNode);
        }

        public override string ToString() =>
            $"DialogueCommand [{_previousNode?.name ?? "START"} → {_targetNode?.name ?? "NULL"}]";
    }
}
