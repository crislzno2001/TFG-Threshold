using System;
using System.Collections.Generic;
using UnityEngine;
using Sprout.Application;

namespace OpenAI.Dialogue
{
    /// <summary>
    /// Enrutador de ENTRADA del diálogo. Va en el mismo GameObject que el NPCBrain/DialogueRunner.
    /// Cuando el jugador habla con el NPC, en vez de arrancar siempre por el mismo nodo de entrada,
    /// arranca por el nodo correspondiente al DÍA actual (la "reacción" del día 2, el desenlace del 3…).
    ///
    /// Configúralo en el inspector: por cada día, el nodo por el que debe empezar la conversación.
    /// Si no hay nodo para el día exacto, usa el del día más cercano por debajo; si no hay ninguno,
    /// usa el entryNode normal del grafo.
    /// </summary>
    public sealed class DialogueEntryRouter : MonoBehaviour
    {
        [Serializable]
        public class DayEntry
        {
            [Min(1)] public int day = 1;
            public DialogueNodeSO node;
        }

        [Tooltip("Nodo de entrada por día. Al hablar, se empieza por el del día actual.")]
        public List<DayEntry> entriesByDay = new();

        /// <summary>Devuelve el nodo de entrada para el día actual (o el fallback del grafo).</summary>
        public DialogueNodeSO ResolveEntry(DialogueNodeSO fallback)
        {
            int day = (SproutGameDirector.Instance != null && SproutGameDirector.Instance.Day != null)
                ? SproutGameDirector.Instance.Day.Day
                : 1;

            DialogueNodeSO exact = null, bestLower = null;
            int bestLowerDay = int.MinValue;

            foreach (var e in entriesByDay)
            {
                if (e == null || e.node == null) continue;
                if (e.day == day) exact = e.node;
                if (e.day <= day && e.day > bestLowerDay) { bestLowerDay = e.day; bestLower = e.node; }
            }

            return exact ?? bestLower ?? fallback;
        }
    }
}
