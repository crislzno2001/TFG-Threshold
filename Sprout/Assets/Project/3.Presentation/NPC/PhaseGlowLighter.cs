using System;
using UnityEngine;
using Sprout.Application;
using Sprout.Domain.Narrative;
using OpenAI.Dialogue;

namespace Sprout.Presentation
{
    /// <summary>
    /// Al EMPEZAR cada fase (mañana / mediodía / tarde / noche) enciende en verde a los NPCs, para guiar
    /// al jugador sobre con quién hablar esa fase. Cada NPC se apaga solo (su nodo de despedida de fase
    /// pone glow_&lt;npc&gt;_none) cuando terminas su conversación de esa fase.
    ///
    /// Robusto ante el arranque por escenas (bootstrap → menú → pueblo): el encendido INICIAL solo
    /// necesita que existan los NPCs (no espera al director). Para los cambios de fase se engancha al
    /// DayCycleService en cuanto aparece. Ponlo en la escena (se auto-instala junto al NpcSpotlight).
    /// </summary>
    public sealed class PhaseGlowLighter : MonoBehaviour
    {
        private bool _litInitial;
        private bool _bound;

        private void Update()
        {
            // 1) Encendido inicial (mañana): en cuanto haya NPCs en la escena, enciéndelos.
            if (!_litInitial && LightAllNpcs("inicio", 1) > 0)
                _litInitial = true;

            // 2) Engánchate al ciclo de día para los cambios de fase (mediodía/tarde/noche).
            if (!_bound)
            {
                var svc = FindFirstObjectByType<DayCycleService>();
                if (svc != null)
                {
                    svc.onPhaseChanged.AddListener(OnPhaseChanged);
                    _bound = true;
                }
            }
        }

        private void OnDestroy()
        {
            var svc = FindFirstObjectByType<DayCycleService>();
            if (svc != null) svc.onPhaseChanged.RemoveListener(OnPhaseChanged);
        }

        private void OnPhaseChanged(int day, string phase) => LightAllNpcs(phase, day);

        private int LightAllNpcs(string phase, int day)
        {
            var spot = NpcSpotlight.Instance;
            int lit = 0;
            foreach (var brain in FindObjectsByType<NPCBrain>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (brain == null) continue;
                if (Enum.TryParse<NpcId>(brain.npcName, true, out var id))
                {
                    spot.SetGlow(id, GlowState.Strong);
                    lit++;
                }
            }
            if (lit > 0) Debug.Log($"[PhaseGlowLighter] Fase {phase} (día {day}) → encendidos {lit} NPCs");
            return lit;
        }
    }
}
