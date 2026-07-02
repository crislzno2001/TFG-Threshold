using System.Collections.Generic;
using UnityEngine;
using OpenAI.Dialogue;

namespace ThresholdGame.Presentation.NPC
{
    /// <summary>
    /// Avisa al DayCycleService de que ESTE NPC ya ha terminado su contenido de la fase actual
    /// (cuando la conversación llega a uno de sus nodos "day_done" / EXHAUSTED_DAY). Así la fase
    /// avanza por PROGRESO REAL (has hecho lo de cada NPC), no solo por haber hablado.
    ///
    /// Ponlo en el mismo GameObject que el NPCBrain/DialogueRunner y arrastra en 'phaseDoneNodes'
    /// los nodos que cierran el contenido de ese NPC en cada fase.
    /// Para usarlo, pon en el DayCycleService "Count Talk As Phase Progress = false".
    /// </summary>
    public sealed class NpcPhaseDoneReporter : MonoBehaviour
    {
        [SerializeField] private NPCBrain brain;
        [SerializeField] private DialogueRunner runner;

        [Tooltip("Nodos que marcan que este NPC ya terminó su contenido de la fase (p. ej. ASTER_A_EXHAUSTED_DAY).")]
        [SerializeField] private List<DialogueNodeSO> phaseDoneNodes = new();

        private Sprout.Application.DayCycleService _cycle;

        private void Awake()
        {
            if (brain == null) brain = GetComponent<NPCBrain>();
            if (runner == null) runner = GetComponent<DialogueRunner>();
        }

        private void Start()
        {
            if (runner != null) runner.onStepCompleted.AddListener(OnStep);
        }

        private void OnDestroy()
        {
            if (runner != null) runner.onStepCompleted.RemoveListener(OnStep);
        }

        private void OnStep(DialogueNodeSO node)
        {
            if (node == null || !phaseDoneNodes.Contains(node)) return;

            if (_cycle == null) _cycle = FindFirstObjectByType<Sprout.Application.DayCycleService>();
            _cycle?.RegisterPhaseGoal(brain != null ? brain.npcName : name);
        }
    }
}
