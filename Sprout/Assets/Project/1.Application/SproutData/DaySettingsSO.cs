using UnityEngine;

namespace Sprout.Data
{
    [CreateAssetMenu(fileName = "DaySettings", menuName = "Sprout/Day Settings")]
    public class DaySettingsSO : ScriptableObject
    {
        [Min(1)]
        [Tooltip("Number of in-game days before the ending is resolved.")]
        public int totalDays = 3;

        [Tooltip("If true, phase advances automatically after this many seconds. " +
                 "If false, phases advance via gameplay events / the rest button.")]
        public bool autoAdvancePhases = false;

        [Min(5f)]
        public float secondsPerPhase = 120f;

        [Tooltip("Flowers granted to the player at the very start of day 1.")]
        public int startingSeedFlowers = 0;
    }
}
