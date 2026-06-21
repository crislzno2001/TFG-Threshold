using System.Collections.Generic;
using UnityEngine;
using Sprout.Domain.Flowers;
using Sprout.Domain.Narrative;

namespace Sprout.Application
{
    /// <summary>
    /// Grows an emotional flower the first time a narrative flag flips true, per the
    /// design's flower rules. This is how your conversation choices (lies, honesty,
    /// gossip, helping Moth, Rix opening up, unresolved arguments, victories) turn
    /// into flowers you can later craft into bouquets.
    /// </summary>
    public class FlowerFlagListener : MonoBehaviour
    {
        [SerializeField] private FlowerService flowerService;

        private SproutGameDirector D => SproutGameDirector.Instance;
        private readonly HashSet<string> _done = new();

        private static readonly Dictionary<string, FlowerKind> Rules = new()
        {
            { NarrativeFlagKeys.PlayerLiedKindly,         FlowerKind.Crisalida }, // kind lie → secret
            { NarrativeFlagKeys.PlayerWasHonest,          FlowerKind.Anima },     // painful honesty → honesty
            { NarrativeFlagKeys.PlayerGossiped,           FlowerKind.Inquieta },  // gossip → unease
            { NarrativeFlagKeys.GossipToMochiAboutAster,  FlowerKind.Inquieta },
            { NarrativeFlagKeys.HelpedMothLie,            FlowerKind.Brasa },     // dark request → passion
            { NarrativeFlagKeys.RixTrustsPlayer,          FlowerKind.Acuariana }, // Rix opens up → calm
            { NarrativeFlagKeys.UnresolvedArgument,       FlowerKind.Velada },    // argument → sadness
            { NarrativeFlagKeys.MochiMasterpiece,         FlowerKind.Sol },       // triumph → joy
            { NarrativeFlagKeys.AsterVictory,             FlowerKind.Sol },
        };

        private void Start()
        {
            if (D != null) D.Flags.OnChanged += OnChanged;
        }

        private void OnDestroy()
        {
            if (D != null) D.Flags.OnChanged -= OnChanged;
        }

        private void OnChanged(string key)
        {
            if (string.IsNullOrEmpty(key) || flowerService == null || D == null) return;
            if (_done.Contains(key)) return;
            if (Rules.TryGetValue(key, out var kind) && D.Flags.GetFlag(key))
            {
                _done.Add(key);
                flowerService.Grant(kind);
                Debug.Log($"[FlowerFlagListener] '{key}' → grew a {kind} flower.");
            }
        }
    }
}
