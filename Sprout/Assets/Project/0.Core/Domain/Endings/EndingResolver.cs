using Sprout.Domain.Creativity;
using Sprout.Domain.Narrative;

namespace Sprout.Domain.Endings
{
    public enum EndingKind
    {
        BloomingVillage,   // high creativity, low harm, honest/helpful
        PrettyButHollow,   // low creativity, evasive
        TangledRoots,      // gossip, lies, obsession, damage
        QuietAcceptance,   // honest but imperfect, accepted consequences
        SecretEnding       // strong Rix trust / unusual bouquet chain
    }

    /// <summary>
    /// Decides which ending the playthrough reaches. Pure C#. Evaluated in
    /// priority order: Secret first (rarest), then harm-driven, then the rest.
    /// </summary>
    public static class EndingResolver
    {
        public static EndingKind Resolve(
            NarrativeFlagStore flags,
            CreativityScores creativity,
            bool unusualBouquetChain = false)
        {
            bool F(string k) => flags.GetFlag(k);

            int harm = 0;
            if (F(NarrativeFlagKeys.HelpedMothLie)) harm++;
            if (F(NarrativeFlagKeys.RixHatesPlayer)) harm++;
            if (F(NarrativeFlagKeys.AsterAngry)) harm++;
            if (F(NarrativeFlagKeys.RixLeftTown)) harm += 2;
            if (F(NarrativeFlagKeys.UnresolvedArgument)) harm++;
            if (F(NarrativeFlagKeys.MochiOffended)) harm++;

            bool highCreativity =
                creativity.Fluency >= 5 ||
                creativity.Originality >= 0.6f ||
                creativity.Elaboration >= 0.6f;

            // ── 1. Secret ending — strong Rix trust or an unusual bouquet chain. ─
            if ((F(NarrativeFlagKeys.RixTrustsPlayer) && F(NarrativeFlagKeys.RixCuriosity))
                || unusualBouquetChain)
            {
                return EndingKind.SecretEnding;
            }

            // ── 2. Tangled Roots — significant harm / obsession / lies. ──────────
            if (harm >= 2 || F(NarrativeFlagKeys.RixLeftTown))
            {
                return EndingKind.TangledRoots;
            }

            // ── 3. Blooming Village — high creativity, low harm, honesty. ────────
            if (highCreativity && harm == 0 && F(NarrativeFlagKeys.PlayerWasHonest))
            {
                return EndingKind.BloomingVillage;
            }

            // ── 4. Quiet Acceptance — honest but imperfect (some hurt remains). ──
            if (F(NarrativeFlagKeys.PlayerWasHonest) && harm >= 1)
            {
                return EndingKind.QuietAcceptance;
            }

            // ── 5. Pretty But Hollow — low engagement / evasive default. ─────────
            return EndingKind.PrettyButHollow;
        }
    }
}
