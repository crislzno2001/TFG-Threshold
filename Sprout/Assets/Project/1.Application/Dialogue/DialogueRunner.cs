using UnityEngine;
using UnityEngine.Events;
using ThresholdGame.Application.NPC;
using ThresholdGame.Presentation.NPC;

namespace OpenAI.Dialogue
{
    /// <summary>
    /// Procesa los mensajes del jugador durante el diálogo.
    ///
    /// Flujo:
    ///   1. Llega un mensaje del jugador.
    ///   2. NPCOrderInterpreter lo analiza.
    ///   3a. Si es una orden → NPCStateMachine la ejecuta → respuesta de confirmación (sin IA).
    ///   3b. Si no es una orden → flujo normal de NPCBrain + OpenAI.
    /// </summary>
    public class DialogueRunner : MonoBehaviour
    {
        // ── Eventos para sistemas externos (Torrance tracker, analytics, etc.) ─
        [Header("Eventos")]
        [Tooltip("Se dispara después de cada paso de diálogo, pasando el nodo en el que se ha quedado el runner.")]
        public UnityEvent<DialogueNodeSO> onStepCompleted;

        private NPCBrain _brain;
        private NPCStateMachine _npcStateMachine;
        private NPCOrderInterpreter _interpreter;
        private DialogueNodeSO _current;

        private void Awake()
        {
            _brain = GetComponent<NPCBrain>();
            _npcStateMachine = GetComponent<NPCStateMachine>();
            _interpreter = new NPCOrderInterpreter();
        }

        // ── Diálogo ────────────────────────────────────────────────────────────

        public void StartDialogue()
        {
            if (_brain == null || _brain.dialogueGraph == null || _brain.dialogueGraph.entryNode == null)
                return;

            _brain.ResetHistory();
            AdvanceTo(_brain.dialogueGraph.entryNode);
        }

        public void AdvanceTo(DialogueNodeSO node)
        {
            if (node == null) return;
            _current = node;
            _brain.SetNode(node);
        }

        public void AdvanceToNext()
        {
            if (_current?.nextNodes?.Count == 1)
                AdvanceTo(_current.nextNodes[0]);
        }

        public async System.Threading.Tasks.Task<string> ProcessMessage(string userMessage)
        {
            if (_brain == null || _current == null) return "...";

            // ── 1. Intentar interpretar como orden ─────────────────────────────
            if (_npcStateMachine != null)
            {
                NPCOrder order = _interpreter.Interpret(userMessage);

                if (order.IsOrder)
                {
                    bool executed = _npcStateMachine.ExecuteOrder(order);

                    if (executed)
                    {
                        onStepCompleted?.Invoke(_current);
                        return ConfirmationFor(order);
                    }

                    if (order.Type == NPCOrderType.MoveToDestination)
                    {
                        onStepCompleted?.Invoke(_current);
                        return "No conozco ese lugar.";
                    }
                }
            }

            // ── 2. Diálogo normal con OpenAI ───────────────────────────────────
            DialogueStepResult result = await _brain.ProcessStep(userMessage, _current);

            if (result?.NextNode != null && result.NextNode != _current)
                AdvanceTo(result.NextNode);

            // 🔔 Notificar al tracker / otros sistemas
            onStepCompleted?.Invoke(_current);

            return string.IsNullOrWhiteSpace(result?.Reply) ? "..." : result.Reply;
        }

        public DialogueNodeSO Current => _current;

        // ── Respuestas de confirmación ─────────────────────────────────────────

        private static string ConfirmationFor(NPCOrder order) => order.Type switch
        {
            NPCOrderType.Follow => "Entendido. Te sigo.",
            NPCOrderType.Stop => "De acuerdo. Me quedo aquí.",
            NPCOrderType.ReturnHome => "Volviendo a mi posición.",
            NPCOrderType.MoveToDestination => "Me dirijo hacia allí.",
            _ => "Orden recibida."
        };
    }
}