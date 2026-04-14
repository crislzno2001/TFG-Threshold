using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using OpenAI;

namespace OpenAI.Dialogue
{
    public class NPCBrain : MonoBehaviour
    {
        [Header("Interacción")]
        public bool isInteracting = false;

        [Header("Perfil del personaje")]
        [SerializeField] private CharacterProfileSO characterProfile;

        [Header("Reglas globales")]
        [SerializeField] private DialogueGlobalRulesSO globalRules;

        [Header("OpenAI")]
        [SerializeField] private string model = "gpt-4o-mini";
        [Range(0f, 2f)]
        [SerializeField] private float temperature = 0.8f;
        [SerializeField] private int maxTokens = 150;

        [Header("Historial conversación")]
        [SerializeField] private int maxHistoryMessages = 6;

        [Header("Memoria NPC")]
        [SerializeField] private int maxMemories = 5;

        [Header("Grafo de diálogo")]
        public DialogueGraphSO dialogueGraph;

        private OpenAIApi openai;
        private readonly List<ChatMessage> history = new();
        private readonly Dictionary<string, string> memory = new();
        private readonly Dictionary<string, bool> progressionFlags = new();

        private DialogueNodeSO _currentNode;

        public string npcName =>
            characterProfile != null && !string.IsNullOrWhiteSpace(characterProfile.characterName)
                ? characterProfile.characterName
                : "NPC";

        private void Awake()
        {
            openai = new OpenAIApi();
            ResetHistory();
        }

        public void ResetHistory()
        {
            history.Clear();
            history.Add(new ChatMessage
            {
                role = "system",
                content = BuildSystemPromptForNode(null)
            });
        }

        public void ClearProgressionFlags()
        {
            progressionFlags.Clear();
        }

        public void SetFlag(string flag, bool value = true)
        {
            if (string.IsNullOrWhiteSpace(flag)) return;
            progressionFlags[flag.Trim()] = value;
        }

        public bool GetFlag(string flag)
        {
            if (string.IsNullOrWhiteSpace(flag)) return false;
            return progressionFlags.TryGetValue(flag.Trim(), out bool value) && value;
        }

        public bool MeetsRequirements(DialogueNodeSO node)
        {
            if (node == null || node.prerequisiteFlags == null || node.prerequisiteFlags.Count == 0)
                return true;

            foreach (var requirement in node.prerequisiteFlags)
            {
                if (requirement == null || string.IsNullOrWhiteSpace(requirement.flag))
                    continue;

                bool currentValue = progressionFlags.TryGetValue(requirement.flag.Trim(), out bool stored) && stored;

                if (currentValue != requirement.expectedValue)
                    return false;
            }

            return true;
        }

        private void ApplyFlagsOnEnter(DialogueNodeSO node)
        {
            if (node?.flagsOnEnter == null) return;

            foreach (var change in node.flagsOnEnter)
            {
                if (change == null || string.IsNullOrWhiteSpace(change.flag))
                    continue;

                SetFlag(change.flag, change.value);
            }
        }

        public void SetNode(DialogueNodeSO node)
        {
            _currentNode = node;
            ApplyFlagsOnEnter(node);
            RebuildSystemPromptForNode(node);
        }

        public async System.Threading.Tasks.Task<DialogueStepResult> ProcessStep(string userMessage, DialogueNodeSO currentNode)
        {
            if (!isInteracting)
            {
                Debug.Log("[Brain] No hay interacción activa");
                return new DialogueStepResult
                {
                    NextNode = currentNode,
                    Reply = ""
                };
            }

            _currentNode = currentNode;
            DetectMemory(userMessage);

            DialogueNodeSO nextNode = await ResolveNextNodeAsync(userMessage, currentNode);
            if (nextNode == null)
                nextNode = currentNode;

            if (nextNode != currentNode && !MeetsRequirements(nextNode))
            {
                string blockedReply = !string.IsNullOrWhiteSpace(nextNode.lockedReply)
                    ? nextNode.lockedReply.Trim()
                    : "Aún no puedes avanzar por aquí.";

                blockedReply = SanitizeReply(blockedReply);

                Debug.Log($"[Dialogue] gate bloqueó acceso a '{nextNode.name}'");

                RebuildSystemPromptForNode(currentNode);

                history.Add(new ChatMessage
                {
                    role = "user",
                    content = userMessage
                });
                TrimHistory();

                history.Add(new ChatMessage
                {
                    role = "assistant",
                    content = blockedReply
                });
                TrimHistory();

                return new DialogueStepResult
                {
                    NextNode = currentNode,
                    Reply = blockedReply
                };
            }

            string reply = await GenerateReplyForNode(userMessage, nextNode);

            return new DialogueStepResult
            {
                NextNode = nextNode,
                Reply = reply
            };
        }

