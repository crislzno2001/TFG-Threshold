using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using OpenAI;
using OpenAI.Dialogue;
using Sprout.Data;

namespace Sprout.Presentation
{
    /// <summary>
    /// Brings an NPC to life during dialogue: plays the "talking" wobble while the
    /// NPC is thinking/replying, and reacts emotionally (happy / offended / sad)
    /// based on a tiny AI classification of each reply. Only reacts when THIS npc is
    /// the one being talked to. Drives ProceduralNpcAnimator.
    /// </summary>
    public class NpcEmotionAnimator : MonoBehaviour
    {
        [SerializeField] private ProceduralNpcAnimator animator;
        [SerializeField] private NPCBrain brain;
        [SerializeField] private DialogueUI dialogueUI;
        [SerializeField] private AISettingsSO aiSettings;

        private OpenAIApi _openai;
        private bool _busy;

        private void Awake()
        {
            _openai = new OpenAIApi();
            if (animator == null) animator = GetComponent<ProceduralNpcAnimator>();
            if (brain == null) brain = GetComponent<NPCBrain>();
        }

        private void Start()
        {
            if (dialogueUI == null) return;
            dialogueUI.onPlayerMessageSent.AddListener(_ => OnThinking());
            dialogueUI.onNPCReplied.AddListener(OnReplied);
        }

        private bool IsActive => brain != null && brain.isInteracting && animator != null;

        private void OnThinking()
        {
            if (IsActive) animator.PlayTalking();
        }

        private async void OnReplied(string reply)
        {
            if (!IsActive) return;
            if (_busy || string.IsNullOrWhiteSpace(reply)) { animator.PlayIdle(); return; }

            _busy = true;
            string mood = await Classify(reply);
            _busy = false;

            if (!IsActive) return;
            switch (mood)
            {
                case "feliz":    animator.PlayHappy(); break;
                case "ofendido": animator.PlayOffended(); break;
                case "triste":   animator.PlaySad(); break;
                default:         animator.PlayIdle(); break;
            }
        }

        private async Task<string> Classify(string text)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Clasifica la emoción del personaje en esta frase.");
            sb.AppendLine("Responde SOLO una palabra: feliz, ofendido, triste o neutral.");
            sb.AppendLine($"Frase: \"{text}\"");

            string model = aiSettings != null ? aiSettings.classifierModel : "gpt-4o-mini";
            var req = new CreateChatCompletionRequest
            {
                model = model,
                temperature = 0f,
                max_tokens = 4,
                messages = new List<ChatMessage>
                {
                    new ChatMessage { role = "system", content = "Eres un clasificador de emoción. Una sola palabra." },
                    new ChatMessage { role = "user", content = sb.ToString() }
                }
            };

            try
            {
                var r = await _openai.CreateChatCompletion(req);
                string raw = r?.choices != null && r.choices.Count > 0
                    ? r.choices[0].message.content?.Trim().ToLowerInvariant() ?? "" : "";
                if (raw.Contains("feliz") || raw.Contains("aleg")) return "feliz";
                if (raw.Contains("ofend") || raw.Contains("enfad")) return "ofendido";
                if (raw.Contains("trist")) return "triste";
                return "neutral";
            }
            catch { return "neutral"; }
        }
    }
}
