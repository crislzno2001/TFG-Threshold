using UnityEngine;
using System.Collections.Generic;

namespace OpenAI.Dialogue
{
    [CreateAssetMenu(menuName = "Dialogue/Choice Node")]
    public class ChoiceNodeSO : DialogueNodeSO
    {
        [TextArea(1, 3)]
        [Tooltip("Lo que dice el NPC al llegar a este nodo, antes de que el jugador elija.")]
        public string openingLine;

        public List<ChoiceData> choices = new();
    }
}