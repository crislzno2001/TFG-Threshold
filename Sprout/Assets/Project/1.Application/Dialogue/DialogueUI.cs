using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using UnityEngine.InputSystem;
using ThresholdGame.Presentation.Interaction;
using Sprout.Application;
using Sprout.Domain.Flowers;
using Sprout.Domain.Narrative;

namespace OpenAI.Dialogue
{
    public class DialogueUI : MonoBehaviour
    {
        // ── Eventos para sistemas externos (Torrance, analytics, etc.) ─────────
        [Header("Eventos de conversación")]
        [Tooltip("Se dispara cuando el jugador envía un mensaje")]
        public UnityEvent<string> onPlayerMessageSent;

        [Tooltip("Se dispara cuando el NPC devuelve su respuesta")]
        public UnityEvent<string> onNPCReplied;

        [Header("Panel principal")]
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private TMP_Text npcNameText;
        [SerializeField] private TMP_Text chatDisplay;

        [Header("Input de texto")]
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private Button sendButton;

        [Header("Feedback")]
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button closeButton;

        [Header("Typewriter")]
        [Tooltip("Characters revealed per second.")]
        [SerializeField] private float charsPerSecond = 45f;
        [Tooltip("Pause after each finished phrase before the next one.")]
        [SerializeField] private float phrasePause = 0.7f;
        [Tooltip("Clear the box between phrases so text doesn't pile up.")]
        [SerializeField] private bool clearBetweenPhrases = true;

        [Header("Voice (typewriter blip, Animal-Crossing style)")]
        [SerializeField] private AudioSource voiceSource;
        [SerializeField] private AudioClip blipClip;
        [Tooltip("Play a blip every N revealed characters (1 = every letter).")]
        [SerializeField] private int blipEveryChars = 2;
        [SerializeField] private float blipVolume = 0.5f;

        [Header("Regalar ramo (durante el diálogo)")]
        [SerializeField] private FlowerService flowerService;
        private Button _giftButton;

        // Elecciones a mano (ChoiceNode): botones que sustituyen al campo de escribir.
        private readonly List<Button> _choiceButtons = new();
        private RectTransform _choiceArea;

        private NPCBrain currentNPC;
        private bool isWaiting = false;
        private DialogueRunner _runner;
        private Coroutine _typing;
        private DialogueNodeSO _lastNode; // último nodo cuya frase inicial ya mostramos
        private bool _closeAfterType;     // cerrar el diálogo al acabar de teclear (nodo de despedida)
        private float _baseChatSize, _baseNameSize; // tamaños base para aplicar el TextScale de la config

        private void Awake()
        {
            dialoguePanel.SetActive(false);

            if (sendButton != null) sendButton.onClick.AddListener(OnSendClicked);
            if (closeButton != null) closeButton.onClick.AddListener(OnCloseClicked);
            if (inputField != null) inputField.onSubmit.AddListener(_ => OnSendClicked());
            if (inputField != null) inputField.onValueChanged.AddListener(_ => LastInputChangeTime = Time.unscaledTime);

            if (flowerService == null) flowerService = FindFirstObjectByType<FlowerService>();

            if (chatDisplay != null) _baseChatSize = chatDisplay.fontSize;
            if (npcNameText != null) _baseNameSize = npcNameText.fontSize;

            SetStatus("");
            SetDialogueText("");
        }

        /// <summary>Aplica el tamaño de letra de la configuración al texto del diálogo.</summary>
        private void ApplyTextScale()
        {
            float s = Sprout.Presentation.SproutTextScale.Get();
            if (chatDisplay != null && _baseChatSize > 0f) chatDisplay.fontSize = _baseChatSize * s;
            if (npcNameText != null && _baseNameSize > 0f) npcNameText.fontSize = _baseNameSize * s;
        }

        /// <summary>The currently-open dialogue, if any (so Esc can close it).</summary>
        public static DialogueUI Active { get; private set; }

        /// <summary>The NPC currently being talked to (for the dialogue camera).</summary>
        public NPCBrain CurrentNpc => currentNPC;

        /// <summary>Closes the dialogue from outside (e.g. the Esc/pause handler).</summary>
        public void RequestClose() => OnCloseClicked();

