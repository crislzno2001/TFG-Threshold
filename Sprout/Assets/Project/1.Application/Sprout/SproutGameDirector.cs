using System.Collections.Generic;
using UnityEngine;
using Sprout.Domain.Narrative;
using Sprout.Domain.Flowers;
using Sprout.Domain.Creativity;
using Sprout.Domain.DayCycle;
using Sprout.Data;

namespace Sprout.Application
{
    /// <summary>
    /// Central owner of runtime game state. Pure domain objects live here so that
    /// every service / UI reads from one source of truth. Thin: it holds state and
    /// exposes it; the actual rules live in the Domain layer.
    /// </summary>
    public class SproutGameDirector : MonoBehaviour
    {
        public static SproutGameDirector Instance { get; private set; }

        [SerializeField] private DaySettingsSO daySettings;

        // ── Domain state ──────────────────────────────────────────────────────
        public NarrativeFlagStore Flags { get; private set; }
        public RelationshipState Relationships { get; private set; }
        public FlowerInventory Inventory { get; private set; }
        public DayCycleState Day { get; private set; }

        // One creativity profile per NPC (Torrance dimensions).
        private readonly Dictionary<NpcId, CreativityProfile> _creativity = new();

        public CreativityProfile CreativityFor(NpcId npc)
        {
            if (!_creativity.TryGetValue(npc, out var p))
            {
                p = new CreativityProfile();
                _creativity[npc] = p;
            }
            return p;
        }

        /// <summary>Aggregate creativity across all NPCs (used by the ending).</summary>
        public CreativityScores AggregateCreativity()
        {
            int fluency = 0, flexibility = 0;
            float orig = 0f, elab = 0f; int withIdeas = 0;
            foreach (var kv in _creativity)
            {
                var s = kv.Value.Snapshot();
                fluency += s.Fluency;
                flexibility += s.Flexibility;
                if (s.Fluency > 0) { orig += s.Originality; elab += s.Elaboration; withIdeas++; }
            }
            return new CreativityScores
            {
                Fluency = fluency,
                Flexibility = flexibility,
                Originality = withIdeas > 0 ? orig / withIdeas : 0f,
                Elaboration = withIdeas > 0 ? elab / withIdeas : 0f
            };
        }

        public DaySettingsSO DaySettings => daySettings;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            Flags = new NarrativeFlagStore();
            Relationships = new RelationshipState();
            Inventory = new FlowerInventory();
            int totalDays = daySettings != null ? daySettings.totalDays : 3;
            Day = new DayCycleState(totalDays);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
