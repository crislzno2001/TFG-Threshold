using UnityEngine;

namespace OpenAI.Dialogue
{
    [System.Serializable]
    public class ChoiceData
    {
        [TextArea(1, 2)]
        public string condition;
        public DialogueNodeSO nextNode;
    }
}