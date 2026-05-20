using UnityEngine;
using System.Collections.Generic;

namespace OpenAI.Dialogue
{
    [System.Serializable]
    public class NodePositionData
    {
        public string nodeId;
        public Vector2 position;
    }

    [CreateAssetMenu(menuName = "Dialogue/Graph")]
    public class DialogueGraphSO : ScriptableObject
    {
        public List<DialogueNodeSO> nodes = new();
        public DialogueNodeSO entryNode;
        public List<NodePositionData> nodePositions = new();
    }
}