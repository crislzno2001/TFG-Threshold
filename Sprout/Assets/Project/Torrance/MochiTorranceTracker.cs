using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using OpenAI;
using OpenAI.Dialogue;

namespace Sprout.Evaluation
{
    /// <summary>
    /// Mide la creatividad divergente del jugador (Test de Torrance) durante
    /// la conversación con Mochi.
    ///
    /// Fase 1: Fluidez — cuenta ideas culinarias propuestas en B2.
    ///
    /// Cada vez que el DialogueRunner se queda en B2 (la IA decidió que el jugador
    /// no quería salir), el tracker hace una llamada IA pequeña para clasificar:
    /// ¿el mensaje del jugador era realmente una idea culinaria, o solo una
    /// pregunta / comentario / saludo? Solo cuenta como idea si es "si".
    /// </summary>
    public class MochiTorranceTracker : MonoBehaviour
    {
        // ── Referencias ───────────────────────────────────────────────────────
        [Header("Referencias")]
        [SerializeField] private NPCBrain _brain;

        [Header("Nodos relevantes")]
        [Tooltip("Nodo B2: lluvia de ideas. Cada idea válida en este nodo cuenta como fluidez.")]
        [SerializeField] private DialogueNodeSO _ideasNode;

        // ── Configuración del clasificador ─────────────────────────────────────
        [Header("Clasificador de ideas (OpenAI)")]
        [SerializeField] private string _classifierModel = "gpt-4o-mini";

        // ── Configuración narrativa ───────────────────────────────────────────
        [Header("Umbrales narrativos")]
        [Tooltip("Ideas necesarias para considerar fluidez alta (afecta veredicto B10)")]
        [SerializeField] private int _highFluencyThreshold = 4;

        // ── Eventos ───────────────────────────────────────────────────────────
        [Header("Eventos")]
        public UnityEvent<int> onIdeaCountChanged;

        // ── Debug visible en Inspector ────────────────────────────────────────
        [Header("📊 Estado en runtime (solo lectura)")]
        [SerializeField] private int _debug_ideaCount = 0;
        [SerializeField] private string _debug_currentNode = "";
        [SerializeField] private string _debug_fluencyLevel = "ninguna";
        [SerializeField] private List<string> _debug_validIdeas = new();
        [SerializeField] private List<string> _debug_rejectedMessages = new();

        // ── Estado interno ────────────────────────────────────────────────────
        private OpenAIApi _openai;
        private DialogueNodeSO _previousNode;
        private string _lastPlayerMessage = "";
        private bool _classifying = false;

        // ── Unity ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            _openai = new OpenAIApi();
        }

        // ── API pública ───────────────────────────────────────────────────────

        public int IdeaCount => _debug_ideaCount;

        /// <summary>Conectar desde DialogueUI.onPlayerMessageSent.</summary>
        public void RegisterPlayerMessage(string message)
        {
            _lastPlayerMessage = message ?? "";
        }

        /// <summary>Conectar desde DialogueRunner.onStepCompleted.</summary>
        public async void OnDialogueStepCompleted(DialogueNodeSO currentNode)
        {
            if (_brain == null || _ideasNode == null || currentNode == null) return;
            if (_classifying) return; // evita doble clasificación si llegan eventos rápidos

            _debug_currentNode = currentNode.name;

            bool wasInIdeasNode = _previousNode == _ideasNode;
            bool stillInIdeasNode = currentNode == _ideasNode;

            _previousNode = currentNode;

            // Solo clasificar si estábamos y seguimos en el nodo de ideas
            if (!wasInIdeasNode || !stillInIdeasNode) return;
            if (string.IsNullOrWhiteSpace(_lastPlayerMessage)) return;

            string messageBeingClassified = _lastPlayerMessage;

            _classifying = true;
            bool isCulinaryIdea = await ClassifyAsCulinaryIdea(messageBeingClassified);
            _classifying = false;

            if (isCulinaryIdea)
            {
                _debug_ideaCount++;
                _debug_validIdeas.Add(messageBeingClassified);
                UpdateFluencyLevel();
                UpdateFlags();
                onIdeaCountChanged?.Invoke(_debug_ideaCount);
                Debug.Log($"[Torrance] 💡 Idea {_debug_ideaCount}: \"{messageBeingClassified}\"");
            }
            else
            {
                _debug_rejectedMessages.Add(messageBeingClassified);
                Debug.Log($"[Torrance] ❌ No es idea: \"{messageBeingClassified}\"");
            }
        }

        public void ResetCount()
        {
            _debug_ideaCount = 0;
            _debug_validIdeas.Clear();
            _debug_rejectedMessages.Clear();
            _debug_fluencyLevel = "ninguna";
            _debug_currentNode = "";
            _previousNode = null;
        }

        // ── Clasificador IA ───────────────────────────────────────────────────

        private async Task<bool> ClassifyAsCulinaryIdea(string playerMessage)
        {
            if (string.IsNullOrWhiteSpace(playerMessage)) return false;

            var sb = new StringBuilder();
            sb.AppendLine("Clasifica el siguiente mensaje del jugador.");
            sb.AppendLine("Responde SOLO 'si' o 'no'. Sin texto adicional.");
            sb.AppendLine();
            sb.AppendLine("Devuelve 'si' SOLO si el mensaje propone una idea o sugerencia culinaria concreta:");
            sb.AppendLine("- combinar ingredientes");
            sb.AppendLine("- una técnica de cocción");
            sb.AppendLine("- un plato o preparación");
            sb.AppendLine("- una forma de presentar la comida");
            sb.AppendLine();
            sb.AppendLine("Devuelve 'no' si el mensaje es:");
            sb.AppendLine("- una pregunta sobre los ingredientes");
            sb.AppendLine("- una pregunta sobre el cliente o la situación");
            sb.AppendLine("- un saludo, despedida o comentario social");
            sb.AppendLine("- una expresión de frustración o sin idea concreta");
            sb.AppendLine();
            sb.AppendLine($"Mensaje del jugador: \"{playerMessage}\"");
            sb.AppendLine();
            sb.AppendLine("¿Es una idea culinaria concreta? Responde 'si' o 'no'.");

            var req = new CreateChatCompletionRequest
            {
                model = _classifierModel,
                messages = new List<ChatMessage>
                {
                    new ChatMessage
                    {
                        role = "system",
                        content = "Eres un clasificador binario. Responde solo 'si' o 'no'."
                    },
                    new ChatMessage
                    {
                        role = "user",
                        content = sb.ToString()
                    }
                },
                temperature = 0f,
                max_tokens = 3
            };

            try
            {
                var response = await _openai.CreateChatCompletion(req);
                if (response?.choices == null || response.choices.Count == 0) return false;

                string raw = response.choices[0].message.content?.Trim().ToLowerInvariant() ?? "";
                return raw.StartsWith("si") || raw.StartsWith("sí") || raw.StartsWith("yes");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Torrance] Error clasificando idea: {ex.Message}");
                return false;
            }
        }

        // ── Privado ───────────────────────────────────────────────────────────

        private void UpdateFluencyLevel()
        {
            if (_debug_ideaCount >= _highFluencyThreshold)
                _debug_fluencyLevel = "alta ✨";
            else if (_debug_ideaCount >= 2)
                _debug_fluencyLevel = "media";
            else
                _debug_fluencyLevel = "baja";
        }

        private void UpdateFlags()
        {
            if (_debug_ideaCount >= _highFluencyThreshold)
            {
                _brain.SetFlag("mochi_fluidez_alta", true);
                Debug.Log("[Torrance] 🎯 Flag activado: mochi_fluidez_alta");
            }
        }
    }
}