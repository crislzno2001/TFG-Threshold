using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace OpenAI.Dialogue
{
    public class DialogueUI : MonoBehaviour
    {
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

        private NPCBrain currentNPC;
        private bool isWaiting = false;
        private DialogueRunner _runner;

        private void Awake()
        {
            dialoguePanel.SetActive(false);

            if (sendButton != null) sendButton.onClick.AddListener(OnSendClicked);
            if (closeButton != null) closeButton.onClick.AddListener(OnCloseClicked);
            if (inputField != null) inputField.onSubmit.AddListener(_ => OnSendClicked());

            SetStatus("");
            SetDialogueText("");
        }

        public void Open(NPCBrain npc)
        {
            currentNPC = npc;
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

            if (_runner.Current is SpeechNodeSO speechNode &&
                !string.IsNullOrEmpty(speechNode.openingLine))
            {
                ShowNPCMessage(speechNode.openingLine);
            }
        }

        public void Close()
        {
            dialoguePanel.SetActive(false);
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

            string reply = await _runner.ProcessMessage(userText);

            SetStatus("");
            ShowNPCMessage(string.IsNullOrWhiteSpace(reply) ? "..." : reply);

            isWaiting = false;
            SetInputInteractable(true);
            inputField.Select();
            inputField.ActivateInputField();
        }

        private void ShowNPCMessage(string text)
        {
            if (currentNPC == null) return;
            SetDialogueText($"<b>{currentNPC.npcName}:</b>\n{text}");
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