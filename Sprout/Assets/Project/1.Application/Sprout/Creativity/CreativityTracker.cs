using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using OpenAI;
using OpenAI.Dialogue;
using Sprout.Data;
using Sprout.Domain.Narrative;

namespace Sprout.Application.Creativity
{
    /// <summary>
    /// Generalised Torrance-style creativity tracker for any NPC (Mochi, Aster,
    /// Moth, Rix). When the conversation sits in a designated "ideas" node, each
    /// player message is scored by AI for: is-it-an-idea (fluency), originality,
    /// elaboration, and category (flexibility). Scores are stored invisibly in the
    /// director's CreativityProfile and drive counters, flags and flower generation.
    ///
    /// Wire:  DialogueUI.onPlayerMessageSent → RegisterPlayerMessage
    ///        DialogueRunner.onStepCompleted → OnDialogueStepCompleted
    /// </summary>
    public class CreativityTracker : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private NpcId npc = NpcId.Mochi;
        [SerializeField] private NPCBrain brain;

        [Header("Auto-subscribe sources (optional)")]
        [Tooltip("If set, this tracker subscribes itself to these at Start, so no " +
                 "manual UnityEvent wiring is needed.")]
        [SerializeField] private DialogueUI dialogueUI;
        [SerializeField] private DialogueRunner runner;

        [Header("Ideas node")]
        [Tooltip("The brainstorm node. Messages sent while parked here are scored.")]
        [SerializeField] private DialogueNodeSO ideasNode;

        [Header("Counter flag this tracker increments")]
        [Tooltip("e.g. mochi_ideas_count, aster_ideas_count, moth_friendship")]
        [SerializeField] private string counterKey = NarrativeFlagKeys.MochiIdeasCount;

        [Header("Domain of valid ideas (for the classifier prompt)")]
        [TextArea(2, 5)]
        [SerializeField] private string ideaDomain =
            "a concrete culinary idea: combining ingredients, a cooking technique, " +
            "a dish, or a way to present food.";

        [Header("Config")]
        [SerializeField] private AISettingsSO aiSettings;
        [SerializeField] private int highFluencyThreshold = 4;

        [Header("Optional flower generation")]
        [SerializeField] private FlowerService flowerService;

        [Header("Events")]
        public UnityEvent<int> onIdeaCountChanged;

        private OpenAIApi _openai;
        private DialogueNodeSO _previousNode;
        private string _lastMessage = "";
        private bool _busy;

        private void Awake() => _openai = new OpenAIApi();

        private void Start()
        {
            if (runner == null) runner = GetComponent<DialogueRunner>();
            if (dialogueUI != null) dialogueUI.onPlayerMessageSent.AddListener(RegisterPlayerMessage);
            if (runner != null) runner.onStepCompleted.AddListener(OnDialogueStepCompleted);
        }

        public void RegisterPlayerMessage(string message) => _lastMessage = message ?? "";

        public async void OnDialogueStepCompleted(DialogueNodeSO currentNode)
        {
            if (ideasNode == null || currentNode == null || _busy) return;

            bool wasInIdeas = _previousNode == ideasNode;
            bool stillInIdeas = currentNode == ideasNode;
            _previousNode = currentNode;

            if (!wasInIdeas || !stillInIdeas) return;
            if (string.IsNullOrWhiteSpace(_lastMessage)) return;

            string msg = _lastMessage;
            _busy = true;
            var eval = await EvaluateIdea(msg);
            _busy = false;

            if (!eval.isIdea) return;

            var director = SproutGameDirector.Instance;
            var profile = director != null ? director.CreativityFor(npc) : null;
            profile?.AddIdea(eval.originality, eval.elaboration, eval.category);

            int count = director != null
                ? director.Flags.IncrementCounter(counterKey)
                : 0;

            if (brain != null) brain.SetFlag($"{counterKey}_value_{count}", true);
            if (count >= highFluencyThreshold && brain != null)
                brain.SetFlag($"{npc.ToString().ToLowerInvariant()}_fluency_high", true);

            // Flower generation hooks (rules in FlowerService).
            if (flowerService != null)
            {
                if (profile != null && profile.HighOriginality()) flowerService.OnHighOriginality();
                if (count >= highFluencyThreshold) flowerService.OnHighFluency();
            }

            onIdeaCountChanged?.Invoke(count);
            Debug.Log($"[Creativity:{npc}] idea #{count} — orig {eval.originality:0.0} elab {eval.elaboration:0.0} cat '{eval.category}'");
        }

        private struct IdeaEval { public bool isIdea; public float originality; public float elaboration; public string category; }

        private async Task<IdeaEval> EvaluateIdea(string message)
        {
            var def = new IdeaEval { isIdea = false, originality = 0, elaboration = 0, category = "" };
            if (string.IsNullOrWhiteSpace(message)) return def;

            var sb = new StringBuilder();
            sb.AppendLine("You score a player's message during a brainstorming moment in a game.");
            sb.AppendLine($"A valid idea is: {ideaDomain}");
            sb.AppendLine("Reply in EXACTLY this pipe format, nothing else:");
            sb.AppendLine("IDEA=yes|no ; ORIGINALITY=0-10 ; ELABORATION=0-10 ; CATEGORY=one_short_word");
            sb.AppendLine("IDEA=no means it is a greeting/question/comment, not a concrete idea (other fields 0).");
            sb.AppendLine("ORIGINALITY: how rare/unexpected. ELABORATION: how detailed/specific.");
            sb.AppendLine();
            sb.AppendLine($"Player message: \"{message}\"");

            string model = aiSettings != null ? aiSettings.classifierModel : "gpt-4o-mini";
            var req = new CreateChatCompletionRequest
            {
                model = model,
                messages = new List<ChatMessage>
                {
                    new ChatMessage { role = "system", content = "You are a precise evaluator. Output only the pipe format." },
                    new ChatMessage { role = "user", content = sb.ToString() }
                },
                temperature = 0f,
                max_tokens = 40
            };

            try
            {
                var resp = await _openai.CreateChatCompletion(req);
                string raw = resp?.choices != null && resp.choices.Count > 0
                    ? resp.choices[0].message.content ?? "" : "";
                return Parse(raw);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Creativity:{npc}] eval error: {e.Message}");
                return def;
            }
        }

        private static IdeaEval Parse(string raw)
        {
            var e = new IdeaEval { isIdea = false, originality = 0, elaboration = 0, category = "" };
            if (string.IsNullOrWhiteSpace(raw)) return e;
            string lower = raw.ToLowerInvariant();

            e.isIdea = Regex.IsMatch(lower, @"idea\s*=\s*yes");

            var orig = Regex.Match(lower, @"originality\s*=\s*(\d+(\.\d+)?)");
            var elab = Regex.Match(lower, @"elaboration\s*=\s*(\d+(\.\d+)?)");
            var cat = Regex.Match(lower, @"category\s*=\s*([a-z_\- ]+)");

            if (orig.Success && float.TryParse(orig.Groups[1].Value, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float o)) e.originality = Mathf.Clamp01(o / 10f);
            if (elab.Success && float.TryParse(elab.Groups[1].Value, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float el)) e.elaboration = Mathf.Clamp01(el / 10f);
            if (cat.Success) e.category = cat.Groups[1].Value.Trim();
            return e;
        }
    }
}
