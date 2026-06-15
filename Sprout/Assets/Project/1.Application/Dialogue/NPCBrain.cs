using System;
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

        private string cachedGlobalRulesBlock = "";

        public string npcName =>
            characterProfile != null && !string.IsNullOrWhiteSpace(characterProfile.characterName)
                ? characterProfile.characterName
                : "NPC";

        private void Awake()
        {
            openai = new OpenAIApi();
            RefreshStaticPromptCache();
            ResetHistory();
        }

        private void RefreshStaticPromptCache()
        {
            cachedGlobalRulesBlock = globalRules != null
                ? globalRules.BuildGlobalRulesBlock()
                : "";
        }

        public void ResetHistory()
        {
            RefreshStaticPromptCache();

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

        /// <summary>Raised when a flag actually changes value (drives the central store).</summary>
        public event System.Action<string, bool> OnFlagSet;

        public void SetFlag(string flag, bool value = true)
        {
            if (string.IsNullOrWhiteSpace(flag)) return;
            flag = flag.Trim();
            bool had = progressionFlags.TryGetValue(flag, out bool old);
            progressionFlags[flag] = value;
            if (!had || old != value) OnFlagSet?.Invoke(flag, value);
        }

        public bool GetFlag(string flag)
        {
            if (string.IsNullOrWhiteSpace(flag)) return false;
            return progressionFlags.TryGetValue(flag.Trim(), out bool value) && value;
        }

        // ── Memory persistence (save/load) ───────────────────────────────────
        public IReadOnlyDictionary<string, string> ExportMemory() => memory;

        public void ImportMemory(Dictionary<string, string> saved)
        {
            memory.Clear();
            if (saved == null) return;
            foreach (var kv in saved) memory[kv.Key] = kv.Value;
        }

        public bool MeetsRequirements(DialogueNodeSO node)
        {
            if (node == null || node.prerequisiteFlags == null || node.prerequisiteFlags.Count == 0)
                return true;

            foreach (var requirement in node.prerequisiteFlags)
            {
                if (requirement == null || string.IsNullOrWhiteSpace(requirement.flag))
                    continue;

                bool currentValue =
                    progressionFlags.TryGetValue(requirement.flag.Trim(), out bool stored) && stored;

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

        public async System.Threading.Tasks.Task<DialogueStepResult> ProcessStep(
            string userMessage,
            DialogueNodeSO currentNode)
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

            DialogueNodeSO nextNode;

            if (!TryResolveFastNextNode(currentNode, out nextNode))
            {
                nextNode = await ResolveNextNodeAsync(userMessage, currentNode);
            }

            if (nextNode == null)
                nextNode = currentNode;

            if (nextNode != currentNode && !MeetsRequirements(nextNode))
            {
                string blockedReply = !string.IsNullOrWhiteSpace(nextNode.lockedReply)
                    ? nextNode.lockedReply.Trim()
                    : "Aún no puedes avanzar por aquí.";

                blockedReply = SanitizeReply(blockedReply);

                Debug.Log($"[Dialogue] gate bloqueó acceso a '{nextNode.name}'");

                AddTurnToHistory(userMessage, blockedReply, currentNode);

                return new DialogueStepResult
                {
                    NextNode = currentNode,
                    Reply = blockedReply
                };
            }

            if (nextNode != currentNode)
            {
                ApplyFlagsOnEnter(nextNode);
            }

            string reply = await GenerateReplyForNode(userMessage, nextNode);

            return new DialogueStepResult
            {
                NextNode = nextNode,
                Reply = reply
            };
        }

        private bool TryResolveFastNextNode(DialogueNodeSO currentNode, out DialogueNodeSO nextNode)
        {
            nextNode = null;

            if (currentNode == null)
                return false;

            // Los ConversationNode siempre evalúan exitCondition async — nunca fast-path.
            if (currentNode is ConversationNodeSO)
                return false;

            if (currentNode is SpeechNodeSO speechNode)
            {
                if (speechNode.transitions != null && speechNode.transitions.Count == 1)
                {
                    var transition = speechNode.transitions[0];

                    if (transition.targetNode != null && IsAutomaticCondition(transition.condition))
                    {
                        nextNode = transition.targetNode;
                        return true;
                    }
                }

                if (speechNode.transitions != null && speechNode.transitions.Count > 0)
                    return false;

                if (speechNode.nextNodes == null || speechNode.nextNodes.Count == 0)
                    return false;

                DialogueNodeSO candidate = speechNode.nextNodes[0];

                if (candidate is ChoiceNodeSO nextChoice &&
                    nextChoice.choices != null &&
                    nextChoice.choices.Count > 0)
                {
                    if (nextChoice.choices.Count == 1 &&
                        nextChoice.choices[0].nextNode != null &&
                        IsAutomaticCondition(nextChoice.choices[0].condition))
                    {
                        nextNode = nextChoice.choices[0].nextNode;
                        return true;
                    }

                    return false;
                }

                if (candidate != null)
                {
                    nextNode = candidate;
                    return true;
                }
            }

            if (currentNode is ChoiceNodeSO choiceNode &&
                choiceNode.choices != null &&
                choiceNode.choices.Count == 1)
            {
                var choice = choiceNode.choices[0];

                if (choice.nextNode != null && IsAutomaticCondition(choice.condition))
                {
                    nextNode = choice.nextNode;
                    return true;
                }
            }

            return false;
        }

        private bool IsAutomaticCondition(string condition)
        {
            if (string.IsNullOrWhiteSpace(condition))
                return true;

            string normalized = condition.Trim().ToLowerInvariant();

            return normalized == "default" ||
                   normalized == "always" ||
                   normalized == "continue" ||
                   normalized == "continuar" ||
                   normalized == "any" ||
                   normalized == "cualquier respuesta";
        }

        private async System.Threading.Tasks.Task<DialogueNodeSO> ResolveNextNodeAsync(
            string userMessage,
            DialogueNodeSO currentNode)
        {
            if (currentNode == null)
                return null;

            // ── ConversationNode: evaluar exitCondition. Si se cumple → avanza. ─
            if (currentNode is ConversationNodeSO conversationNode)
            {
                bool shouldExit = await EvaluateExitCondition(
                    userMessage,
                    conversationNode.exitCondition,
                    conversationNode
                );

                if (shouldExit &&
                    conversationNode.nextNodes != null &&
                    conversationNode.nextNodes.Count > 0)
                {
                    return conversationNode.nextNodes[0];
                }

                return currentNode;
            }

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

                    return currentNode;
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

        private async System.Threading.Tasks.Task<string> GenerateReplyForNode(
            string userMessage,
            DialogueNodeSO nodeForReply)
        {
            RebuildSystemPromptForNode(nodeForReply);

            history.Add(new ChatMessage
            {
                role = "user",
                content = userMessage ?? ""
            });

            TrimHistory();

            var req = new CreateChatCompletionRequest
            {
                model = model,
                messages = new List<ChatMessage>(history),
                temperature = temperature,
                max_tokens = maxTokens
            };

            string reply = "...";

            try
            {
                var response = await openai.CreateChatCompletion(req);

                if (response?.choices != null && response.choices.Count > 0)
                {
                    reply = response.choices[0].message.content?.Trim();

                    if (string.IsNullOrWhiteSpace(reply))
                        reply = "...";
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OpenAI] Error generando respuesta: {ex.Message}");
            }

            reply = SanitizeReply(reply);

            history.Add(new ChatMessage
            {
                role = "assistant",
                content = reply
            });

            TrimHistory();

            return reply;
        }

        private async System.Threading.Tasks.Task<int> EvaluateTransition(
            string userMessage,
            List<(string condition, DialogueNodeSO target)> options)
        {
            if (options == null || options.Count == 0)
                return -1;

            if (options.Count == 1 && IsAutomaticCondition(options[0].condition))
                return 0;

            var sb = new StringBuilder();

            sb.AppendLine("Clasifica la intención del jugador en una de las opciones.");
            sb.AppendLine("Responde SOLO con el número de la opción. Si ninguna encaja claramente, responde -1.");
            sb.AppendLine();

            if (_currentNode != null && !string.IsNullOrWhiteSpace(_currentNode.contextForAI))
            {
                sb.AppendLine("Contexto actual:");
                sb.AppendLine(_currentNode.contextForAI);
                sb.AppendLine();
            }

            sb.AppendLine("Opciones:");

            for (int i = 0; i < options.Count; i++)
            {
                string condition = string.IsNullOrWhiteSpace(options[i].condition)
                    ? "Continuación/default"
                    : options[i].condition.Trim();

                sb.AppendLine($"{i}: {condition}");
            }

            sb.AppendLine();
            sb.AppendLine($"Mensaje del jugador: \"{userMessage}\"");

            var req = new CreateChatCompletionRequest
            {
                model = model,
                messages = new List<ChatMessage>
                {
                    new ChatMessage
                    {
                        role = "system",
                        content = "Eres un clasificador de intención. Responde solo con un número entero."
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
            catch (Exception ex)
            {
                Debug.LogError($"[OpenAI] Error evaluando transición: {ex.Message}");
                return -1;
            }
        }

        private void AddTurnToHistory(
            string userMessage,
            string assistantReply,
            DialogueNodeSO nodeForPrompt)
        {
            RebuildSystemPromptForNode(nodeForPrompt);

            history.Add(new ChatMessage
            {
                role = "user",
                content = userMessage ?? ""
            });

            TrimHistory();

            history.Add(new ChatMessage
            {
                role = "assistant",
                content = assistantReply ?? "..."
            });

            TrimHistory();
        }

        private void TrimHistory()
        {
            if (history.Count == 0)
            {
                history.Add(new ChatMessage
                {
                    role = "system",
                    content = BuildSystemPromptForNode(_currentNode)
                });
            }

            while (history.Count > maxHistoryMessages + 1)
                history.RemoveAt(1);
        }

        private void RebuildSystemPromptForNode(DialogueNodeSO node)
        {
            if (history.Count == 0)
            {
                history.Add(new ChatMessage
                {
                    role = "system",
                    content = BuildSystemPromptForNode(node)
                });

                return;
            }

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
            string flagsContext = BuildFlagsContext();

            if (characterProfile != null)
            {
                string prompt = characterProfile.BuildCharacterPrompt(
                    cachedGlobalRulesBlock,
                    nodeContext,
                    memoryContext
                );

                if (!string.IsNullOrWhiteSpace(flagsContext))
                    prompt += "\n\n" + flagsContext;

                return prompt;
            }

            var sb = new StringBuilder();

            sb.AppendLine("Eres un NPC de un videojuego.");

            if (!string.IsNullOrWhiteSpace(cachedGlobalRulesBlock))
            {
                sb.AppendLine();
                sb.AppendLine(cachedGlobalRulesBlock);
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
            if (string.IsNullOrWhiteSpace(reply))
                return "...";

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
            if (string.IsNullOrWhiteSpace(text))
                return "...";

            string[] words = Regex.Split(text.Trim(), @"\s+");

            if (words.Length <= maxWords)
                return text;

            return string.Join(" ", words, 0, maxWords).Trim() + "...";
        }

        public void Remember(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
                return;

            key = key.Trim();
            value = value.Trim();

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
            if (string.IsNullOrWhiteSpace(text))
                return;

            string lowerText = text.ToLowerInvariant();

            if (lowerText.Contains("me llamo"))
            {
                var parts = lowerText.Split("me llamo");

                if (parts.Length > 1)
                {
                    string name = parts[1].Trim();

                    if (!string.IsNullOrEmpty(name))
                        Remember("Nombre del jugador", name);
                }
            }

            if (lowerText.Contains("odio"))
            {
                var parts = lowerText.Split("odio");

                if (parts.Length > 1)
                {
                    string thing = parts[1].Trim();

                    if (!string.IsNullOrEmpty(thing))
                        Remember("Odia", thing);
                }
            }

            if (lowerText.Contains("me gusta"))
            {
                var parts = lowerText.Split("me gusta");

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

        private async System.Threading.Tasks.Task<bool> EvaluateExitCondition(
            string userMessage,
            string exitCondition,
            ConversationNodeSO node)
        {
            if (string.IsNullOrWhiteSpace(exitCondition))
                return false;

            var sb = new StringBuilder();

            sb.AppendLine("Determina si la conversación debe avanzar.");
            sb.AppendLine("Responde SOLO 'si' o 'no'. Sin texto adicional.");
            sb.AppendLine();

            if (node != null && !string.IsNullOrWhiteSpace(node.contextForAI))
            {
                sb.AppendLine("Contexto del nodo actual:");
                sb.AppendLine(node.contextForAI);
                sb.AppendLine();
            }

            sb.AppendLine("Condición de salida (cuando se cumpla, la conversación avanza):");
            sb.AppendLine(exitCondition.Trim());
            sb.AppendLine();
            sb.AppendLine($"Último mensaje del jugador: \"{userMessage}\"");
            sb.AppendLine();
            sb.AppendLine("¿Se cumple la condición de salida? Responde 'si' o 'no'.");

            var req = new CreateChatCompletionRequest
            {
                model = model,
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
                var response = await openai.CreateChatCompletion(req);

                if (response?.choices == null || response.choices.Count == 0)
                    return false;

                string raw = response.choices[0].message.content?.Trim().ToLowerInvariant() ?? "";
                bool result = raw.StartsWith("si") || raw.StartsWith("sí") || raw.StartsWith("yes");

                Debug.Log($"[Dialogue] exitCondition → \"{userMessage}\" → {result}");
                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OpenAI] Error evaluando exitCondition: {ex.Message}");
                return false;
            }
        }

    }
}