using UnityEngine;
using Sprout.Domain.Flowers;

namespace Sprout.Data
{
    /// <summary>How one NPC reacts to receiving a bouquet.</summary>
    [System.Serializable]
    public class BouquetNpcReaction
    {
        public Sprout.Domain.Narrative.NpcId npc;

        [TextArea(2, 4)]
        [Tooltip("Context handed to the AI so the NPC reacts in-character to this gift.")]
        public string reactionContext;

        [Tooltip("Relationship delta applied when this NPC receives this bouquet.")]
        public int affinityDelta;

        [Tooltip("If true, only positive when that NPC already trusts the player.")]
        public bool requiresTrust;
    }

    [CreateAssetMenu(fileName = "Bouquet_", menuName = "Sprout/Bouquet Definition")]
    public class BouquetDefinitionSO : ScriptableObject
    {
        public BouquetKind kind = BouquetKind.Peace;
        public string displayName = "Bouquet of Peace";

        [TextArea(2, 4)]
        public string emotionalMeaning = "Calm joy. A wish for things to be simple again.";

        public FlowerKind ingredientA;
        public FlowerKind ingredientB;

        public Sprite icon;
        public GameObject model;

        [Tooltip("Per-NPC reactions. NPCs not listed use a neutral default.")]
        public BouquetNpcReaction[] reactions = new BouquetNpcReaction[0];
    }
}
