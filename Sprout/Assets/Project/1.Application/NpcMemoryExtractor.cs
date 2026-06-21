using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using OpenAI;
using OpenAI.Dialogue;
using Sprout.Data;

namespace Sprout.Application
{
    /// <summary>
    /// Gives an NPC real memory: after each player message (only while THIS npc is
    /// the one talking), it asks the AI to extract one durable fact about the player
    /// and stores it via NPCBrain.Remember, so it feeds future system prompts.
    /// Wire: assign brain + dialogueUI; it self-subscribes in Start.
    /// </summary>
    public class NpcMemoryExtractor : MonoBehaviour
    {
        [SerializeField] private NPCBrain brain;
        [SerializeField] private DialogueUI dialogueUI;
        [SerializeField] private AISettingsSO aiSettings;
        [Tooltip("Skip extraction for very short messages.")]
        [SerializeField] private int minChars = 4;

        private OpenAIApi _openai;
        private bool _busy;

        private void Awake() => _openai = new OpenAIApi();

        private void Start()
        {
            if (brain == null) brain = GetComponent<NPCBrain>();
            if (dialogueUI != null) dialogueUI.onPlayerMessageSent.AddListener(OnPlayerMessage);
        }

        private async void OnPlayerMessage(string message)
        {
            if (brain == null || !brain.isInteracting) return;          // only the active NPC
            if (_busy || string.IsNullOrWhiteSpace(message)) return;
            if (message.Trim().Length < minChars) return;

            _busy = true;
            var fact = await Extract(message);
            _busy = false;

            if (fact.HasValue)
            {
                brain.Remember(fact.Value.key, fact.Value.value);
                Debug.Log($"[Memory:{brain.npcName}] remembered {fact.Value.key} = {fact.Value.value}");
            }
        }

        private async Task<(string key, string value)?> Extract(string message)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Extract ONE durable fact about the player from their message");
            sb.AppendLine("(their name, a preference, a relationship, something personal they revealed).");
            sb.AppendLine("Reply EXACTLY as 'key: value' (e.g. 'name: Laura' or 'likes: tulips').");
            sb.AppendLine("If there is no durable fact, reply exactly: NONE");
            sb.AppendLine();
            sb.AppendLine($"Player message: \"{message}\"");

            string model = aiSettings != null ? aiSettings.classifierModel : "gpt-4o-mini";
            var req = new CreateChatCompletionRequest
            {
                model = model,
                messages = new List<ChatMessage>
                {
                    new ChatMessage { role = "system", content = "You extract one concise memory or reply NONE." },
                    new ChatMessage { role = "user", content = sb.ToString() }
                },
                temperature = 0f,
                max_tokens = 20
            };

            try
            {
                var resp = await _openai.CreateChatCompletion(req);
                string raw = resp?.choices != null && resp.choices.Count > 0
                    ? resp.choices[0].message.content?.Trim() ?? "" : "";
                if (string.IsNullOrWhiteSpace(raw) || raw.ToUpperInvariant().StartsWith("NONE")) return null;
                int colon = raw.IndexOf(':');
                if (colon <= 0) return null;
                string key = raw.Substring(0, colon).Trim();
                string value = raw.Substring(colon + 1).Trim();
                if (key.Length == 0 || value.Length == 0) return null;
                return (key, value);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Memory] extract error: {e.Message}");
                return null;
            }
        }
    }
}
