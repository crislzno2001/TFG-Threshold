using System.Collections;
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
            chatDisplay.text = "";

            npcNameText.text = npc.npcName;

            // Mensaje de bienvenida
            AppendNPC($"(Se acerca {npc.npcName})");

            inputField.text = "";
            inputField.Select();
            inputField.ActivateInputField();

            SetStatus("");
            SetInputInteractable(true);
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

            string reply = await currentNPC.SendMessage(userText);

            SetStatus("");
            AppendNPC(reply);

            isWaiting = false;
            SetInputInteractable(true);
            inputField.Select();
            inputField.ActivateInputField();

            ScrollToBottom();
        }

        // ---- HELPERS UI ----

        private void AppendPlayer(string text)
        {
            chatDisplay.text += $"\n<color=#88ccff><b>Tú:</b></color> {text}";
            ScrollToBottom();
        }

        private void AppendNPC(string text)
        {
            chatDisplay.text += $"\n<color=#ffcc88><b>{currentNPC.npcName}:</b></color> {text}";
            ScrollToBottom();
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
            StartCoroutine(ScrollNextFrame());
        }

        private IEnumerator ScrollNextFrame()
        {
            yield return null;
            scrollRect.verticalNormalizedPosition = 0f;
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