using System.Collections.Generic;
using UnityEngine;

namespace OpenAI.Dialogue
{
    public class DialogueRunner : MonoBehaviour
    {
        private NPCBrain _brain;
        private DialogueNodeSO _current;

        void Awake() => _brain = GetComponent<NPCBrain>();

        public void StartDialogue()
        {
            if (_brain.dialogueGraph == null) return;
            AdvanceTo(_brain.dialogueGraph.entryNode);
        }

        public void AdvanceTo(DialogueNodeSO node)
        {
            if (node == null) return;
            _current = node;
            _brain.SetNode(node);
        }

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
    }
}