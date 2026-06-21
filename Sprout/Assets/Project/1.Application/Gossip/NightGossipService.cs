using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Sprout.Domain.Gossip;

namespace Sprout.Application
{
    /// <summary>
    /// Runs the gossip rules at night: applies flag + relationship changes and
    /// emits vague summary lines for the night-summary UI. Internal flags are not
    /// exposed to the player — only the summary text is.
    /// </summary>
    public class NightGossipService : MonoBehaviour
    {
        [Header("Events")]
        public UnityEvent<List<string>> onNightSummary; // player-facing lines

        private SproutGameDirector D => SproutGameDirector.Instance;

        public List<string> RunNight()
        {
            var lines = new List<string>();
            if (D == null) return lines;

            var results = GossipRuleEngine.RunNight(D.Flags);
            foreach (var r in results)
            {
                foreach (var (flag, value) in r.FlagChanges)
                    D.Flags.SetFlag(flag, value);
                foreach (var (npc, delta) in r.RelationshipChanges)
                    D.Relationships.Add(npc, delta);
                if (!string.IsNullOrWhiteSpace(r.SummaryText))
                    lines.Add(r.SummaryText);
            }

            if (lines.Count == 0)
                lines.Add("El pueblo duerme. Esta noche nada se mueve.");

            onNightSummary?.Invoke(lines);
            return lines;
        }
    }
}
