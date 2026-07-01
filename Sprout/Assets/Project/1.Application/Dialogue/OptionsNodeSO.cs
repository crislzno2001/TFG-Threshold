using UnityEngine;
using System.Collections.Generic;

namespace OpenAI.Dialogue
{
    [System.Serializable]
    public class OptionData
    {
        [TextArea(1, 2)]
        [Tooltip("Texto del BOTÓN: lo que dice la florista al elegir esta opción.")]
        public string text;

        [Tooltip("A qué nodo lleva esta opción.")]
        public DialogueNodeSO nextNode;
    }

    /// <summary>
    /// Nodo de OPCIONES DEFINIDAS, sin IA: el NPC dice 'openingLine' y la jugadora elige entre botones que
    /// escribes TÚ a mano. Cada opción lleva a otro nodo. (Es distinto del ChoiceNode, que usa texto libre + IA.)
    /// Úsalo cuando quieras controlar exactamente qué puede responder la florista.
    /// </summary>
    [CreateAssetMenu(menuName = "Dialogue/Options Node (defined)")]
    public class OptionsNodeSO : DialogueNodeSO
    {
        [TextArea(1, 3)]
        [Tooltip("Lo que dice el NPC antes de que la jugadora elija.")]
        public string openingLine;

        public List<OptionData> options = new();
    }
}
