using System;
using UnityEngine;
using Sprout.Application;
using Sprout.Domain.DayCycle;
using Sprout.Domain.Narrative;
using OpenAI.Dialogue;

namespace Sprout.Presentation
{
    /// <summary>
    /// Al EMPEZAR cada fase (mañana / mediodía / tarde / noche) enciende en verde a los NPCs, para guiar
    /// al jugador sobre con quién hablar esa fase. Cada NPC se apaga solo (su nodo de despedida de fase
    /// pone glow_&lt;npc&gt;_none) cuando terminas su conversación de esa fase.
    ///
    /// Se engancha al cambio de fase del director (DayCycleState.OnPhaseChanged). Enlace perezoso: en
    /// cuanto el director existe, se suscribe una vez. Ponlo en la escena (junto al NpcSpotlight).
    /// </summary>
    public sealed class PhaseGlowLighter : MonoBehaviour
    {
        private SproutGameDirector _director;
        private bool _bound;

        private void Update()
        {
            if (_bound) return;
            _director = SproutGameDirector.Instance;
            if (_director == null || _director.Day == null) return;
            _director.Day.OnPhaseChanged += OnPhaseChanged;
            _bound = true;
            OnPhaseChanged(_director.Day.Day, _director.Day.Phase);  // enciende ya la fase actual
        }

        private void OnDestroy()
        {
            if (_director != null && _director.Day != null)
                _director.Day.OnPhaseChanged -= OnPhaseChanged;
        }

        private void OnPhaseChanged(int day, DayPhase phase)
        {
            var spot = NpcSpotlight.Instance;
            foreach (var brain in FindObjectsByType<NPCBrain>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (brain == null) continue;
                if (Enum.TryParse<NpcId>(brain.npcName, true, out var id))
                    spot.SetGlow(id, GlowState.Strong);
            }
        }
    }
}
