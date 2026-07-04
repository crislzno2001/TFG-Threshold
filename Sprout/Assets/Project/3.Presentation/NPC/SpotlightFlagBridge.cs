using System;
using UnityEngine;
using Sprout.Application;
using Sprout.Domain.Narrative;

namespace Sprout.Presentation
{
    /// <summary>
    /// Traduce flags del STORE central (glow_*, recommend_*) al NpcSpotlight. Se usa sobre todo para el
    /// glow que provoca el COTILLEO nocturno (glow_X_red), que se escribe directamente en el store.
    ///
    /// Escucha al STORE (persistente, siempre existe vía el director) — NO a los brains — así evita el
    /// problema de timing que tenía antes. El glow del DIÁLOGO lo gestiona NpcGlow sobre cada NPC.
    /// Ponlo junto al NpcSpotlight.
    /// </summary>
    public sealed class SpotlightFlagBridge : MonoBehaviour
    {
        private NarrativeFlagStore _flags;

        private void Update()
        {
            // Enganche perezoso: en cuanto el director exista, nos suscribimos una sola vez.
            if (_flags != null) return;
            var d = SproutGameDirector.Instance;
            if (d == null) return;
            _flags = d.Flags;
            _flags.OnChanged += OnChanged;
        }

        private void OnDestroy()
        {
            if (_flags != null) _flags.OnChanged -= OnChanged;
        }

        private void OnChanged(string key)
        {
            if (string.IsNullOrEmpty(key) || NpcSpotlight.Instance == null) return;
            if (!_flags.GetFlag(key)) return;   // solo cuando el flag se ENCIENDE
            string flag = key.Trim().ToLowerInvariant();

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
