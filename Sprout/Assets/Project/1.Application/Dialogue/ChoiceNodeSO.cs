using UnityEngine;
using System.Collections.Generic;

namespace OpenAI.Dialogue
{
    [CreateAssetMenu(menuName = "Dialogue/Choice Node")]
    public class ChoiceNodeSO : DialogueNodeSO
    {
        public List<ChoiceData> choices = new();
    }
}