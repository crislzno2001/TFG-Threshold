using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace OpenAI.Dialogue
{
    /// <summary>
    /// Controla la UI del recuadro de diálogo.
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        [Header("Panel principal")]
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private TMP_Text npcNameText;
        [SerializeField] private TMP_Text chatDisplay;
        [SerializeField] private ScrollRect scrollRect;

        [Header("Input de texto")]
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private Button sendButton;


        [Header("Feedback")]
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button closeButton;

        private NPCBrain currentNPC;
        private bool isWaiting = false;
        private bool _scrollPending = false;

        // Buffer de líneas para no reconstruir todo el string en cada mensaje
        private readonly List<string> _chatLines = new List<string>();
        private readonly StringBuilder _sb = new StringBuilder();

        private DialogueRunner _runner;

        private void Awake()
        {


            dialoguePanel.SetActive(false);

            if (sendButton != null) sendButton.onClick.AddListener(OnSendClicked);
            if (closeButton != null) closeButton.onClick.AddListener(OnCloseClicked);
            if (inputField != null) inputField.onSubmit.AddListener(_ => OnSendClicked());

            SetStatus("");
        }

        public void Open(NPCBrain npc)
        {
            currentNPC = npc;
            dialoguePanel.SetActive(true);
            _chatLines.Clear();
            chatDisplay.text = "";

            npcNameText.text = npc.npcName;

            // Mensaje de bienvenida
            AppendNPC($"(Se acerca {npc.npcName})");

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

            // Si el nodo de entrada tiene frase de apertura, mostrarla
            if (_runner.Current is SpeechNodeSO speechNode &&
                !string.IsNullOrEmpty(speechNode.openingLine))
                AppendNPC(speechNode.openingLine);



        }

        public void Close()
        {
            dialoguePanel.SetActive(false);
            currentNPC = null;
        }

        // ---- ENVIAR TEXTO ----

        private async void OnSendClicked()
        {
            if (isWaiting || currentNPC == null) return;

            string userText = inputField.text.Trim();
            if (string.IsNullOrEmpty(userText)) return;

            inputField.text = "";
            await ProcessUserInput(userText);
        }



        // ---- PROCESAR INPUT (texto o voz) ----

        private async System.Threading.Tasks.Task ProcessUserInput(string userText)
        {
            isWaiting = true;
            SetInputInteractable(false);

            AppendPlayer(userText);
            SetStatus($"{currentNPC.npcName} está pensando...");

            string reply = await _runner.ProcessMessage(userText);

            SetStatus("");
            AppendNPC(reply);

            isWaiting = false;
            SetInputInteractable(true);
            inputField.Select();
            inputField.ActivateInputField();

            ScrollToBottom();
        }

        // ---- HELPERS UI ----

        private void AppendLine(string line)
        {
            _chatLines.Add(line);
            _sb.Clear();
            for (int i = 0; i < _chatLines.Count; i++)
            {
                if (i > 0) _sb.Append('\n');
                _sb.Append(_chatLines[i]);
            }
            chatDisplay.text = _sb.ToString();
            ScrollToBottom();
        }

        private void AppendPlayer(string text)
        {
            AppendLine($"<color=#88ccff><b>Tú:</b></color> {text}");
        }

        private void AppendNPC(string text)
        {
            AppendLine($"<color=#ffcc88><b>{currentNPC.npcName}:</b></color> {text}");
        }

        private void SetStatus(string msg)
        {
            if (statusText != null)
                statusText.text = msg;
        }

        private void SetInputInteractable(bool value)
        {
            inputField.interactable = value;
            sendButton.interactable = value;
        }


        private void ScrollToBottom()
        {
            // Evita lanzar múltiples coroutines si hay varios mensajes seguidos
            if (!_scrollPending)
            {
                _scrollPending = true;
                StartCoroutine(ScrollNextFrame());
            }
        }

        private IEnumerator ScrollNextFrame()
        {
            yield return null;
            scrollRect.verticalNormalizedPosition = 0f;
            _scrollPending = false;
        }

        private void OnCloseClicked()
        {
            // Notificar al trigger para que también desbloquee al jugador
            var trigger = currentNPC?.GetComponent<NPCInteractionTrigger>();
            if (trigger != null) trigger.CloseDialogue();
            else Close();
        }





    }
}