        public void Open(NPCBrain npc)
        {
            currentNPC = npc;
            Active = this;
            dialoguePanel.SetActive(true);
            ApplyTextScale();
            ClearChoices();
            SetInputVisible(true);

            npcNameText.text = npc.npcName;
            SetDialogueText("");

            inputField.text = "";
            inputField.Select();
            inputField.ActivateInputField();

            SetStatus("");
            SetInputInteractable(true);

            EnsureGiftButton();
            if (_giftButton != null) _giftButton.gameObject.SetActive(true);

            _runner = npc.GetComponent<DialogueRunner>();
            if (_runner == null)
            {
                Debug.LogError($"[DialogueUI] El NPC '{npc.npcName}' no tiene componente DialogueRunner. Añádelo al mismo GameObject.", npc);
                return;
            }

            _runner.StartDialogue();

            _lastNode = _runner.Current;
            string opening = OpeningLineOf(_runner.Current);
            // Si retomamos una conversación a medias y el nodo tiene 'resumeLine', saluda de vuelta.
            if (_runner.Resumed && _runner.Current != null && !string.IsNullOrWhiteSpace(_runner.Current.resumeLine))
                opening = _runner.Current.resumeLine;
            if (!string.IsNullOrEmpty(opening))
                ShowNPCMessage(opening);
        }

        /// <summary>Frase inicial de un nodo (vale para Speech, Conversation y Choice).</summary>
        private static string OpeningLineOf(DialogueNodeSO node)
        {
            switch (node)
            {
                case SpeechNodeSO s:       return s.openingLine;
                case ConversationNodeSO c: return c.openingLine;
                case ChoiceNodeSO ch:      return ch.openingLine;
                case OptionsNodeSO o:      return o.openingLine;
                default:                   return null;
            }
        }

        /// <summary>True while the dialogue panel is visible.</summary>
        public bool IsOpen => dialoguePanel != null && dialoguePanel.activeSelf;

        /// <summary>True mientras el NPC "piensa" o está tecleando una respuesta (no se puede enviar otra).</summary>
        public bool IsBusy => isWaiting;

        /// <summary>Última vez (unscaled) que el jugador tocó el input. Para mover la boca al escribir.</summary>
        public float LastInputChangeTime { get; private set; }

        /// <summary>True si el jugador ha escrito algo en los últimos instantes (está "hablando").</summary>
        public bool IsTyping => IsOpen && (Time.unscaledTime - LastInputChangeTime) < 0.3f;

        public void Close()
        {
            if (_typing != null) { StopCoroutine(_typing); _typing = null; }
            if (_giftButton != null) _giftButton.gameObject.SetActive(false);
            ClearChoices();
            SetInputVisible(true);
            dialoguePanel.SetActive(false);
            if (Active == this) Active = null;
            currentNPC = null;
            _runner = null;
            isWaiting = false;
        }

        private async void OnSendClicked()
        {
            if (isWaiting || currentNPC == null || _runner == null) return;

            string userText = inputField.text.Trim();
            if (string.IsNullOrEmpty(userText)) return;

            inputField.text = "";

            try
            {
                await ProcessUserInput(userText);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                SetStatus("Ha ocurrido un error.");
                isWaiting = false;
                SetInputInteractable(true);
            }
        }

        private async System.Threading.Tasks.Task ProcessUserInput(string userText)
        {
            isWaiting = true;
            SetInputInteractable(false);

            SetStatus($"{currentNPC.npcName} está pensando...");

            // 🔔 Notificar a los listeners (Torrance tracker, etc.)
            onPlayerMessageSent?.Invoke(userText);

            string reply = await _runner.ProcessMessage(userText);

            SetStatus("");

            // Si hemos avanzado a un nodo NUEVO que tiene frase inicial, el NPC dice esa
            // frase guionizada (la del beat: confesión, pregunta de elección, etc.). Si
            // seguimos en el mismo nodo, mostramos la respuesta libre de la IA.
            DialogueNodeSO node = _runner.Current;
            string opening = OpeningLineOf(node);
            string displayReply;
            if (node != _lastNode && !string.IsNullOrEmpty(opening))
                displayReply = opening;
            else
                displayReply = string.IsNullOrWhiteSpace(reply) ? "..." : reply;
            _lastNode = node;
            _closeAfterType = IsTerminal(node) || IsGoodbye(userText); // despedida (nodo terminal o el jugador se despide) -> cerrar

            // 🔔 Notificar respuesta del NPC
            onNPCReplied?.Invoke(displayReply);

            // The typewriter coroutine re-enables input when it finishes.
            ShowNPCMessage(displayReply);
        }

