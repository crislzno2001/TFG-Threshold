using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Sprout.Data;
using Sprout.Domain.Endings;

namespace Sprout.Application
{
    /// <summary>
    /// Resolves the playthrough's ending from current state and surfaces the
    /// matching EndingDefinitionSO to the ending screen.
    /// </summary>
    public class EndingService : MonoBehaviour
    {
        [SerializeField] private List<EndingDefinitionSO> endings = new();

        [Tooltip("Set true if the player completed an unusual bouquet chain " +
                 "(e.g. Confession→Promise to Rix). Enables the secret ending.")]
        [SerializeField] private bool unusualBouquetChain;

        [Header("Events")]
        public UnityEvent<EndingDefinitionSO> onEndingResolved;
        public UnityEvent<EndingKind> onEndingKind;
        [Tooltip("Frases poéticas del resumen final según cómo jugó la florista (creatividad/manipulación).")]
        public UnityEvent<List<string>> onEndingSummary;

        private SproutGameDirector D => SproutGameDirector.Instance;

        public void SetUnusualBouquetChain(bool value) => unusualBouquetChain = value;

        public EndingKind ResolveAndShow()
        {
            if (D == null) return EndingKind.PrettyButHollow;

            var creativity = D.AggregateCreativity();
            EndingKind kind = EndingResolver.Resolve(D.Flags, creativity, unusualBouquetChain);

            // Resumen poético según cómo jugó (dimensiones de creatividad + manipulación).
            var summary = CreativitySummary.Build(creativity, D.Flags);

            Debug.Log($"[EndingService] Ending resolved: {kind}\n{string.Join("\n", summary)}");

            var def = endings.Find(e => e != null && e.kind == kind);
            onEndingKind?.Invoke(kind);
            onEndingResolved?.Invoke(def);
            onEndingSummary?.Invoke(summary);
            return kind;
        }
    }
}
