using UnityEngine;
using Sprout.Domain.DayCycle;

namespace Sprout.Data
{
    [CreateAssetMenu(fileName = "DaySettings", menuName = "Sprout/Day Settings")]
    public class DaySettingsSO : ScriptableObject
    {
        [Min(1)]
        [Tooltip("Number of in-game days before the ending is resolved.")]
        public int totalDays = 3;

        [Header("Avance de fase por PROGRESO (recomendado)")]
        [Tooltip("Nº de NPCs distintos con los que hablar en una fase para que AVANCE sola. " +
                 "Así la fase pasa según lo que has hecho, no por reloj.")]
        [Min(1)] public int npcsToAdvancePhase = 4;

        [Header("Avance por TIEMPO (opcional, red de seguridad)")]
        [Tooltip("Si true, además avanza al llegar al TOPE de tiempo de la fase aunque no hayas hecho nada. " +
                 "Déjalo en FALSE para que solo avance por progreso.")]
        public bool autoAdvancePhases = false;

        [Min(5f)]
        [Tooltip("Fallback si no se usan las duraciones por fase de abajo.")]
        public float secondsPerPhase = 120f;

        [Header("Duración por fase (solo si autoAdvancePhases está ON)")]
        [Min(5f)] public float secondsMorning = 300f;    // mañana  ~5 min
        [Min(5f)] public float secondsMidday = 240f;     // mediodía ~4 min
        [Min(5f)] public float secondsAfternoon = 240f;  // tarde   ~4 min
        [Min(5f)] public float secondsNight = 120f;      // noche   ~2 min (normalmente se cierra al dormir)

        [Tooltip("Flowers granted to the player at the very start of day 1.")]
        public int startingSeedFlowers = 0;

        /// <summary>Tope de tiempo (segundos) de la fase indicada.</summary>
        public float SecondsFor(DayPhase phase) => phase switch
        {
            DayPhase.Morning   => secondsMorning,
            DayPhase.Midday    => secondsMidday,
            DayPhase.Afternoon => secondsAfternoon,
            DayPhase.Night     => secondsNight,
            _                  => secondsPerPhase
        };
    }
}
