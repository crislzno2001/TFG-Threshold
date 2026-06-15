using System;
using System.Collections.Generic;
using Sprout.Domain.Narrative;

namespace Sprout.Domain.Gossip
{
    /// <summary>One thing that happened overnight, shown in the night summary.</summary>
    public struct GossipResult
    {
        public string SummaryText;       // player-facing, vague on purpose
        public List<(string flag, bool value)> FlagChanges;
        public List<(NpcId npc, int delta)> RelationshipChanges;

        public static GossipResult Empty => new GossipResult
        {
            SummaryText = null,
            FlagChanges = new List<(string, bool)>(),
            RelationshipChanges = new List<(NpcId, int)>()
        };
    }

    /// <summary>
    /// At night the NPCs gossip without the player. This engine reads the current
    /// flags and produces flag/relationship changes plus a vague summary line.
    /// Pure C#. Rules implement the spec exactly.
    /// </summary>
    public static class GossipRuleEngine
    {
        public static List<GossipResult> RunNight(NarrativeFlagStore flags)
        {
            var results = new List<GossipResult>();
            if (flags == null) return results;

            bool F(string k) => flags.GetFlag(k);

            // 1. gossip_to_mochi_about_aster → Aster learns & will confront next day.
            if (F(NarrativeFlagKeys.GossipToMochiAboutAster) &&
                !F(NarrativeFlagKeys.AsterAngry) && !F(NarrativeFlagKeys.AsterForgives))
            {
                results.Add(Make(
                    "El secreto de Aster ha llegado a oídos equivocados. Esta mañana Aster está tenso.",
                    flagChanges: new() { (NarrativeFlagKeys.AsterAngry, true), (NarrativeFlagKeys.UnresolvedArgument, true) },
                    rel: new() { (NpcId.Aster, -2) }));
            }

            // 2. helped_moth_lie → Mochi hears something weird.
            if (F(NarrativeFlagKeys.HelpedMothLie))
            {
                results.Add(Make(
                    "Un rumor extraño recorre la plaza: Mochi ha oído algo raro sobre un mensaje para Rix.",
                    rel: new() { (NpcId.Mochi, -1), (NpcId.Rix, -1) }));
            }

            // 3. rix_hates_player → Aster becomes colder.
            if (F(NarrativeFlagKeys.RixHatesPlayer))
            {
                results.Add(Make(
                    "El enfado de Rix se ha contagiado. Aster ahora mantiene las distancias.",
                    rel: new() { (NpcId.Aster, -1) }));
            }

            // 4. aster_secret_known AND low mochi trust → Mochi does NOT learn (silence).
            //    (No gossip about Aster spreads — explicitly a non-event we surface softly.)
            if (F(NarrativeFlagKeys.AsterSecretKnown) && !F(NarrativeFlagKeys.MochiTrust) &&
                !F(NarrativeFlagKeys.GossipToMochiAboutAster))
            {
                results.Add(Make(
                    "Lo que sabes de Aster se quedó contigo. La noche está tranquila."));
            }

            // 5. player helped honestly → positive gossip improves one relationship.
            if (F(NarrativeFlagKeys.PlayerWasHonest) && !F(NarrativeFlagKeys.UnresolvedArgument))
            {
                results.Add(Make(
                    "Hoy alguien habló bien de tu sinceridad. Eso corre rápido por el pueblo.",
                    rel: new() { (NpcId.Mochi, +1) }));
            }

            // 6. comforting bouquet given → positive gossip spreads (flag set by FlowerService).
            if (F("gave_comforting_bouquet"))
            {
                results.Add(Make(
                    "El pueblo notó una pequeña bondad floreciendo. Ablanda algunos corazones.",
                    rel: new() { (NpcId.Aster, +1), (NpcId.Moth, +1) }));
            }

            return results;
        }

        private static GossipResult Make(
            string summary,
            List<(string, bool)> flagChanges = null,
            List<(NpcId, int)> rel = null) => new GossipResult
        {
            SummaryText = summary,
            FlagChanges = flagChanges ?? new List<(string, bool)>(),
            RelationshipChanges = rel ?? new List<(NpcId, int)>()
        };
    }
}
