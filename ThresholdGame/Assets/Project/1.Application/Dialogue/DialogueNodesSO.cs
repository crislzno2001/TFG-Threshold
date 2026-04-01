using UnityEngine;
using System.Collections.Generic;

namespace OpenAI.Dialogue
{
    public abstract class DialogueNodeSO : ScriptableObject
    {
        [TextArea(2, 5)]
        [Tooltip("Contexto que la IA recibe sobre este momento de la historia")]
        public string contextForAI;

        public List<DialogueNodeSO> nextNodes = new();
    }
}