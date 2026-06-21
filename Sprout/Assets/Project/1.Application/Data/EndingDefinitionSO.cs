using UnityEngine;
using Sprout.Domain.Endings;

namespace Sprout.Data
{
    [CreateAssetMenu(fileName = "Ending_", menuName = "Sprout/Ending Definition")]
    public class EndingDefinitionSO : ScriptableObject
    {
        public EndingKind kind = EndingKind.PrettyButHollow;
        public string title = "Pretty But Hollow";

        [TextArea(4, 12)]
        public string epilogueText =
            "The village stays as cute as the day you arrived. The flowers are " +
            "pretty. No one remembers anything in particular about you.";

        public Sprite backgroundImage;
        public Color tint = Color.white;
    }
}
