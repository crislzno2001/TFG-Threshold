using System.Collections.Generic;
using UnityEngine;
using OpenAI;

namespace OpenAI.Dialogue
{
    public class NPCBrain : MonoBehaviour
    {
        [Header("Interacción")]
        public bool isInteracting = false;
        [Header("Identidad")]
        public string npcName = "Gareth";

        [TextArea(4, 10)]
        public string personality = "Eres un tabernero medieval llamado Gareth. Eres gruñón pero en el fondo tienes buen corazón. Hablas de forma informal. Sabes todo sobre la taberna y el pueblo. Responde siempre en menos de 3 frases cortas.";

        [Header("OpenAI")]
        [SerializeField] private string model = "gpt-4o-mini";
        [Range(0f, 2f)]
        [SerializeField] private float temperature = 0.8f;
        [SerializeField] private int maxTokens = 150;

        [Header("Historial conversación")]
        [SerializeField] private int maxHistoryMessages = 6;

        [Header("Memoria NPC")]
        [SerializeField] private int maxMemories = 5;

        private OpenAIApi openai;
        private List<ChatMessage> history = new List<ChatMessage>();

        // memoria persistente
        private Dictionary<string, string> memory = new Dictionary<string, string>();

        [Header("Grafo de diálogo")]
        public DialogueGraphSO dialogueGraph;

        private DialogueNodeSO _currentNode;

        private void Awake()
        {
            openai = new OpenAIApi();
            ResetHistory();
        }

        public void ResetHistory()
        {
            history.Clear();
            history.Add(new ChatMessage { role = "system", content = personality });
        }

        public async System.Threading.Tasks.Task<string> NPCSendMessage(string userMessage)
        {
            if (!isInteracting)
            {
                Debug.Log("[Brain] No hay interacción activa");
                return "";
            }

            Debug.Log($"[Brain] currentNode: {_currentNode?.name ?? "NULL"}");
            DetectMemory(userMessage);

            if (_currentNode is SpeechNodeSO speechNode && speechNode.transitions?.Count > 0)
            {
                int idx = await EvaluateTransition(userMessage,
                    speechNode.transitions.ConvertAll(t => (t.condition, t.targetNode)));
                if (idx >= 0 && idx < speechNode.transitions.Count)
                    if (speechNode.transitions[idx].targetNode != null)
                        SetNode(speechNode.transitions[idx].targetNode);
            }
            else if (_currentNode is SpeechNodeSO speechSimple && speechSimple.nextNodes?.Count > 0)
            {
                SetNode(speechSimple.nextNodes[0]);
                if (_currentNode is ChoiceNodeSO nextChoice && nextChoice.choices?.Count > 0)
                {
                    int idx = await EvaluateTransition(userMessage,
                        nextChoice.choices.ConvertAll(c => (c.condition, (DialogueNodeSO)c.nextNode)));
                    if (idx >= 0 && idx < nextChoice.choices.Count)
                        if (nextChoice.choices[idx].nextNode != null)
                            SetNode(nextChoice.choices[idx].nextNode);
                }
            }
            else if (_currentNode is ChoiceNodeSO choiceNode && choiceNode.choices?.Count > 0)
            {
                int idx = await EvaluateTransition(userMessage,
                    choiceNode.choices.ConvertAll(c => (c.condition, (DialogueNodeSO)c.nextNode)));
                if (idx >= 0 && idx < choiceNode.choices.Count)
                    if (choiceNode.choices[idx].nextNode != null)
                        SetNode(choiceNode.choices[idx].nextNode);
            }

            history.Add(new ChatMessage { role = "user", content = userMessage });
            TrimHistory();

            // ... resto del método igual
            var req = new CreateChatCompletionRequest
            {
                model = model,
                messages = history,
                temperature = temperature,
                max_tokens = maxTokens
            };

            var response = await openai.CreateChatCompletion(req);

            if (response?.choices == null || response.choices.Count == 0)
                return "...";

            string reply = response.choices[0].message.content;

            history.Add(new ChatMessage
            {
                role = "assistant",
                content = reply
            });

            return reply;
        }
        private async System.Threading.Tasks.Task<int> EvaluateTransition(
            string userMessage, List<(string condition, DialogueNodeSO target)> options)
        {
            if (options == null || options.Count == 0) return -1;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Eres un clasificador semántico para un juego de rol.");
            sb.AppendLine("Tu tarea es clasificar la intención del jugador.");
            sb.AppendLine("Elige la opción que mejor describa la INTENCIÓN del mensaje.");
            sb.AppendLine("No compares palabras exactas, sino significado.");
            sb.AppendLine();

            // Añadir contexto actual del nodo
            if (_currentNode != null && !string.IsNullOrEmpty(_currentNode.contextForAI))
                sb.AppendLine($"Contexto actual del nodo: {_currentNode.contextForAI}");

            // Añadir memoria relevante
            if (memory.Count > 0)
            {
                sb.AppendLine("Recuerdos del NPC sobre el jugador:");
                foreach (var kv in memory)
                    sb.AppendLine($"{kv.Key}: {kv.Value}");
            }

            sb.AppendLine();
            sb.AppendLine("Opciones disponibles:");
            for (int i = 0; i < options.Count; i++)
                sb.AppendLine($"{i}: {options[i].condition}");

            sb.AppendLine();
            sb.AppendLine($"Mensaje del jugador: \"{userMessage}\"");
            sb.AppendLine("Responde SOLO con el número entero correspondiente a la opción correcta. Nada más.");
            sb.AppendLine("Si ninguna opción encaja claramente, responde -1.");

            var req = new CreateChatCompletionRequest
            {
                model = model,
                messages = new List<ChatMessage>
        {
            new ChatMessage { role = "system", content = "Eres un clasificador de intenciones. Responde SOLO con un número." },
            new ChatMessage { role = "user", content = sb.ToString() }
        },
                temperature = 0f,
                max_tokens = 5
            };

            var response = await openai.CreateChatCompletion(req);

            if (response?.choices == null || response.choices.Count == 0)
                return -1;

            string raw = response.choices[0].message.content.Trim();
            raw = System.Text.RegularExpressions.Regex.Match(raw, @"-?\d+").Value;

            int result = int.TryParse(raw, out int r) ? r : -1;

            if (result < 0 || result >= options.Count)
            {
                Debug.Log("[Dialogue] transición inválida");
                return -1;
            }

            Debug.Log($"[Dialogue] transición → {userMessage} → {result}");

            return result;
        }

