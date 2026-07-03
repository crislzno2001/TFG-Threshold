using System;
using System.Collections.Generic;
using UnityEngine;
using OpenAI.Dialogue;
using Sprout.Domain.Narrative;

namespace Sprout.Presentation
{
    /// <summary>
    /// Traduce FLAGS que ponen los grafos (con flagsOnEnter) al NpcSpotlight. Convención:
    ///
    ///   recommend_&lt;npc&gt;      → "ve a hablar con &lt;npc&gt;" (glow referral).
    ///   glow_&lt;npc&gt;_strong    → brilla fuerte (tiene nodo principal ahora).
    ///   glow_&lt;npc&gt;_soft      → brillo suave (reacción corta opcional).
    ///   glow_&lt;npc&gt;_red       → confrontación / consecuencia incómoda.
    ///   glow_&lt;npc&gt;_none      → apaga su brillo.
    ///
    /// &lt;npc&gt; = mochi / aster / moth / rix. Ponlo en la escena junto al NpcSpotlight.
    /// </summary>
    public sealed class SpotlightFlagBridge : MonoBehaviour
    {
        private readonly List<NPCBrain> _brains = new();

        private void Start()
        {
            foreach (var brain in FindObjectsByType<NPCBrain>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                brain.OnFlagSet += OnFlagSet;
                _brains.Add(brain);
            }
        }

        private void OnDestroy()
        {
            foreach (var b in _brains)
                if (b != null) b.OnFlagSet -= OnFlagSet;
        }

        private void OnFlagSet(string flag, bool value)
        {
            if (!value || string.IsNullOrEmpty(flag) || NpcSpotlight.Instance == null) return;
            flag = flag.Trim().ToLowerInvariant();

            if (flag.StartsWith("recommend_"))
            {
                if (TryNpc(flag.Substring("recommend_".Length), out var npc))
                    NpcSpotlight.Instance.Recommend(npc);
                return;
            }

            if (flag.StartsWith("glow_"))
            {
                string rest = flag.Substring("glow_".Length);   // "<npc>_<estado>"
                int us = rest.LastIndexOf('_');
                if (us <= 0) return;

                if (!TryNpc(rest.Substring(0, us), out var npc)) return;
                switch (rest.Substring(us + 1))
                {
                    case "strong":   NpcSpotlight.Instance.SetGlow(npc, GlowState.Strong); break;
                    case "soft":     NpcSpotlight.Instance.SetGlow(npc, GlowState.Soft); break;
                    case "red":      NpcSpotlight.Instance.SetGlow(npc, GlowState.Red); break;
                    case "referral": NpcSpotlight.Instance.Recommend(npc); break;
                    case "none":     NpcSpotlight.Instance.SetGlow(npc, GlowState.None); break;
                }
            }
        }

        private static bool TryNpc(string name, out NpcId npc) => Enum.TryParse(name, true, out npc);
    }
}
