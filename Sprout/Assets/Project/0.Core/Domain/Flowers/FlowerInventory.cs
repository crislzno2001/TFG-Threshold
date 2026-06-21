using System;
using System.Collections.Generic;

namespace Sprout.Domain.Flowers
{
    /// <summary>Player's collected flowers and crafted bouquets. Pure C#.</summary>
    [Serializable]
    public class FlowerInventory
    {
        private readonly Dictionary<FlowerKind, int> _flowers = new();
        private readonly Dictionary<BouquetKind, int> _bouquets = new();

        public event Action OnChanged;

        // ── Flowers ───────────────────────────────────────────────────────────

        public int CountOf(FlowerKind kind) => _flowers.TryGetValue(kind, out int n) ? n : 0;

        public void AddFlower(FlowerKind kind, int amount = 1)
        {
            if (kind == FlowerKind.None || amount <= 0) return;
            _flowers[kind] = CountOf(kind) + amount;
            OnChanged?.Invoke();
        }

        public bool RemoveFlower(FlowerKind kind, int amount = 1)
        {
            if (CountOf(kind) < amount) return false;
            _flowers[kind] -= amount;
            if (_flowers[kind] <= 0) _flowers.Remove(kind);
            OnChanged?.Invoke();
            return true;
        }

        public IReadOnlyDictionary<FlowerKind, int> Flowers => _flowers;

        // ── Bouquets ──────────────────────────────────────────────────────────

        public int CountOf(BouquetKind kind) => _bouquets.TryGetValue(kind, out int n) ? n : 0;

        public IReadOnlyDictionary<BouquetKind, int> Bouquets => _bouquets;

        /// <summary>
        /// Crafts a bouquet from two flowers. Consumes one of each on success.
        /// Returns BouquetKind.None if the recipe is invalid or flowers missing.
        /// </summary>
        public BouquetKind Craft(FlowerKind a, FlowerKind b)
        {
            var result = BouquetResolver.Resolve(a, b);
            if (result == BouquetKind.None) return BouquetKind.None;

            // Need both flowers; handle the a==b case (needs 2 of the same).
            if (a == b)
            {
                if (CountOf(a) < 2) return BouquetKind.None;
            }
            else
            {
                if (CountOf(a) < 1 || CountOf(b) < 1) return BouquetKind.None;
            }

            RemoveFlower(a);
            RemoveFlower(b);
            _bouquets[result] = CountOf(result) + 1;
            OnChanged?.Invoke();
            return result;
        }

        public bool GiveBouquet(BouquetKind kind)
        {
            if (CountOf(kind) <= 0) return false;
            _bouquets[kind] -= 1;
            if (_bouquets[kind] <= 0) _bouquets.Remove(kind);
            OnChanged?.Invoke();
            return true;
        }

        public void Clear()
        {
            _flowers.Clear();
            _bouquets.Clear();
            OnChanged?.Invoke();
        }

        public void LoadFrom(Dictionary<FlowerKind, int> flowers, Dictionary<BouquetKind, int> bouquets)
        {
            _flowers.Clear();
            _bouquets.Clear();
            if (flowers != null) foreach (var kv in flowers) _flowers[kv.Key] = kv.Value;
            if (bouquets != null) foreach (var kv in bouquets) _bouquets[kv.Key] = kv.Value;
            OnChanged?.Invoke();
        }
    }
}
