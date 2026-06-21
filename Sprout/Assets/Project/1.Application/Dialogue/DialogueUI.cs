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

        private NPCBrain currentNPC;
        private bool isWaiting = false;
        private DialogueRunner _runner;
        private Coroutine _typing;
        private DialogueNodeSO _lastNode; // último nodo cuya frase inicial ya mostramos
        private bool _closeAfterType;     // cerrar el diálogo al acabar de teclear (nodo de despedida)

        private void Awake()
        {
            dialoguePanel.SetActive(false);

            if (sendButton != null) sendButton.onClick.AddListener(OnSendClicked);
            if (closeButton != null) closeButton.onClick.AddListener(OnCloseClicked);
            if (inputField != null) inputField.onSubmit.AddListener(_ => OnSendClicked());

            SetStatus("");
            SetDialogueText("");
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

            npcNameText.text = npc.npcName;
            SetDialogueText("");

            inputField.text = "";
            inputField.Select();
            inputField.ActivateInputField();

            SetStatus("");
            SetInputInteractable(true);

            _runner = npc.GetComponent<DialogueRunner>();
            if (_runner == null)
            {
                Debug.LogError($"[DialogueUI] El NPC '{npc.npcName}' no tiene componente DialogueRunner. Añádelo al mismo GameObject.", npc);
                return;
            }

            _runner.StartDialogue();

            _lastNode = _runner.Current;
            string opening = OpeningLineOf(_runner.Current);
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
                default:                   return null;
            }
        }

        /// <summary>True while the dialogue panel is visible.</summary>
        public bool IsOpen => dialoguePanel != null && dialoguePanel.activeSelf;

        public void Close()
        {
            if (_typing != null) { StopCoroutine(_typing); _typing = null; }
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
            _closeAfterType = IsTerminal(node); // nodo de despedida -> cerrar al acabar de teclear

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

            SetInputInteractable(true);
            if (inputField != null) { inputField.Select(); inputField.ActivateInputField(); }
        }

        /// <summary>True si el nodo no tiene salidas (es una despedida / final de rama).</summary>
        private static bool IsTerminal(DialogueNodeSO node)
        {
            if (node == null) return false;
            if (node.nextNodes != null && node.nextNodes.Count > 0) return false;
            if (node is ChoiceNodeSO c && c.choices != null && c.choices.Count > 0) return false;
            if (node is SpeechNodeSO s && s.transitions != null && s.transitions.Count > 0) return false;
            return true;
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
        }

        private void OnCloseClicked()
        {
            var trigger = currentNPC?.GetComponent<NPCInteractionTrigger>();
            if (trigger != null) trigger.CloseDialogue();
            else Close();
        }
    }
}