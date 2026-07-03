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
        public List<(string counter, int delta)> CounterChanges;

        public static GossipResult Empty => new GossipResult
        {
            SummaryText = null,
            FlagChanges = new List<(string, bool)>(),
            RelationshipChanges = new List<(NpcId, int)>(),
            CounterChanges = new List<(string, int)>()
        };
    }

    /// <summary>
    /// De noche los NPCs cotillean sin la florista. Este motor lee los flags actuales y produce
    /// cambios de flag/relación/contador + una línea vaga de resumen. Además enciende los glow ROJOS
    /// del día siguiente (flags glow_X_red) para señalar confrontaciones. C# puro.
    /// </summary>
    public static class GossipRuleEngine
    {
        public static List<GossipResult> RunNight(NarrativeFlagStore flags)
        {
            var results = new List<GossipResult>();
            if (flags == null) return results;

            bool F(string k) => flags.GetFlag(k);
            int C(string k) => flags.GetCounter(k);

            // ── NEGATIVOS ──────────────────────────────────────────────────────

            // 1. Cotilleaste el secreto de Aster → se entera y confronta mañana (glow rojo).
            if (F(NarrativeFlagKeys.GossipToMochiAboutAster) &&
                !F(NarrativeFlagKeys.AsterAngry) && !F(NarrativeFlagKeys.AsterForgives))
            {
                results.Add(Make(
                    "El secreto de Aster ha llegado a oídos equivocados. Mañana estará tenso.",
                    flagChanges: new() {
                        (NarrativeFlagKeys.AsterAngry, true),
                        (NarrativeFlagKeys.UnresolvedArgument, true),
                        ("glow_aster_red", true)
                    },
                    rel: new() { (NpcId.Aster, -2) }));
            }

            // 2. Ayudaste a mentir a Moth → Rix detecta la incoherencia y confrontará (glow rojo).
            if (F(NarrativeFlagKeys.HelpedMothLie))
            {
                results.Add(Make(
                    "Un rumor con perfume de polilla recorre la plaza. Rix ha fruncido el ceño.",
                    flagChanges: new() {
                        (NarrativeFlagKeys.RixAlert, true),
                        ("glow_rix_red", true)
                    },
                    rel: new() { (NpcId.Mochi, -1), (NpcId.Rix, -1) }));
            }

            // 3. Rix te odia → habla mal de ti con Aster; Aster empieza incómodo.
            if (F(NarrativeFlagKeys.RixHatesPlayer))
            {
                results.Add(Make(
                    "El enfado de Rix se ha contagiado. Aster ahora mantiene las distancias.",
                    flagChanges: new() { ("aster_incomodo", true) },
                    rel: new() { (NpcId.Aster, -1) }));
            }

            // 4. Dependencia de Moth alta → te busca demasiado (glow rojo + escena de dependencia).
            if (C(NarrativeFlagKeys.MothDependency) >= 2)
            {
                results.Add(Make(
                    "Moth ha preguntado por ti tres veces esta noche. La luz se tuerce cuando te vas.",
                    flagChanges: new() {
                        ("moth_dependency_triggered", true),
                        ("glow_moth_red", true)
                    },
                    rel: new() { (NpcId.Moth, +1) }));
            }

            // 5. Moth sabe que hablaste con Rix + Rix en alerta → se siente expuesta.
            if (F(NarrativeFlagKeys.MothKnowsYouTalkedToRix) && F(NarrativeFlagKeys.RixAlert))
            {
                results.Add(Make(
                    "Moth ha atado cabos. Siente que alguien abrió su nombre por dentro.",
                    flagChanges: new() {
                        (NarrativeFlagKeys.MothFeelsExposed, true),
                        ("glow_moth_red", true)
                    },
                    rel: new() { (NpcId.Moth, -2) }));
            }

            // 6. aster_secret_known sin cotilleo ni confianza → silencio (no-evento suave).
            if (F(NarrativeFlagKeys.AsterSecretKnown) && !F(NarrativeFlagKeys.MochiTrust) &&
                !F(NarrativeFlagKeys.GossipToMochiAboutAster))
            {
                results.Add(Make("Lo que sabes de Aster se quedó contigo. La noche está tranquila."));
            }

            // ── POSITIVOS ──────────────────────────────────────────────────────

            // 7. Guardaste el secreto (discreción) → Aster se entera y te aprecia.
            if (F(NarrativeFlagKeys.MochiRespectsDiscretion))
            {
                results.Add(Make(
                    "\"La florista sabe guardar un secreto. Qué peligro tan elegante\", dijo Mochi.",
                    rel: new() { (NpcId.Aster, +1) }));
            }

            // 8. Ayudaste bien a Aster → habla bien de ti; Moth se interesa.
            if (F(NarrativeFlagKeys.PlayerHelpedAsterGood))
            {
                results.Add(Make(
                    "Aster te elogió sin darse cuenta. \"Quien ayuda a volar quizá entiende la luz\", pensó Moth.",
                    rel: new() { (NpcId.Moth, +1) }));
            }

            // 9. Rix confía → deja de corregir cuando hablan bien de ti → sube tu reputación sincera.
            if (F(NarrativeFlagKeys.RixTrustsPlayer))
            {
                results.Add(Make(
                    "Rix no cotilleó. Pero tampoco corrigió a quien habló bien de ti. Para Rix, eso es mucho.",
                    counters: new() { (NarrativeFlagKeys.FloristSincereRep, +1) }));
            }

            // 10. Rechazaste la mentira de Moth (con tacto) → Mochi respeta tu honestidad.
            if (F(NarrativeFlagKeys.RefusedMothLie) && !F(NarrativeFlagKeys.MochiOffended))
            {
                results.Add(Make(
                    "No moviste palabras de una boca a otra. Mochi lo notó, y eso, en un pueblo pequeño, pesa.",
                    counters: new() { (NarrativeFlagKeys.FloristSincereRep, +1) },
                    rel: new() { (NpcId.Mochi, +1) }));
            }

            // 11. Ramo consolador → bondad que ablanda corazones.
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
            List<(NpcId, int)> rel = null,
            List<(string, int)> counters = null) => new GossipResult
        {
            SummaryText = summary,
            FlagChanges = flagChanges ?? new List<(string, bool)>(),
            RelationshipChanges = rel ?? new List<(NpcId, int)>(),
            CounterChanges = counters ?? new List<(string, int)>()
        };
    }
}
