using System;
using System.Collections.Generic;

namespace Sprout.Domain.Creativity
{
    /// <summary>
    /// Dimensiones de creatividad (inspiradas en Torrance + rúbrica ampliada), puntuadas de forma
    /// INVISIBLE a partir del texto del jugador. La UI nunca muestra los números en crudo.
    /// Fluency y Flexibility son conteos; el resto son medias en [0,1].
    /// </summary>
    [Serializable]
    public struct CreativityScores
    {
        public int Fluency;        // nº de ideas concretas propuestas
        public float Originality;  // rareza / cosa inesperada
        public float Elaboration;  // detalle / concreción
        public float Coherence;    // encaja con el problema y el mundo
        public float Empathy;      // tiene en cuenta la emoción del personaje
        public float Adaptation;   // mejora/revisa una idea tras una objeción
        public float WorldUse;     // usa flores, comida, noche, pueblo, rumores…
        public float Risk;         // se atreve con algo raro pero con sentido
        public int Flexibility;    // cambios de categoría de idea
        public float Overall;      // mezcla global de las dimensiones de calidad

        public static CreativityScores Zero => new CreativityScores();
    }

    /// <summary>
    /// Agrega la creatividad de una conversación / NPC. C# puro.
    /// Cada dimensión de calidad se guarda como suma PONDERADA + peso total, para poder dar
    /// más importancia a los mensajes enviados en nodos de "reto creativo".
    /// </summary>
    [Serializable]
    public class CreativityProfile
    {
        private int _ideaCount;

        // sumas ponderadas por dimensión y peso total acumulado
        private float _wOrig, _wElab, _wCoh, _wEmp, _wAdapt, _wWorld, _wRisk;
        private float _totalWeight;

        private int _flexibility;
        private readonly HashSet<string> _categories = new();
        private string _lastCategory;

        /// <summary>
        /// Registra la evaluación de UN mensaje del jugador. Todas las dimensiones en [0,1].
        /// 'weight' permite dar más peso a los retos creativos (p. ej. 2x).
        /// </summary>
        public void AddEvaluation(
            bool isIdea,
            float originality01, float detail01, float coherence01,
            float empathy01, float worldUse01, float risk01, float adaptation01,
            string category = null, float weight = 1f)
        {
            if (weight <= 0f) weight = 1f;
            if (isIdea) _ideaCount++;

            _wOrig  += Clamp01(originality01) * weight;
            _wElab  += Clamp01(detail01)      * weight;
            _wCoh   += Clamp01(coherence01)   * weight;
            _wEmp   += Clamp01(empathy01)     * weight;
            _wWorld += Clamp01(worldUse01)    * weight;
            _wRisk  += Clamp01(risk01)        * weight;
            _wAdapt += Clamp01(adaptation01)  * weight;
            _totalWeight += weight;

            if (!string.IsNullOrWhiteSpace(category))
            {
                category = category.Trim().ToLowerInvariant();
                _categories.Add(category);
                if (_lastCategory != null && _lastCategory != category) _flexibility++;
                _lastCategory = category;
            }
        }

        /// <summary>Compat con el tracker antiguo: una idea "clásica" (solo originalidad/elaboración/categoría).</summary>
        public void AddIdea(float originality01, float elaboration01, string category = null)
            => AddEvaluation(true, originality01, elaboration01, 0f, 0f, 0f, 0f, 0f, category, 1f);

        private float Avg(float weightedSum) => _totalWeight > 0f ? weightedSum / _totalWeight : 0f;

        public CreativityScores Snapshot()
        {
            var s = new CreativityScores
            {
                Fluency = _ideaCount,
                Originality = Avg(_wOrig),
                Elaboration = Avg(_wElab),
                Coherence = Avg(_wCoh),
                Empathy = Avg(_wEmp),
                Adaptation = Avg(_wAdapt),
                WorldUse = Avg(_wWorld),
                Risk = Avg(_wRisk),
                Flexibility = _flexibility
            };
            // Mezcla global: media de las 7 dimensiones de calidad (fluidez/flexibilidad son conteos aparte).
            s.Overall = (s.Originality + s.Elaboration + s.Coherence +
                         s.Empathy + s.Adaptation + s.WorldUse + s.Risk) / 7f;
            return s;
        }

        // ── Consultas para flags/eventos ──────────────────────────────────────
        public int Fluency => _ideaCount;
        public int DistinctCategories => _categories.Count;
        public bool HighFluency(int threshold) => _ideaCount >= threshold;
        public bool HighOriginality(float threshold = 0.6f) => _totalWeight > 0f && Avg(_wOrig) >= threshold;
        public bool HighElaboration(float threshold = 0.6f) => _totalWeight > 0f && Avg(_wElab) >= threshold;
        public bool HighEmpathy(float threshold = 0.6f) => _totalWeight > 0f && Avg(_wEmp) >= threshold;
        public bool HighAdaptation(float threshold = 0.6f) => _totalWeight > 0f && Avg(_wAdapt) >= threshold;
        public bool HighWorldUse(float threshold = 0.6f) => _totalWeight > 0f && Avg(_wWorld) >= threshold;
        public bool HighRisk(float threshold = 0.6f) => _totalWeight > 0f && Avg(_wRisk) >= threshold;

        public void Reset()
        {
            _ideaCount = 0;
            _wOrig = _wElab = _wCoh = _wEmp = _wAdapt = _wWorld = _wRisk = 0f;
            _totalWeight = 0f;
            _flexibility = 0;
            _categories.Clear();
            _lastCategory = null;
        }

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
