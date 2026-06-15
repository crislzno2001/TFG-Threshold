using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using Sprout.Application.AI;

namespace Sprout.Presentation.UI
{
    /// <summary>
    /// Mandatory AI setup / connection screen. Shown before gameplay. If the key is
    /// missing or the test fails, the player stays here. On success, enables the
    /// gameplay roots and raises onConnected.
    /// </summary>
    public class AISetupScreen : MonoBehaviour
    {
        [SerializeField] private AIConnectionService connection;

        [Header("UI")]
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_InputField keyInput;
        [SerializeField] private Button submitButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private GameObject keyEntryGroup;
        [SerializeField] private GameObject busySpinner;

        [Header("On success")]
        [Tooltip("Objects enabled when AI connects (3D gameplay root, HUD). Kept disabled until then.")]
        [SerializeField] private GameObject[] enableOnConnect;

        [Header("Events")]
        public UnityEvent onConnected;

        private void Awake()
        {
            if (submitButton != null) submitButton.onClick.AddListener(OnSubmit);
            if (retryButton != null) retryButton.onClick.AddListener(OnRetry);

            if (connection != null)
            {
                connection.OnTestStarted.AddListener(ShowBusy);
                connection.OnConnected.AddListener(HandleConnected);
                connection.OnFailed.AddListener(HandleFailed);
                connection.OnNeedsKey.AddListener(HandleNeedsKey);
            }
        }

        private void OnEnable() => RunCheck();

        public void RunCheck()
        {
            Show(true);
            if (connection != null) connection.RunCheck();
            else SetStatus("AIConnectionService not assigned.");
        }

        private void OnSubmit()
        {
            string key = keyInput != null ? keyInput.text : "";
            if (string.IsNullOrWhiteSpace(key)) { SetStatus("Please paste your OpenAI API key."); return; }
            ShowBusy();
            connection?.SubmitKey(key.Trim());
        }

        private void OnRetry() => RunCheck();

        private void ShowBusy()
        {
            if (busySpinner != null) busySpinner.SetActive(true);
            SetStatus("Checking AI connection...");
            SetKeyEntry(false);
        }

        private void HandleConnected()
        {
            if (busySpinner != null) busySpinner.SetActive(false);
            SetStatus("Connected. Welcome to Sprout.");
            Show(false);
            if (enableOnConnect != null)
                foreach (var go in enableOnConnect)
                    if (go != null) go.SetActive(true);
            onConnected?.Invoke();
        }

        private void HandleFailed(string msg)
        {
            if (busySpinner != null) busySpinner.SetActive(false);
            SetStatus("Connection problem:\n" + msg);
            SetKeyEntry(true);
        }

        private void HandleNeedsKey(string msg)
        {
            if (busySpinner != null) busySpinner.SetActive(false);
            SetStatus(msg);
            SetKeyEntry(true);
        }

        private void SetKeyEntry(bool on) { if (keyEntryGroup != null) keyEntryGroup.SetActive(on); }
        private void SetStatus(string s) { if (statusText != null) statusText.text = s; }
        private void Show(bool on) { if (panel != null) panel.SetActive(on); }
    }
}
