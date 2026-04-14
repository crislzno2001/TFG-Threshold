using UnityEngine;

namespace OpenAI.Dialogue
{
    public class DialogueRunner : MonoBehaviour
    {
        private NPCBrain _brain;
        private DialogueNodeSO _current;

        private void Awake()
        {
            _brain = GetComponent<NPCBrain>();
        }

        public void StartDialogue()
        {
            if (_brain == null || _brain.dialogueGraph == null || _brain.dialogueGraph.entryNode == null)
                return;

            _brain.ResetHistory();
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
            if (_brain == null || _current == null)
                return "...";

            DialogueStepResult result = await _brain.ProcessStep(userMessage, _current);

            if (result != null && result.NextNode != null && result.NextNode != _current)
                AdvanceTo(result.NextNode);

            if (result == null || string.IsNullOrWhiteSpace(result.Reply))
                return "...";

            return result.Reply;
        }

        public DialogueNodeSO Current => _current;
    }
}