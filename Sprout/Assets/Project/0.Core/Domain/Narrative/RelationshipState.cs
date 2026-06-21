using System;
using System.Collections.Generic;

namespace Sprout.Domain.Narrative
{
    /// <summary>
    /// Per-NPC relationship values. Kept deliberately coarse and never shown to
    /// the player as raw numbers — the UI translates them into subtle moods.
    /// </summary>
    [Serializable]
    public class RelationshipState
    {
        // Range roughly [-5, +5]. Negative = cold/hostile, positive = warm.
        private readonly Dictionary<NpcId, int> _affinity = new();

        public int Get(NpcId npc) => _affinity.TryGetValue(npc, out int v) ? v : 0;

        public void Add(NpcId npc, int delta)
        {
            int next = Clamp(Get(npc) + delta);
            _affinity[npc] = next;
        }

        public void Set(NpcId npc, int value) => _affinity[npc] = Clamp(value);

        private static int Clamp(int v) => v < -5 ? -5 : (v > 5 ? 5 : v);

        /// <summary>Subtle, player-facing mood label (no numbers).</summary>
        public string MoodLabel(NpcId npc)
        {
            int v = Get(npc);
            if (v <= -4) return "te guarda rencor";
            if (v <= -2) return "está a la defensiva contigo";
            if (v < 1)  return "te trata con cortés distancia";
            if (v < 3)  return "disfruta de tu compañía";
            if (v < 5)  return "confía en ti";
            return "te siente cercano";
        }

        public IReadOnlyDictionary<NpcId, int> All => _affinity;

        public void LoadFrom(Dictionary<NpcId, int> values)
        {
            _affinity.Clear();
            if (values == null) return;
            foreach (var kv in values) _affinity[kv.Key] = Clamp(kv.Value);
        }
    }
}