        private async System.Threading.Tasks.Task<DialogueNodeSO> ResolveNextNodeAsync(string userMessage, DialogueNodeSO currentNode)
        {
            if (currentNode == null)
                return null;

            if (currentNode is SpeechNodeSO speechNodeWithTransitions &&
                speechNodeWithTransitions.transitions != null &&
                speechNodeWithTransitions.transitions.Count > 0)
            {
                int idx = await EvaluateTransition(
                    userMessage,
                    speechNodeWithTransitions.transitions.ConvertAll(t => (t.condition, t.targetNode))
                );

                if (idx >= 0 &&
                    idx < speechNodeWithTransitions.transitions.Count &&
                    speechNodeWithTransitions.transitions[idx].targetNode != null)
                {
                    return speechNodeWithTransitions.transitions[idx].targetNode;
                }

                return currentNode;
            }

            if (currentNode is SpeechNodeSO speechNode &&
                speechNode.nextNodes != null &&
                speechNode.nextNodes.Count > 0)
            {
                DialogueNodeSO nextNode = speechNode.nextNodes[0];

                if (nextNode is ChoiceNodeSO nextChoice &&
                    nextChoice.choices != null &&
                    nextChoice.choices.Count > 0)
                {
                    int idx = await EvaluateTransition(
                        userMessage,
                        nextChoice.choices.ConvertAll(c => (c.condition, (DialogueNodeSO)c.nextNode))
                    );

                    if (idx >= 0 &&
                        idx < nextChoice.choices.Count &&
                        nextChoice.choices[idx].nextNode != null)
                    {
                        return nextChoice.choices[idx].nextNode;
                    }
                }

                return nextNode;
            }

            if (currentNode is ChoiceNodeSO choiceNode &&
                choiceNode.choices != null &&
                choiceNode.choices.Count > 0)
            {
                int idx = await EvaluateTransition(
                    userMessage,
                    choiceNode.choices.ConvertAll(c => (c.condition, (DialogueNodeSO)c.nextNode))
                );

                if (idx >= 0 &&
                    idx < choiceNode.choices.Count &&
                    choiceNode.choices[idx].nextNode != null)
                {
                    return choiceNode.choices[idx].nextNode;
                }

                return currentNode;
            }

            return currentNode;
        }

        private async System.Threading.Tasks.Task<string> GenerateReplyForNode(string userMessage, DialogueNodeSO nodeForReply)
        {
            RebuildSystemPromptForNode(nodeForReply);

            history.Add(new ChatMessage
            {
                role = "user",
                content = userMessage
            });

            TrimHistory();

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

            string reply = response.choices[0].message.content?.Trim();
            if (string.IsNullOrWhiteSpace(reply))
                reply = "...";

            reply = SanitizeReply(reply);

            history.Add(new ChatMessage
            {
                role = "assistant",
                content = reply
            });

            return reply;
        }

        private async System.Threading.Tasks.Task<int> EvaluateTransition(
            string userMessage,
            List<(string condition, DialogueNodeSO target)> options)
        {
            if (options == null || options.Count == 0)
                return -1;

            var sb = new StringBuilder();
            sb.AppendLine("Eres un clasificador semántico para un juego de rol.");
            sb.AppendLine("Tu tarea es clasificar la intención del jugador.");
            sb.AppendLine("Elige la opción que mejor describa la INTENCIÓN del mensaje.");
            sb.AppendLine("No compares palabras exactas, sino significado.");
            sb.AppendLine();

            if (_currentNode != null && !string.IsNullOrEmpty(_currentNode.contextForAI))
                sb.AppendLine($"Contexto actual del nodo: {_currentNode.contextForAI}");

            if (memory.Count > 0)
            {
                sb.AppendLine("Recuerdos del NPC sobre el jugador:");
                foreach (var kv in memory)
                    sb.AppendLine($"{kv.Key}: {kv.Value}");
            }

            if (progressionFlags.Count > 0)
            {
                sb.AppendLine("Flags narrativas actuales:");
                foreach (var kv in progressionFlags)
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
                    new ChatMessage
                    {
                        role = "system",
                        content = "Eres un clasificador de intenciones. Responde SOLO con un número."
                    },
                    new ChatMessage
                    {
                        role = "user",
                        content = sb.ToString()
                    }
                },
                temperature = 0f,
                max_tokens = 5
            };