        private void ShowNPCMessage(string text)
        {
            if (currentNPC == null) return;
            if (_typing != null) StopCoroutine(_typing);
            _typing = StartCoroutine(TypeRoutine(text));
        }

        /// <summary>
        /// Reveals the reply phrase-by-phrase with a typewriter effect, clearing the
        /// box between phrases so text never piles up in the same place.
        /// </summary>
        private IEnumerator TypeRoutine(string fullText)
        {
            isWaiting = true;
            SetInputInteractable(false);

            // El nombre del NPC ya se muestra arriba (npcNameText), así que aquí NO lo
            // repetimos: solo el texto de lo que dice.
            float delay = charsPerSecond > 0f ? 1f / charsPerSecond : 0f;

            int step = Mathf.Max(1, blipEveryChars);
            int idx = 0;

            foreach (string phrase in SplitPhrases(fullText))
            {
                var sb = new StringBuilder();
                if (!clearBetweenPhrases) { /* keep previous */ }

                foreach (char c in phrase)
                {
                    sb.Append(c);
                    SetDialogueText(sb.ToString());

                    if (!char.IsWhiteSpace(c) && idx % step == 0) PlayBlip();
                    idx++;

                    if (delay > 0f) yield return new WaitForSeconds(delay);
                }

                yield return new WaitForSeconds(phrasePause);
            }

            _typing = null;
            isWaiting = false;

            // Si hemos llegado a un nodo de despedida (terminal), cerramos solos tras una pausa.
            if (_closeAfterType)
            {
                _closeAfterType = false;
                yield return new WaitForSeconds(1.4f);
                RequestClose();
                yield break;
            }

            // ¿El nodo actual es de OPCIONES DEFINIDAS? -> botones a mano, no el campo de escribir.
            // (El ChoiceNode normal sigue con texto libre + IA, sin tocar.)
            if (_runner != null && _runner.Current is OptionsNodeSO optionsNode &&
                optionsNode.options != null && optionsNode.options.Count > 0)
            {
                ShowOptions(optionsNode);
            }
            else
            {
                SetInputVisible(true);
                SetInputInteractable(true);
                if (inputField != null) { inputField.Select(); inputField.ActivateInputField(); }
            }
        }

        /// <summary>True si el nodo no tiene salidas (es una despedida / final de rama).</summary>
        private static bool IsTerminal(DialogueNodeSO node)
        {
            if (node == null) return false;
            if (node.nextNodes != null && node.nextNodes.Count > 0) return false;
            if (node is ChoiceNodeSO c && c.choices != null && c.choices.Count > 0) return false;
            if (node is OptionsNodeSO o && o.options != null && o.options.Count > 0) return false;
            if (node is SpeechNodeSO s && s.transitions != null && s.transitions.Count > 0) return false;
            return true;
        }

        private static readonly string[] _byeWords =
        {
            "adios", "adiós", "hasta luego", "hasta pronto", "me voy", "me marcho", "me tengo que ir",
            "nos vemos", "chao", "chau", "bye", "hasta la próxima", "hasta la proxima",
            "hasta mañana", "hasta manana", "cuídate", "cuidate", "hasta otra"
        };

        /// <summary>True si el mensaje del jugador es una despedida clara, para poder salir siempre del diálogo.</summary>
        private static bool IsGoodbye(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            string t = text.ToLowerInvariant().Trim();
            foreach (var w in _byeWords) if (t.Contains(w)) return true;
            return false;
        }

