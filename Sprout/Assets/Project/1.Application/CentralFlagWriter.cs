using UnityEngine;
using Sprout.Domain.Narrative;

namespace Sprout.Application
{
    /// <summary>
    /// Bridge so dialogue/UI/GameEvent UnityEvents can write into the authoritative
    /// NarrativeFlagStore (and relationships). Hook these methods from dialogue
    /// consequences, choice buttons, or GameEventListener responses.
    /// </summary>
    public class CentralFlagWriter : MonoBehaviour
    {
        [SerializeField] private FlowerService flowerService;

        private SproutGameDirector D => SproutGameDirector.Instance;

        public void SetFlag(string key) { D?.Flags.SetFlag(key, true); }
        public void ClearFlag(string key) { D?.Flags.SetFlag(key, false); }
        public void IncrementCounter(string key) { D?.Flags.IncrementCounter(key); }

        // "Mochi:+1", "Rix:-2"
        public void AddRelationship(string npcAndDelta)
        {
            if (D == null || string.IsNullOrWhiteSpace(npcAndDelta)) return;
            var parts = npcAndDelta.Split(':');
            if (parts.Length != 2) return;
            if (!System.Enum.TryParse(parts[0].Trim(), true, out NpcId npc)) return;
            if (!int.TryParse(parts[1].Trim(), out int delta)) return;
            D.Relationships.Add(npc, delta);
        }

        public void GrantFlower(string flowerName) => flowerService?.GrantByName(flowerName);

        // Convenience consequences matching the flower spec.
        public void LiedKindlyToMochi()
        {
            D?.Flags.SetFlag(NarrativeFlagKeys.PlayerLiedKindly, true);
            D?.Flags.SetFlag(NarrativeFlagKeys.MochiTrust, true);
            flowerService?.OnLiedKindlyToMochi();
        }

        public void PainfulHonesty()
        {
            D?.Flags.SetFlag(NarrativeFlagKeys.PlayerWasHonest, true);
            D?.Flags.SetFlag(NarrativeFlagKeys.MochiOffended, true);
            flowerService?.OnPainfulHonesty();
        }

        public void Gossiped()
        {
            D?.Flags.SetFlag(NarrativeFlagKeys.PlayerGossiped, true);
            D?.Flags.SetFlag(NarrativeFlagKeys.GossipToMochiAboutAster, true);
            flowerService?.OnGossiped();
        }

        public void HelpedMothLie()
        {
            D?.Flags.SetFlag(NarrativeFlagKeys.HelpedMothLie, true);
            flowerService?.OnHelpedMothLie();
        }

        public void RefusedMothLie()
        {
            D?.Flags.SetFlag(NarrativeFlagKeys.PlayerWasHonest, true);
            flowerService?.OnRefusedMothLie();
        }

        public void RixOpenedUp()
        {
            D?.Flags.SetFlag(NarrativeFlagKeys.RixTrustsPlayer, true);
            flowerService?.OnRixOpenedUp();
        }
    }
}
