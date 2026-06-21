using UnityEngine;
using Sprout.Domain.Flowers;

namespace Sprout.Data
{
    /// <summary>Designer-facing data for one emotional flower.</summary>
    [CreateAssetMenu(fileName = "Flower_", menuName = "Sprout/Flower Definition")]
    public class FlowerDefinitionSO : ScriptableObject
    {
        public FlowerKind kind = FlowerKind.Sol;

        [Tooltip("In-world display name (Spanish).")]
        public string displayName = "Sol";

        [Tooltip("One-word emotion this flower embodies.")]
        public string emotion = "joy";

        [TextArea(2, 5)]
        public string description = "A warm, sun-yellow bloom that grows from moments of joy.";

        [Tooltip("UI icon (generated placeholder by default).")]
        public Sprite icon;

        [Tooltip("Optional 3D model for the growing area.")]
        public GameObject model;

        public Color color = new Color(1f, 0.85f, 0.2f);

        [TextArea(2, 4)]
        [Tooltip("How this flower matters in gameplay / what it signals about the player.")]
        public string gameplayMeaning =
            "Generated when the player shows high fluency (many ideas).";
    }
}
