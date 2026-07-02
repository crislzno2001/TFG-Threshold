using System;

namespace Sprout.Domain.DayCycle
{
    public enum DayPhase
    {
        Morning,    // mañana
        Midday,     // mediodía
        Afternoon,  // tarde
        Night       // noche — hay cotilleo y luego pasa el día
    }

    /// <summary>
    /// Tracks the current day and phase. Pure C#. The game ends after
    /// <see cref="TotalDays"/> days have completed.
    /// </summary>
    [Serializable]
    public class DayCycleState
    {
        public int Day { get; private set; } = 1;
        public DayPhase Phase { get; private set; } = DayPhase.Morning;
        public int TotalDays { get; }

        public event Action<int, DayPhase> OnPhaseChanged; // (day, newPhase)
        public event Action<int> OnNewDay;                 // (new day number)
        public event Action OnFinalDayCompleted;

        public DayCycleState(int totalDays = 3)
        {
            TotalDays = Math.Max(1, totalDays);
        }

        public bool IsNight => Phase == DayPhase.Night;
        public bool IsFinished => Day > TotalDays;

        /// <summary>Advance to the next phase; rolls into a new day after Night.</summary>
        public void Advance()
        {
            if (IsFinished) return;

            switch (Phase)
            {
                case DayPhase.Morning:   SetPhase(DayPhase.Midday); break;
                case DayPhase.Midday:    SetPhase(DayPhase.Afternoon); break;
                case DayPhase.Afternoon: SetPhase(DayPhase.Night); break;
                case DayPhase.Night:     RollOverDay(); break;
            }
        }

        private void RollOverDay()
        {
            if (Day >= TotalDays)
            {
                Day++; // moves past TotalDays → IsFinished
                OnFinalDayCompleted?.Invoke();
                return;
            }

            Day++;
            SetPhase(DayPhase.Morning);
            OnNewDay?.Invoke(Day);
        }

        private void SetPhase(DayPhase p)
        {
            Phase = p;
            OnPhaseChanged?.Invoke(Day, p);
        }

        public void Load(int day, DayPhase phase)
        {
            Day = Math.Max(1, day);
            Phase = phase;
            OnPhaseChanged?.Invoke(Day, Phase);
        }
    }
}
