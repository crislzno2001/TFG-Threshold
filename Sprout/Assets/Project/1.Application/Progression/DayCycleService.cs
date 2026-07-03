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

        [Header("Avance de fase")]
        [Tooltip("Si true, HABLAR con un NPC ya cuenta como progreso de fase. " +
                 "Si false, solo cuentan los NPCs que TERMINAN su contenido (nodo day_done vía NpcPhaseDoneReporter).")]
        [SerializeField] private bool countTalkAsPhaseProgress = true;

        [Header("Events (for UI)")]
        public UnityEvent<int, string> onPhaseChanged; // (day, phase name)
        public UnityEvent<int> onNewDay;

        private SproutGameDirector D => SproutGameDirector.Instance;
        private float _phaseTimer;

        // NPCs distintos con los que ya se ha hablado en la fase actual (para avanzar por progreso).
        private readonly System.Collections.Generic.HashSet<string> _phaseGoals = new();

        /// <summary>
        /// Registra que has completado un "hito" de la fase actual (p. ej. hablar con un NPC).
        /// Cuando hay suficientes NPCs distintos, la fase avanza SOLA (pacing por progreso).
        /// La NOCHE no avanza así: se cierra al ir a dormir.
        /// </summary>
        public void RegisterPhaseGoal(string id)
        {
            if (D == null || D.Day.IsFinished || D.Day.Phase == DayPhase.Night) return;
            if (string.IsNullOrWhiteSpace(id)) return;

            _phaseGoals.Add(id.Trim());

            var settings = D.DaySettings;
            int target = settings != null ? Mathf.Max(1, settings.npcsToAdvancePhase) : 4;
            if (_phaseGoals.Count >= target)
                AdvancePhase();
        }

        /// <summary>
        /// Lo llama el trigger al CERRAR una conversación. Solo cuenta como progreso si
        /// 'countTalkAsPhaseProgress' está activo (modo simple). En modo "por contenido"
        /// se ignora y solo cuentan los NpcPhaseDoneReporter.
        /// </summary>
        public void RegisterPhaseGoalFromTalk(string id)
        {
            if (countTalkAsPhaseProgress) RegisterPhaseGoal(id);
        }

        // ── Guardado del progreso de fase (para continuar sin perderlo) ─────────

        /// <summary>NPCs con los que ya has hablado en la fase actual (para guardar).</summary>
        public System.Collections.Generic.IReadOnlyCollection<string> ExportPhaseGoals() => _phaseGoals;

        /// <summary>Restaura el progreso de fase al cargar partida (no avanza la fase, solo repuebla).</summary>
        public void ImportPhaseGoals(System.Collections.Generic.IEnumerable<string> goals)
        {
            _phaseGoals.Clear();
            if (goals == null) return;
            foreach (var g in goals)
                if (!string.IsNullOrWhiteSpace(g)) _phaseGoals.Add(g.Trim());
        }

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

            // Tope de tiempo de ESTA fase (mañana/mediodía/tarde). La noche ya se filtró arriba.
            _phaseTimer += Time.deltaTime;
            if (_phaseTimer >= settings.SecondsFor(D.Day.Phase))
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
            _phaseGoals.Clear();   // empieza fase nueva → objetivo de progreso reiniciado

            // Al cambiar de fase, cada NPC empieza por el nodo de entrada de la fase nueva
            // (en vez de retomar la despedida de la fase anterior).
            foreach (var r in FindObjectsByType<OpenAI.Dialogue.DialogueRunner>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (r != null) r.ResetForNewPhase();

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
