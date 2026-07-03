using System;
using System.Collections.Generic;
using UnityEngine;
using Sprout.Application;
using Sprout.Domain.DayCycle;

namespace OpenAI.Dialogue
{
    /// <summary>
    /// Enrutador de ENTRADA del diálogo. Va en el mismo GameObject que el NPCBrain/DialogueRunner.
    /// Al hablar con el NPC, en vez de arrancar siempre por el mismo nodo, arranca por el nodo que
    /// corresponde al DÍA y la FASE actuales (mañana / mediodía / tarde / noche).
    ///
    /// Prioridad al resolver:
    ///   1. Entrada que coincide con DÍA + FASE exactos (entriesByPhase).
    ///   2. Entrada de FASE del mismo día "a esta hora o antes" (la más avanzada que ya toca).
    ///   3. Entrada por DÍA (entriesByDay), el del día exacto o el más cercano por debajo.
    ///   4. entryNode normal del grafo (fallback).
    /// </summary>
    public sealed class DialogueEntryRouter : MonoBehaviour
    {
        [Serializable]
        public class DayEntry
        {
            [Min(1)] public int day = 1;
            public DialogueNodeSO node;
        }

        [Serializable]
        public class PhaseEntry
        {
            [Min(1)] public int day = 1;
            public DayPhase phase = DayPhase.Morning;
            public DialogueNodeSO node;
        }

        [Serializable]
        public class FlagEntry
        {
            public string flag;
            public bool expectedValue = true;
            public DialogueNodeSO node;
        }

        [Tooltip("Entrada por FLAG (MÁXIMA prioridad): si el flag coincide, empieza por este nodo. " +
                 "Ej: aster_angry → nodo de confrontación de cotilleo.")]
        public List<FlagEntry> entriesByFlag = new();

        [Tooltip("Nodo de entrada por DÍA (sin distinguir fase). Se usa si no hay match por fase.")]
        public List<DayEntry> entriesByDay = new();

        [Tooltip("Nodo de entrada por DÍA + FASE (mañana/mediodía/tarde/noche). Tiene prioridad.")]
        public List<PhaseEntry> entriesByPhase = new();

        /// <summary>Devuelve el nodo de entrada para el día y la fase actuales (o el fallback del grafo).</summary>
        public DialogueNodeSO ResolveEntry(DialogueNodeSO fallback)
        {
            int day = 1;
            DayPhase phase = DayPhase.Morning;
            var dir = SproutGameDirector.Instance;
            if (dir != null && dir.Day != null)
            {
                day = dir.Day.Day;
                phase = dir.Day.Phase;
            }

            // 0: por FLAG (máxima prioridad). Ej: si aster_angry, entra por la confrontación.
            if (dir != null && dir.Flags != null)
                foreach (var e in entriesByFlag)
                {
                    if (e == null || e.node == null || string.IsNullOrEmpty(e.flag)) continue;
                    if (dir.Flags.GetFlag(e.flag) == e.expectedValue) return e.node;
                }

            // 1 y 2: por fase.
            DialogueNodeSO exactPhase = null, bestPhase = null;
            int bestPhaseValue = int.MinValue;
            foreach (var e in entriesByPhase)
            {
                if (e == null || e.node == null || e.day != day) continue;
                if (e.phase == phase) exactPhase = e.node;
                // la fase "más avanzada que ya toca" (a esta hora o antes)
                if ((int)e.phase <= (int)phase && (int)e.phase > bestPhaseValue)
                {
                    bestPhaseValue = (int)e.phase;
                    bestPhase = e.node;
                }
            }
            if (exactPhase != null) return exactPhase;
            if (bestPhase != null) return bestPhase;

            // 3: por día (día exacto o el más cercano por debajo).
            DialogueNodeSO exactDay = null, bestLower = null;
            int bestLowerDay = int.MinValue;
            foreach (var e in entriesByDay)
            {
                if (e == null || e.node == null) continue;
                if (e.day == day) exactDay = e.node;
                if (e.day <= day && e.day > bestLowerDay) { bestLowerDay = e.day; bestLower = e.node; }
            }

            return exactDay ?? bestLower ?? fallback;
        }
    }
}