        private void PlayBlip()
        {
            if (voiceSource == null || blipClip == null) return;
            // Each NPC gets a distinct base pitch; small per-letter wobble on top.
            float basePitch = 1f;
            if (currentNPC != null)
                basePitch = 0.85f + (Mathf.Abs(currentNPC.npcName.GetHashCode()) % 100) / 200f; // ~0.85..1.35
            voiceSource.pitch = basePitch * UnityEngine.Random.Range(0.96f, 1.07f);
            voiceSource.PlayOneShot(blipClip, blipVolume);
        }

        private static IEnumerable<string> SplitPhrases(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) { yield return "..."; yield break; }
            // Split after sentence-ending punctuation, keeping the punctuation.
            string[] parts = Regex.Split(text.Trim(), @"(?<=[\.\!\?…])\s+");
            foreach (string p in parts)
            {
                string t = p.Trim();
                if (t.Length > 0) yield return t;
            }
        }

        private void SetDialogueText(string text)
        {
            if (chatDisplay != null)
                chatDisplay.text = text;
        }

        private void SetStatus(string msg)
        {
            if (statusText != null)
                statusText.text = msg;
        }

        private void SetInputInteractable(bool value)
        {
            if (inputField != null) inputField.interactable = value;
            if (sendButton != null) sendButton.interactable = value;
            if (_giftButton != null) _giftButton.interactable = value;
        }

        // ── Regalar ramo durante el diálogo ─────────────────────────────────────

        private void EnsureGiftButton()
        {
            if (_giftButton != null || dialoguePanel == null) return;
            var panelRT = dialoguePanel.GetComponent<RectTransform>();
            if (panelRT == null) return;

            var go = new GameObject("GiftBouquetButton",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(panelRT, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-14f, -14f);
            rt.sizeDelta = new Vector2(150f, 42f);

            go.GetComponent<Image>().color = new Color(0.84f, 0.55f, 0.62f, 1f);
            _giftButton = go.GetComponent<Button>();
            _giftButton.onClick.AddListener(GiveActiveBouquet);

            var txtGo = new GameObject("Label", typeof(RectTransform));
            var trt = txtGo.GetComponent<RectTransform>();
            trt.SetParent(rt, false);
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
            var label = txtGo.AddComponent<TextMeshProUGUI>();
            label.text = "Dar ramo";
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 18;
            label.color = Color.white;
        }

        /// <summary>Regala el primer ramo del inventario al NPC con el que hablas; reacciona en la conversación.</summary>
        public async void GiveActiveBouquet()
        {
            if (isWaiting || currentNPC == null || _runner == null) return;
            if (flowerService == null) flowerService = FindFirstObjectByType<FlowerService>();

            var D = SproutGameDirector.Instance;
            if (D == null || flowerService == null) { SetStatus("Falta el sistema de flores en la escena."); return; }

            if (!System.Enum.TryParse(currentNPC.npcName.Trim(), true, out NpcId npc))
            { SetStatus($"No reconozco a {currentNPC.npcName} como vecino (Mochi/Aster/Moth/Rix)."); return; }

            BouquetKind bouquet = BouquetKind.None;
            foreach (var kv in D.Inventory.Bouquets) if (kv.Value > 0) { bouquet = kv.Key; break; }
            if (bouquet == BouquetKind.None) { SetStatus("No tienes ningún ramo. Crea uno con C."); return; }

            string ctx = flowerService.GiveBouquetTo(bouquet, npc);
            string giftText = $"*Te doy un {PrettyBouquet(bouquet)}.* {ctx}";

            try { await ProcessUserInput(giftText); }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                SetStatus("Ha ocurrido un error.");
                isWaiting = false;
                SetInputInteractable(true);
            }
        }

        private static string PrettyBouquet(BouquetKind k) => k switch
        {
            BouquetKind.Peace        => "Ramo de Paz",
            BouquetKind.HiddenDesire => "Ramo de Deseo Oculto",
            BouquetKind.Comfort      => "Ramo de Consuelo",
            BouquetKind.Obsession    => "Ramo de Obsesión",
            BouquetKind.Promise      => "Ramo de Promesa",
            BouquetKind.Confession   => "Ramo de Confesión",
            BouquetKind.Farewell     => "Ramo de Despedida",
            BouquetKind.Suspicion    => "Ramo de Sospecha",
            _ => "ramo"
        };

        // ── Elecciones a mano (ChoiceNode) ──────────────────────────────────────

