using System;
using System.Collections.Generic;

namespace Sprout.Domain.Creativity
{
    /// <summary>
    /// Torrance-inspired creativity dimensions, scored invisibly from player text.
    /// All values are accumulated; the UI never shows the raw numbers.
    /// </summary>
    [Serializable]
    public struct CreativityScores
    {
        public int Fluency;      // count of distinct ideas
        public float Originality; // rarity / unexpectedness  (0..1 averaged)
        public float Elaboration; // detail / specificity     (0..1 averaged)
        public int Flexibility;  // category switches after rejection

        public static CreativityScores Zero => new CreativityScores();
    }

    /// <summary>
    /// Aggregates creativity across a single conversation / NPC. Pure C#.
    /// Originality & Elaboration are kept as running averages of per-idea AI
    /// scores in [0,1]; Fluency & Flexibility are counts.
    /// </summary>
    [Serializable]
    public class CreativityProfile
    {
        private int _ideaCount;
        private float _originalitySum;
        private float _elaborationSum;
        private int _flexibility;
        private readonly HashSet<string> _categories = new();
        private string _lastCategory;

        public void AddIdea(float originality01, float elaboration01, string category = null)
        {
            _ideaCount++;
            _originalitySum += Clamp01(originality01);
            _elaborationSum += Clamp01(elaboration01);

            if (!string.IsNullOrWhiteSpace(category))
            {
                category = category.Trim().ToLowerInvariant();
                _categories.Add(category);
                if (_lastCategory != null && _lastCategory != category)
                    _flexibility++;
                _lastCategory = category;
            }
        }

        public CreativityScores Snapshot() => new CreativityScores
        {
            Fluency = _ideaCount,
            Originality = _ideaCount > 0 ? _originalitySum / _ideaCount : 0f,
            Elaboration = _ideaCount > 0 ? _elaborationSum / _ideaCount : 0f,
            Flexibility = _flexibility
        };

        public int Fluency => _ideaCount;
        public bool HighFluency(int threshold) => _ideaCount >= threshold;
        public bool HighOriginality(float threshold = 0.6f)
            => _ideaCount > 0 && (_originalitySum / _ideaCount) >= threshold;
        public bool HighElaboration(float threshold = 0.6f)
            => _ideaCount > 0 && (_elaborationSum / _ideaCount) >= threshold;
        public int DistinctCategories => _categories.Count;

        public void Reset()
        {
            _ideaCount = 0;
            _originalitySum = 0f;
            _elaborationSum = 0f;
            _flexibility = 0;
            _categories.Clear();
            _lastCategory = null;
        }

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
