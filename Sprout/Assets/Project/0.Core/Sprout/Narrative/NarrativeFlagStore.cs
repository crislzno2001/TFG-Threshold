using System;
using System.Collections.Generic;

namespace Sprout.Domain.Narrative
{
    /// <summary>
    /// Authoritative, cross-NPC store of boolean flags and integer counters that
    /// drive the narrative. Pure C# — no UnityEngine dependency — so it is fully
    /// unit-testable. MonoBehaviours mirror these values into each NPCBrain.
    /// </summary>
    [Serializable]
    public class NarrativeFlagStore
    {
        private readonly Dictionary<string, bool> _flags = new();
        private readonly Dictionary<string, int> _counters = new();

        /// <summary>Raised whenever any flag or counter changes (key, isCounter).</summary>
        public event Action<string> OnChanged;

        // ── Boolean flags ─────────────────────────────────────────────────────

        public bool GetFlag(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return false;
            return _flags.TryGetValue(key.Trim(), out bool v) && v;
        }

        public void SetFlag(string key, bool value = true)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            key = key.Trim();
            bool had = _flags.TryGetValue(key, out bool old);
            if (had && old == value) return;
            _flags[key] = value;
            OnChanged?.Invoke(key);
        }

        // ── Integer counters ──────────────────────────────────────────────────

        public int GetCounter(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return 0;
            return _counters.TryGetValue(key.Trim(), out int v) ? v : 0;
        }

        public void SetCounter(string key, int value)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            key = key.Trim();
            if (_counters.TryGetValue(key, out int old) && old == value) return;
            _counters[key] = value;
            OnChanged?.Invoke(key);
        }

        public int IncrementCounter(string key, int by = 1)
        {
            int next = GetCounter(key) + by;
            SetCounter(key, next);
            return next;
        }

        // ── Bulk access (for save / debug / bridge) ───────────────────────────

        public IReadOnlyDictionary<string, bool> AllFlags => _flags;
        public IReadOnlyDictionary<string, int> AllCounters => _counters;

        public void Clear()
        {
            _flags.Clear();
            _counters.Clear();
            OnChanged?.Invoke(null);
        }

        public void LoadFrom(Dictionary<string, bool> flags, Dictionary<string, int> counters)
        {
            _flags.Clear();
            _counters.Clear();
            if (flags != null)
                foreach (var kv in flags) _flags[kv.Key] = kv.Value;
            if (counters != null)
                foreach (var kv in counters) _counters[kv.Key] = kv.Value;
            OnChanged?.Invoke(null);
        }
    }
}
