namespace Sprout.Domain.Narrative
{
    /// <summary>
    /// Canonical narrative flag / counter names.
    /// These strings MUST match the flag strings used in the dialogue-graph
    /// ScriptableObjects (prerequisiteFlags / flagsOnEnter) so designer data and
    /// code agree. Boolean flags are stored in NarrativeFlagStore.SetFlag;
    /// counters in NarrativeFlagStore.SetCounter.
    /// </summary>
    public static class NarrativeFlagKeys
    {
        // ── Aster ────────────────────────────────────────────────────────────
        public const string AsterMet = "aster_met";
        public const string AsterIdeasCount = "aster_ideas_count";       // counter
        public const string AsterIdeaStrange = "aster_idea_strange";
        public const string AsterSecretKnown = "aster_secret_known";
        public const string AsterAngry = "aster_angry";
        public const string AsterForgives = "aster_forgives";
        public const string AsterVictory = "aster_victory";

        // ── Mochi ────────────────────────────────────────────────────────────
        public const string MochiMet = "mochi_met";
        public const string MochiIdeasCount = "mochi_ideas_count";       // counter
        public const string MochiTrust = "mochi_trust";
        public const string MochiOffended = "mochi_offended";
        public const string MochiMasterpiece = "mochi_masterpiece";
        public const string GossipToMochiAboutAster = "gossip_to_mochi_about_aster";

        // ── Moth ─────────────────────────────────────────────────────────────
        public const string MothKnown = "moth_known";
        public const string MothFriendship = "moth_friendship";          // counter
        public const string MothAskedForHelp = "moth_asked_for_help";
        public const string HelpedMothLie = "helped_moth_lie";

        // ── Rix ──────────────────────────────────────────────────────────────
        public const string RixKnown = "rix_known";
        public const string RixFriendship = "rix_friendship";            // counter
        public const string RixCuriosity = "rix_curiosity";
        public const string RixAlert = "rix_alert";
        public const string RixNeutral = "rix_neutral";
        public const string RixHatesPlayer = "rix_hates_player";
        public const string RixTrustsPlayer = "rix_trusts_player";
        public const string RixLeftTown = "rix_left_town";

        // ── Player-level ──────────────────────────────────────────────────────
        public const string PlayerGossiped = "player_gossiped";
        public const string PlayerWasHonest = "player_was_honest";
        public const string PlayerLiedKindly = "player_lied_kindly";
        public const string UnresolvedArgument = "unresolved_argument";

        // ── Cotilleo cruzado (nuevos) ─────────────────────────────────────────
        public const string PlayerHelpedAsterGood = "player_helped_aster_good";
        public const string MochiRespectsDiscretion = "mochi_respects_discretion";
        public const string MothDependency = "moth_dependency";            // counter
        public const string MothKnowsYouTalkedToRix = "moth_knows_you_talked_rix";
        public const string MothFeelsExposed = "moth_feels_exposed";
        public const string RefusedMothLie = "refused_moth_lie";
        public const string FloristSincereRep = "florist_sincere_rep";     // counter
    }

    /// <summary>The four neighbours, used as keys across systems.</summary>
    public enum NpcId
    {
        Mochi,
        Aster,
        Moth,
        Rix
    }
}
