using UnityEngine;
using System.Collections.Generic;

namespace OpenAI.Dialogue
{
    /// <summary>
    /// Nodo de conversación abierta.
    /// 
    /// A diferencia del ChoiceNode (donde cada opción es una transición a otro nodo),
    /// el ConversationNode permite que el jugador hable libremente sobre varios temas
    /// dentro del MISMO nodo. La IA usa los "topics" como guía conversacional.
    /// 
    /// El nodo solo avanza cuando la IA detecta que se cumple la "exitCondition".
    /// </summary>
    [CreateAssetMenu(menuName = "Dialogue/Conversation Node")]
    public class ConversationNodeSO : DialogueNodeSO
    {
        [TextArea(1, 3)]
        [Tooltip("Lo que dice el NPC al entrar al nodo")]
        public string openingLine;

        [Tooltip("Temas que el jugador puede tocar. La IA los usa como guía, NO son transiciones.")]
        public List<string> conversationTopics = new();

        [TextArea(2, 4)]
        [Tooltip("Condición en lenguaje natural. Cuando se cumple, el nodo avanza al siguiente.")]
        public string exitCondition;
    }
}