            var response = await openai.CreateChatCompletion(req);

            if (response?.choices == null || response.choices.Count == 0)
                return -1;

            string raw = response.choices[0].message.content?.Trim() ?? "";
            raw = Regex.Match(raw, @"-?\d+").Value;

            int result = int.TryParse(raw, out int parsed) ? parsed : -1;

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

        private void RebuildSystemPromptForNode(DialogueNodeSO node)
        {
            if (history.Count == 0)
                ResetHistory();

            history[0] = new ChatMessage
            {
                role = "system",
                content = BuildSystemPromptForNode(node)
            };
        }

        private string BuildSystemPromptForNode(DialogueNodeSO node)
        {
            string nodeContext = node != null ? node.contextForAI : "";
            string memoryContext = BuildMemoryContext();
            string globalRulesBlock = globalRules != null ? globalRules.BuildGlobalRulesBlock() : "";
            string flagsContext = BuildFlagsContext();

            if (characterProfile != null)
            {
                string prompt = characterProfile.BuildCharacterPrompt(globalRulesBlock, nodeContext, memoryContext);

                if (!string.IsNullOrWhiteSpace(flagsContext))
                    prompt += "\n\n" + flagsContext;

                return prompt;
            }

            var sb = new StringBuilder();
            sb.AppendLine("Eres un NPC de un videojuego.");

            if (!string.IsNullOrWhiteSpace(globalRulesBlock))
            {
                sb.AppendLine();
                sb.AppendLine(globalRulesBlock);
            }

            if (!string.IsNullOrWhiteSpace(nodeContext))
            {
                sb.AppendLine();
                sb.AppendLine("Contexto actual de la historia:");
                sb.AppendLine(nodeContext);
            }

            if (!string.IsNullOrWhiteSpace(memoryContext))
            {
                sb.AppendLine();
                sb.AppendLine(memoryContext);
            }

            if (!string.IsNullOrWhiteSpace(flagsContext))
            {
                sb.AppendLine();
                sb.AppendLine(flagsContext);
            }

            return sb.ToString();
        }

        private string SanitizeReply(string reply)
        {
            if (globalRules != null && globalRules.forbiddenPhrases != null)
            {
                foreach (string forbidden in globalRules.forbiddenPhrases)
                {
                    if (string.IsNullOrWhiteSpace(forbidden))
                        continue;

                    reply = Regex.Replace(
                        reply,
                        Regex.Escape(forbidden),
                        "",
                        RegexOptions.IgnoreCase
                    ).Trim();
                }
            }

            if (characterProfile != null && characterProfile.maxWords > 0)
                reply = ClampWords(reply, characterProfile.maxWords);

            return string.IsNullOrWhiteSpace(reply) ? "..." : reply;
        }

        private string ClampWords(string text, int maxWords)
        {
            string[] words = Regex.Split(text.Trim(), @"\s+");
            if (words.Length <= maxWords)
                return text;

            return string.Join(" ", words, 0, maxWords).Trim() + "...";
        }

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
                    if (!string.IsNullOrEmpty(thing))
                        Remember("Odia", thing);
                }
            }

            if (text.Contains("me gusta"))
            {
                var parts = text.Split("me gusta");
                if (parts.Length > 1)
                {
                    string thing = parts[1].Trim();
                    if (!string.IsNullOrEmpty(thing))
                        Remember("Le gusta", thing);
                }
            }
        }

        private string BuildMemoryContext()
        {
            if (memory.Count == 0)
                return "";

            var sb = new StringBuilder();
            sb.AppendLine("Recuerdos del NPC sobre el jugador:");

            foreach (var kv in memory)
                sb.AppendLine($"{kv.Key}: {kv.Value}");

            return sb.ToString();
        }

        private string BuildFlagsContext()
        {
            if (progressionFlags.Count == 0)
                return "";

            var sb = new StringBuilder();
            sb.AppendLine("Estado narrativo actual:");

            foreach (var kv in progressionFlags)
                sb.AppendLine($"- {kv.Key} = {kv.Value}");

            return sb.ToString();
        }
    }
}