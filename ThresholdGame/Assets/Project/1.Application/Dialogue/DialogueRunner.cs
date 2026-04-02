using UnityEngine;
using ThresholdGame.Application.Commands;

namespace OpenAI.Dialogue
{
    public class DialogueRunner : MonoBehaviour
    {
        private NPCBrain _brain;
        private DialogueNodeSO _current;

        // Historial de decisiones de esta conversación
        private readonly CommandInvoker _invoker = new CommandInvoker();

        void Awake() => _brain = GetComponent<NPCBrain>();

        public void StartDialogue()
        {
            if (_brain.dialogueGraph == null) return;
            _invoker.Clear();
            AdvanceTo(_brain.dialogueGraph.entryNode);
        }

        /// <summary>
        /// Avanza al nodo indicado registrando la transición.
        /// Permite deshacer con UndoLastDecision().
        /// </summary>
        public void AdvanceTo(DialogueNodeSO node)
        {
            if (node == null) return;
            var cmd = new DialogueCommand(this, node, _current);
            _invoker.Execute(cmd);
        }

        /// <summary>
        /// Llamado por DialogueCommand para aplicar el cambio de nodo
        /// sin registrar de nuevo en el historial.
        /// </summary>
        internal void ApplyNode(DialogueNodeSO node)
        {
            if (node == null) return;
            _current = node;
            _brain.SetNode(node);
        }

        /// <summary>Deshace la última decisión y vuelve al nodo anterior.</summary>
        public bool UndoLastDecision() => _invoker.TryUndo();

        public void AdvanceToNext()
        {
            if (_current?.nextNodes?.Count == 1)
                AdvanceTo(_current.nextNodes[0]);
        }

        public async System.Threading.Tasks.Task<string> ProcessMessage(string userMessage)
        {
            string reply = await _brain.NPCSendMessage(userMessage);
            return reply;
        }

        public DialogueNodeSO Current => _current;

        /// <summary>Cuántas decisiones se pueden deshacer en esta conversación.</summary>
        public int UndoableDecisions => _invoker.Count;
    }
}