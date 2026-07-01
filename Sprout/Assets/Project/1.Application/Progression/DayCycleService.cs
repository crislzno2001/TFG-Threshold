using UnityEngine;
using UnityEngine.Events;
using Sprout.Domain.DayCycle;

namespace Sprout.Application
{
    /// <summary>
    /// MonoBehaviour wrapper around DayCycleState. Advancing into Night runs the
    /// gossip service; completing the final day triggers the ending.
    /// Phases advance via AdvancePhase() (e.g. a "rest" button / sleep object) or
    /// automatically if DaySettings.autoAdvancePhases is on.
    /// </summary>
    public class DayCycleService : MonoBehaviour
    {
        [SerializeField] private NightGossipService gossip;
        [SerializeField] private EndingService ending;

        [Header("Events (for UI)")]
        public UnityEvent<int, string> onPhaseChanged; // (day, phase name)
        public UnityEvent<int> onNewDay;

        private SproutGameDirector D => SproutGameDirector.Instance;
        private float _phaseTimer;

        private void Start()
        {
            if (D == null) { enabled = false; return; }

            D.Day.OnPhaseChanged += HandlePhaseChanged;
            D.Day.OnNewDay += d => onNewDay?.Invoke(d);
            D.Day.OnFinalDayCompleted += HandleFinalDay;

            HandlePhaseChanged(D.Day.Day, D.Day.Phase);
        }

        private void Update()
        {
            var settings = D != null ? D.DaySettings : null;
            // Avanza solo hasta la NOCHE; al llegar la noche se para y hay que ir a dormir para pasar de día.
            if (settings == null || !settings.autoAdvancePhases || D.Day.IsFinished || D.Day.Phase == DayPhase.Night) return;

            _phaseTimer += Time.deltaTime;
            if (_phaseTimer >= settings.secondsPerPhase)
            {
                _phaseTimer = 0f;
                AdvancePhase();
            }
        }

        /// <summary>Public entry point — call from a "go to sleep / rest" interaction.</summary>
        public void AdvancePhase()
        {
            if (D == null || D.Day.IsFinished) return;
            D.Day.Advance();
        }

        private void HandlePhaseChanged(int day, DayPhase phase)
        {
            _phaseTimer = 0f;
            onPhaseChanged?.Invoke(day, phase.ToString());

            if (phase == DayPhase.Night && gossip != null)
                gossip.RunNight();
        }

        private void HandleFinalDay()
        {
            if (ending != null) ending.ResolveAndShow();
        }
    }
}
