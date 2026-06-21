using System.Collections.Generic;
using UnityEngine;
using OpenAI.Dialogue;

namespace Sprout.Application
{
    /// <summary>
    /// Keeps each NPCBrain's local flags in sync with the authoritative
    /// NarrativeFlagStore, so the AI system prompt always reflects current state
    /// (including cross-NPC gossip outcomes). Drop this on the game director object
    /// and assign the four NPC brains.
    /// </summary>
    public class NPCBrainFlagBridge : MonoBehaviour
    {
        [SerializeField] private List<NPCBrain> npcBrains = new();

        private SproutGameDirector _director;

        private void Start()
        {
            _director = SproutGameDirector.Instance;
            if (_director == null)
            {
                Debug.LogError("[NPCBrainFlagBridge] No SproutGameDirector in scene.");
                enabled = false;
                return;
            }

            _director.Flags.OnChanged += OnFlagChanged;

            // Two-way: when a dialogue node sets a flag on an NPC, push it to the
            // central store so gossip / endings / flowers react to story beats.
            foreach (var brain in npcBrains)
                if (brain != null) brain.OnFlagSet += OnBrainFlag;

            PushAll();
        }

        private void OnDestroy()
        {
            if (_director != null)
                _director.Flags.OnChanged -= OnFlagChanged;
            foreach (var brain in npcBrains)
                if (brain != null) brain.OnFlagSet -= OnBrainFlag;
        }

        private void OnFlagChanged(string key) => PushAll();

        private void OnBrainFlag(string key, bool value) => _director?.Flags.SetFlag(key, value);

        /// <summary>Push every flag + counter into every brain.</summary>
        public void PushAll()
        {
            if (_director == null) return;
            foreach (var brain in npcBrains)
            {
                if (brain == null) continue;
                foreach (var kv in _director.Flags.AllFlags)
                    brain.SetFlag(kv.Key, kv.Value);
                // counters are surfaced to the AI as boolean "high" hints + raw value flag
                foreach (var kv in _director.Flags.AllCounters)
                    brain.SetFlag($"{kv.Key}_value_{kv.Value}", true);
            }
        }
    }
}