        private void TrimHistory()
        {
            while (history.Count > maxHistoryMessages + 1)
                history.RemoveAt(1);
        }

        public void SetNode(DialogueNodeSO node)
        {
            _currentNode = node;
            RebuildSystemPrompt();
        }

        private void RebuildSystemPrompt()
        {
            string context = "";

            if (_currentNode != null && !string.IsNullOrEmpty(_currentNode.contextForAI))
                context += $"\n\nContexto actual de la historia:\n{_currentNode.contextForAI}";

            context += BuildMemoryContext();

            history[0] = new ChatMessage
            {
                role = "system",
                content = personality + context
            };
        }

        // =========================
        // MEMORIA NPC
        // =========================

        public void Remember(string key, string value)
        {
            if (memory.ContainsKey(key))
            {
                memory[key] = value;
                return;
            }

            if (memory.Count >= maxMemories)
            {
                string firstKey = new List<string>(memory.Keys)[0];
                memory.Remove(firstKey);
            }

            memory[key] = value;
        }

        private void DetectMemory(string text)
        {
            text = text.ToLower();

            if (text.Contains("me llamo"))
            {
                var parts = text.Split("me llamo");

                if (parts.Length > 1)
                {
                    string name = parts[1].Trim();

                    if (!string.IsNullOrEmpty(name))
                        Remember("Nombre del jugador", name);
                }
            }

            if (text.Contains("odio"))
            {
                var parts = text.Split("odio");

                if (parts.Length > 1)
                {
                    string thing = parts[1].Trim();
                    Remember("Odia", thing);
                }
            }

            if (text.Contains("me gusta"))
            {
                var parts = text.Split("me gusta");

                if (parts.Length > 1)
                {
                    string thing = parts[1].Trim();
                    Remember("Le gusta", thing);
                }
            }
        }

        private string BuildMemoryContext()
        {
            if (memory.Count == 0)
                return "";

            var sb = new System.Text.StringBuilder();

            sb.AppendLine("\nRecuerdos del NPC sobre el jugador:");

            foreach (var kv in memory)
                sb.AppendLine($"{kv.Key}: {kv.Value}");

            return sb.ToString();
        }
    }
}