        private void SetInputVisible(bool visible)
        {
            if (inputField != null) inputField.gameObject.SetActive(visible);
            if (sendButton != null) sendButton.gameObject.SetActive(visible);
        }

        private void ShowOptions(OptionsNodeSO node)
        {
            ClearChoices();
            SetInputVisible(false);   // sin escribir: solo botones
            EnsureChoiceArea();
            if (_choiceArea == null) return;

            foreach (var option in node.options)
            {
                if (option == null || option.nextNode == null) continue;
                var o = option; // captura para el listener
                var btn = CreateChoiceButton(string.IsNullOrWhiteSpace(o.text) ? "..." : o.text);
                btn.onClick.AddListener(() => OnOptionPicked(o));
            }
        }

        private void OnOptionPicked(OptionData option)
        {
            if (currentNPC == null || _runner == null || option == null) return;
            ClearChoices();

            string label = string.IsNullOrWhiteSpace(option.text) ? "" : option.text;
            onPlayerMessageSent?.Invoke(label);   // Torrance: cuenta la opción elegida

            var target = option.nextNode;

            // Gate: si no cumple requisitos, mensaje de bloqueo y se vuelven a mostrar las opciones.
            if (target != null && !currentNPC.MeetsRequirements(target))
            {
                string locked = !string.IsNullOrWhiteSpace(target.lockedReply) ? target.lockedReply : "Aún no puedes por aquí.";
                onNPCReplied?.Invoke(locked);
                _closeAfterType = false;
                ShowNPCMessage(locked);   // al acabar, como seguimos en el ChoiceNode, re-aparecen las opciones
                return;
            }

            _runner.AdvanceTo(target);
            _lastNode = _runner.Current;
            _closeAfterType = IsTerminal(_runner.Current);

            string opening = OpeningLineOf(_runner.Current);
            onNPCReplied?.Invoke(opening ?? "...");
            ShowNPCMessage(string.IsNullOrEmpty(opening) ? "..." : opening);
        }

        private void EnsureChoiceArea()
        {
            if (_choiceArea != null || dialoguePanel == null) return;
            var panelRT = dialoguePanel.GetComponent<RectTransform>();
            if (panelRT == null) return;

            var go = new GameObject("ChoiceArea", typeof(RectTransform), typeof(VerticalLayoutGroup));
            _choiceArea = go.GetComponent<RectTransform>();
            _choiceArea.SetParent(panelRT, false);
            _choiceArea.anchorMin = new Vector2(0.04f, 0.06f);
            _choiceArea.anchorMax = new Vector2(0.74f, 0.64f);
            _choiceArea.offsetMin = Vector2.zero; _choiceArea.offsetMax = Vector2.zero;

            var vlg = go.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 6;
            vlg.padding = new RectOffset(0, 0, 0, 0);
            vlg.childAlignment = TextAnchor.LowerLeft;
            vlg.childControlWidth = true; vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        }

        private Button CreateChoiceButton(string text)
        {
            var go = new GameObject("Choice",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(_choiceArea, false);
            go.GetComponent<Image>().color = new Color(0.96f, 0.86f, 0.72f, 1f);
            go.GetComponent<LayoutElement>().minHeight = 38;
            var btn = go.GetComponent<Button>();

            var txtGo = new GameObject("Label", typeof(RectTransform));
            var trt = txtGo.GetComponent<RectTransform>();
            trt.SetParent(go.transform, false);
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(12, 4); trt.offsetMax = new Vector2(-12, -4);
            var label = txtGo.AddComponent<TextMeshProUGUI>();
            label.text = "▸ " + text;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.fontSize = 16;
            label.color = new Color(0.23f, 0.18f, 0.16f);
            label.enableWordWrapping = true;

            _choiceButtons.Add(btn);
            return btn;
        }

        private void ClearChoices()
        {
            foreach (var b in _choiceButtons) if (b != null) Destroy(b.gameObject);
            _choiceButtons.Clear();
        }

        private void OnCloseClicked()
        {
            var trigger = currentNPC?.GetComponent<NPCInteractionTrigger>();
            if (trigger != null) trigger.CloseDialogue();
            else Close();
        }
    }
}