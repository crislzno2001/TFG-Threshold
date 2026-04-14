using UnityEngine;
using System.Collections.Generic;

namespace OpenAI.Dialogue
{
    [System.Serializable]
    public class DialogueFlagRequirement
    {
        public string flag;
        public bool expectedValue = true;
    }

    [System.Serializable]
    public class DialogueFlagChange
    {
        public string flag;
        public bool value = true;
    }

    public abstract class DialogueNodeSO : ScriptableObject
    {
        [HideInInspector]
        public string nodeGuid;

        [TextArea(2, 5)]
        [Tooltip("Contexto que la IA recibe sobre este momento de la historia")]
        public string contextForAI;

        [Header("Prerequisite Gates")]
        [Tooltip("Flags que deben cumplirse para poder entrar en este nodo")]
        public List<DialogueFlagRequirement> prerequisiteFlags = new();

        [TextArea(1, 3)]
        [Tooltip("Respuesta opcional si el jugador intenta entrar aquí sin cumplir los requisitos")]
        public string lockedReply = "Aún no puedes avanzar por aquí.";

        [Header("Flags al entrar")]
        [Tooltip("Flags que se activan automáticamente al entrar en este nodo")]
        public List<DialogueFlagChange> flagsOnEnter = new();

        public List<DialogueNodeSO> nextNodes = new();
    }
}