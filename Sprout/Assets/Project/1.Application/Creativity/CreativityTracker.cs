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
    /// Medidor de creatividad (estilo Torrance ampliado) para cualquier NPC. Puntúa CADA mensaje
    /// libre del jugador durante la conversación en 7 dimensiones: originalidad, detalle, coherencia,
    /// empatía, uso del mundo, riesgo y adaptación (mejorar una idea tras una objeción). Los mensajes
    /// enviados en un nodo de "reto creativo" pesan más (challengeWeight). Todo es invisible: se
    /// acumula en el CreativityProfile del director y dispara contadores, flags y flores.
    ///
    /// Se auto-suscribe a DialogueUI.onPlayerMessageSent y DialogueRunner.onStepCompleted.
    /// </summary>
    public class CreativityTracker : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private NpcId npc = NpcId.Mochi;
        [SerializeField] private NPCBrain brain;

        [Header("Fuentes (auto-suscripción)")]
        [SerializeField] private DialogueUI dialogueUI;
        [SerializeField] private DialogueRunner runner;

        [Header("Reto creativo")]
        [Tooltip("Nodo principal de ideas/reto. Los mensajes aquí pesan 'challengeWeight'.")]
        [SerializeField] private DialogueNodeSO ideasNode;
        [Tooltip("Nodos EXTRA de reto (p. ej. la revisión de noche). También pesan más.")]
        [SerializeField] private List<DialogueNodeSO> extraChallengeNodes = new();
        [Tooltip("Si está activo, puntúa en TODA conversación. Si no, solo en los nodos de reto.")]
        [SerializeField] private bool scoreEverywhere = true;
        [SerializeField] private float normalWeight = 1f;
        [SerializeField] private float challengeWeight = 2f;

        [Header("Contador que incrementa este tracker")]
        [Tooltip("e.g. mochi_ideas_count, aster_ideas_count, moth_friendship")]
        [SerializeField] private string counterKey = NarrativeFlagKeys.MochiIdeasCount;

        [Header("Dominio de idea válida (para el prompt)")]
        [TextArea(2, 5)]
        [SerializeField] private string ideaDomain =
            "a concrete culinary idea: combining ingredients, a cooking technique, " +
            "a dish, or a way to present food.";

        [Header("Config")]
        [SerializeField] private AISettingsSO aiSettings;
        [SerializeField] private int highFluencyThreshold = 4;

        [Header("Generación de flores (opcional)")]
        [SerializeField] private FlowerService flowerService;

        [Header("Eventos")]
        public UnityEvent<int> onIdeaCountChanged;

        private OpenAIApi _openai;
        private DialogueNodeSO _lastNode;   // fallback si runner.Current no está disponible
        private string _previousMessage = ""; // para medir ADAPTACIÓN (mejora sobre la idea anterior)
        private bool _busy;

        private void Awake() => _openai = new OpenAIApi();

        private void Start()
        {
            if (runner == null) runner = GetComponent<DialogueRunner>();
            if (dialogueUI != null) dialogueUI.onPlayerMessageSent.AddListener(RegisterPlayerMessage);
            if (runner != null) runner.onStepCompleted.AddListener(OnDialogueStepCompleted);
        }

        /// <summary>Sigue el nodo actual (por si runner.Current no estuviera disponible).</summary>
        public void OnDialogueStepCompleted(DialogueNodeSO currentNode)
        {
            if (currentNode != null) _lastNode = currentNode;
        }

        /// <summary>Cada mensaje libre del jugador se evalúa aquí (lo llama DialogueUI.onPlayerMessageSent).</summary>
        public async void RegisterPlayerMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message) || _busy) return;

            DialogueNodeSO node = CurrentNode();
            bool isChallenge = IsChallenge(node);
            if (!scoreEverywhere && !isChallenge) return;   // modo "solo retos"

            float weight = isChallenge ? challengeWeight : normalWeight;

            _busy = true;
            var eval = await Evaluate(message, _previousMessage, node);
            _busy = false;

            _previousMessage = message;

            var director = SproutGameDirector.Instance;
            var profile = director != null ? director.CreativityFor(npc) : null;
            profile?.AddEvaluation(eval.isIdea, eval.originality, eval.detail, eval.coherence,
                                   eval.empathy, eval.worldUse, eval.risk, eval.adaptation,
                                   eval.category, weight);

            // Fluidez: solo las ideas CONCRETAS cuentan para el contador.
            if (eval.isIdea && director != null)
            {
                int count = director.Flags.IncrementCounter(counterKey);
                if (brain != null) brain.SetFlag($"{counterKey}_value_{count}", true);
                if (count >= highFluencyThreshold && brain != null)
                    brain.SetFlag($"{npc.ToString().ToLowerInvariant()}_fluency_high", true);

                if (flowerService != null)
                {
                    if (profile != null && profile.HighOriginality()) flowerService.OnHighOriginality();
                    if (count >= highFluencyThreshold) flowerService.OnHighFluency();
                }
                onIdeaCountChanged?.Invoke(count);
            }

            Debug.Log($"[Creativity:{npc}] w{weight:0.#} idea={eval.isIdea} " +
                      $"orig{eval.originality:0.0} det{eval.detail:0.0} coh{eval.coherence:0.0} " +
                      $"emp{eval.empathy:0.0} world{eval.worldUse:0.0} risk{eval.risk:0.0} adapt{eval.adaptation:0.0}");
        }

        // ── Ayudas ────────────────────────────────────────────────────────────

        private DialogueNodeSO CurrentNode()
        {
            if (runner != null && runner.Current != null) return runner.Current;
            return _lastNode;
        }

        private bool IsChallenge(DialogueNodeSO node)
            => node != null && (node == ideasNode || extraChallengeNodes.Contains(node));

        // ── Evaluación por IA ────────────────────────────────────────────────

        private struct Eval
        {
            public bool isIdea;
            public float originality, detail, coherence, empathy, worldUse, risk, adaptation;
            public string category;
        }

        private async Task<Eval> Evaluate(string message, string previous, DialogueNodeSO node)
        {
            var def = new Eval { category = "" };
            if (string.IsNullOrWhiteSpace(message)) return def;

            string situation = node != null && !string.IsNullOrWhiteSpace(node.contextForAI)
                ? node.contextForAI : "casual conversation with a neighbour.";

            var sb = new StringBuilder();
            sb.AppendLine("You invisibly score a player's message in a cozy narrative game, for a");
            sb.AppendLine("Torrance-style creativity profile. Be strict: use the FULL 0-10 range.");
            sb.AppendLine($"Character/situation right now: {situation}");
            sb.AppendLine($"What counts as a concrete idea for this character: {ideaDomain}");
            sb.AppendLine("World elements the player could weave in: flowers, bouquets, cooking/food, the");
            sb.AppendLine("village, the neighbours, the day/night cycle, rumours, and emotions.");
            sb.AppendLine($"Previous player idea (only for ADAPTATION): \"{(string.IsNullOrWhiteSpace(previous) ? "(none)" : previous)}\"");
            sb.AppendLine();
            sb.AppendLine("Score the CURRENT message 0-10 on each dimension:");
            sb.AppendLine("ORIGINALITY = rare/unexpected. DETAIL = concrete/specific. COHERENCE = fits the");
            sb.AppendLine("problem and world. EMPATHY = considers the character's feelings. WORLDUSE = uses");
            sb.AppendLine("world elements. RISK = dares something odd but sensible. ADAPTATION = improves or");
            sb.AppendLine("revises the previous idea after pushback (0 if unrelated or no previous idea).");
            sb.AppendLine("IDEA=yes only if it's a concrete idea/proposal (no for greetings/questions/filler).");
            sb.AppendLine("CATEGORY = one short word for the kind of idea.");
            sb.AppendLine("Reply in EXACTLY this pipe format, nothing else:");
            sb.AppendLine("IDEA=yes|no ; ORIGINALITY=0-10 ; DETAIL=0-10 ; COHERENCE=0-10 ; EMPATHY=0-10 ; WORLDUSE=0-10 ; RISK=0-10 ; ADAPTATION=0-10 ; CATEGORY=word");
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
                max_tokens = 70
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

        private static Eval Parse(string raw)
        {
            var e = new Eval { category = "" };
            if (string.IsNullOrWhiteSpace(raw)) return e;
            string lower = raw.ToLowerInvariant();

            e.isIdea = Regex.IsMatch(lower, @"idea\s*=\s*yes");
            e.originality = Score(lower, "originality");
            e.detail      = Score(lower, "detail");
            e.coherence   = Score(lower, "coherence");
            e.empathy     = Score(lower, "empathy");
            e.worldUse    = Score(lower, "worlduse");
            e.risk        = Score(lower, "risk");
            e.adaptation  = Score(lower, "adaptation");

            var cat = Regex.Match(lower, @"category\s*=\s*([a-z_\- ]+)");
            if (cat.Success) e.category = cat.Groups[1].Value.Trim();
            return e;
        }

        /// <summary>Extrae una dimensión 0-10 y la normaliza a [0,1].</summary>
        private static float Score(string lower, string key)
        {
            var m = Regex.Match(lower, key + @"\s*=\s*(\d+(\.\d+)?)");
            if (m.Success && float.TryParse(m.Groups[1].Value,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float v))
                return Mathf.Clamp01(v / 10f);
            return 0f;
        }
    }
}
