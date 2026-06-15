using System.Collections.Generic;

namespace Sprout.Domain.Flowers
{
    /// <summary>
    /// Resolves which bouquet two flowers combine into. Order-independent.
    /// Pure C#, fully unit-testable.
    /// </summary>
    public static class BouquetResolver
    {
        // Unordered pair key so (a,b) == (b,a).
        private static long Key(FlowerKind a, FlowerKind b)
        {
            int lo = (int)a, hi = (int)b;
            if (lo > hi) (lo, hi) = (hi, lo);
            return ((long)lo << 32) | (uint)hi;
        }

        private static readonly Dictionary<long, BouquetKind> Recipes = new()
        {
            { Key(FlowerKind.Sol,       FlowerKind.Acuariana), BouquetKind.Peace },
            { Key(FlowerKind.Brasa,     FlowerKind.Crisalida), BouquetKind.HiddenDesire },
            { Key(FlowerKind.Velada,    FlowerKind.Acuariana), BouquetKind.Comfort },
            { Key(FlowerKind.Brasa,     FlowerKind.Inquieta),  BouquetKind.Obsession },
            { Key(FlowerKind.Sol,       FlowerKind.Anima),     BouquetKind.Promise },
            { Key(FlowerKind.Crisalida, FlowerKind.Anima),     BouquetKind.Confession },
            { Key(FlowerKind.Velada,    FlowerKind.Brasa),     BouquetKind.Farewell },
            { Key(FlowerKind.Inquieta,  FlowerKind.Crisalida), BouquetKind.Suspicion },
        };

        /// <summary>Returns BouquetKind.None if the pair has no defined recipe.</summary>
        public static BouquetKind Resolve(FlowerKind a, FlowerKind b)
        {
            if (a == FlowerKind.None || b == FlowerKind.None) return BouquetKind.None;
            return Recipes.TryGetValue(Key(a, b), out var bouquet) ? bouquet : BouquetKind.None;
        }

        public static bool IsValidCombination(FlowerKind a, FlowerKind b)
            => Resolve(a, b) != BouquetKind.None;

        public static IEnumerable<KeyValuePair<long, BouquetKind>> AllRecipes => Recipes;
    }
}
