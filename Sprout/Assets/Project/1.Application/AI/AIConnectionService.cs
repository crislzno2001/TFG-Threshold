using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using OpenAI;
using Sprout.Data;

namespace Sprout.Application.AI
{
    /// <summary>
    /// Runs the startup AI connection test and gates the game on success.
    /// If AI is unreachable or unconfigured, the AI setup screen is shown and the
    /// game does NOT start (per design: AI is mandatory).
    /// </summary>
    public class AIConnectionService : MonoBehaviour
    {
        [SerializeField] private AISettingsSO settings;

        [Header("Events")]
        public UnityEvent OnTestStarted;
        public UnityEvent OnConnected;
        public UnityEvent<string> OnFailed;     // friendly message
        public UnityEvent<string> OnNeedsKey;   // friendly message

        public bool IsConnected { get; private set; }
        public AIConnectionResult LastResult { get; private set; }

        public async void RunCheck()
        {
            OnTestStarted?.Invoke();

            if (settings != null && settings.useMockAI)
            {
                Debug.LogWarning("[AIConnectionService] DEV: Mock AI enabled — skipping real connection test.");
                IsConnected = true;
                OnConnected?.Invoke();
                return;
            }

            if (!AIConfig.HasKey)
            {
                IsConnected = false;
                LastResult = AIConnectionResult.Make(AIConnectionStatus.MissingKey,
                    "No API key found.\nPut it in  ~/.openai/auth.json\n" +
                    "(Windows: C:\\Users\\<you>\\.openai\\auth.json)\n" +
                    "as  { \"apiKey\": \"sk-...\" }  then press Retry.");
                OnNeedsKey?.Invoke(LastResult.Message);
                return;
            }

            var api = new OpenAIApi();
            string model = settings != null ? settings.connectionTestModel : "gpt-4o-mini";
            float timeout = settings != null ? settings.requestTimeoutSeconds : 15f;

            AIConnectionResult result = await api.TestConnection(model, timeout);
            LastResult = result;

            if (result.IsOk)
            {
                IsConnected = true;
                Debug.Log("[AIConnectionService] AI connection verified.");
                OnConnected?.Invoke();
            }
            else
            {
                IsConnected = false;
                Debug.LogWarning($"[AIConnectionService] AI check failed: {result.Status} — {result.Message}");
                if (result.Status == AIConnectionStatus.MissingKey)
                    OnNeedsKey?.Invoke(result.Message);
                else
                    OnFailed?.Invoke(result.Message);
            }
        }

        /// <summary>Called by the setup screen after the user enters a key.</summary>
        public void SubmitKey(string apiKey)
        {
            if (AIConfig.SaveUserKey(apiKey))
                RunCheck();
            else
                OnFailed?.Invoke("That key could not be saved. Please check it and try again.");
        }
    }
}
