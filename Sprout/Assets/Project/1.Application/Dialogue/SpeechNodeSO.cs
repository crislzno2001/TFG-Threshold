using UnityEngine;
using System.Collections.Generic;

namespace OpenAI.Dialogue
{
    [CreateAssetMenu(menuName = "Dialogue/Speech Node")]
    public class SpeechNodeSO : DialogueNodeSO
    {
        [TextArea(1, 3)]
        public string openingLine;
        public List<NodeTransition> transitions = new();
    }

    [System.Serializable]
    public class NodeTransition
    {
        [TextArea(1, 2)]
        public string condition;
        public DialogueNodeSO targetNode;
    }
}