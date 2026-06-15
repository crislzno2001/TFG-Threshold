using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace OpenAI
{
    public class OpenAIApi
    {
        private const string BASE_URL = "https://api.openai.com/v1/";
        private string apiKey;
        private string organization;

        /// <summary>True once a non-empty API key has been resolved.</summary>
        public bool IsConfigured => !string.IsNullOrWhiteSpace(apiKey);

        // Resolves the key via AIConfig (env var → persistentDataPath →
        // StreamingAssets → ~/.openai/auth.json). Never hardcodes a key.
        public OpenAIApi()
        {
            apiKey = AIConfig.ApiKey;
            organization = AIConfig.Organization;
        }

        // Explicit key (used by tests / tooling). DEV use only.
        public OpenAIApi(string key, string org = null)
        {
            apiKey = key;
            organization = org;
        }

        // ---- HELPERS ----

        private UnityWebRequest CreateRequest(string endpoint, string method, string body = null)
        {
            var url = BASE_URL + endpoint;
            var request = new UnityWebRequest(url, method);

            if (!string.IsNullOrEmpty(body))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(body);
                request.uploadHandler = new UploadHandlerRaw(bytes);
            }

            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            if (!string.IsNullOrEmpty(organization))
                request.SetRequestHeader("OpenAI-Organization", organization);

            return request;
        }

        private async Task<T> SendRequest<T>(string endpoint, string jsonBody) where T : new()
        {
            using var request = CreateRequest(endpoint, "POST", jsonBody);
            var operation = request.SendWebRequest();

            while (!operation.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[OpenAI] Error {request.responseCode}: {request.downloadHandler.text}");
                return default;
            }

            return SimpleJson.Deserialize<T>(request.downloadHandler.text);
        }

        // ---- CONNECTION TEST ----

        /// <summary>
        /// Performs a tiny chat request to verify the key works and the network is
        /// reachable. Returns a categorised result for the AI setup screen.
        /// </summary>
        public async Task<AIConnectionResult> TestConnection(string model = "gpt-4o-mini", float timeoutSeconds = 15f)
        {
            if (!IsConfigured)
                return AIConnectionResult.Make(AIConnectionStatus.MissingKey,
                    "No API key configured. Add your OpenAI key to start the game.");

            var req = new CreateChatCompletionRequest
            {
                model = model,
                messages = new List<ChatMessage>
                {
                    new ChatMessage { role = "system", content = "Reply with the single word: ok" },
                    new ChatMessage { role = "user", content = "ping" }
                },
                temperature = 0f,
                max_tokens = 2,
                stream = false
            };

            string json = SimpleJson.SerializeChatRequest(req);

            using var request = CreateRequest("chat/completions", "POST", json);
            request.timeout = Mathf.Max(1, Mathf.CeilToInt(timeoutSeconds));

            var op = request.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            switch (request.result)
            {
                case UnityWebRequest.Result.Success:
                    return AIConnectionResult.Make(AIConnectionStatus.Ok, "AI connection OK.", 200);

                case UnityWebRequest.Result.ConnectionError:
                    return AIConnectionResult.Make(AIConnectionStatus.NetworkError,
                        "Could not reach OpenAI. Check your internet connection.", request.responseCode);

                case UnityWebRequest.Result.DataProcessingError:
                    return AIConnectionResult.Make(AIConnectionStatus.Unknown,
                        "Unexpected response from OpenAI.", request.responseCode);

                default: // ProtocolError (HTTP >= 400)
                    long code = request.responseCode;
                    if (code == 401 || code == 403)
                        return AIConnectionResult.Make(AIConnectionStatus.InvalidKey,
                            "API key was rejected (401/403). Check the key is valid and active.", code);
                    if (code == 429)
                        return AIConnectionResult.Make(AIConnectionStatus.RateLimited,
                            "Rate limited or out of quota (429). Try again shortly.", code);
                    if (code >= 500)
                        return AIConnectionResult.Make(AIConnectionStatus.ServerError,
                            "OpenAI server error. Try again in a moment.", code);
                    // Unity reports timeouts as a ProtocolError with code 0 sometimes.
                    if (code == 0)
                        return AIConnectionResult.Make(AIConnectionStatus.Timeout,
                            "The request timed out. Check your connection and try again.", 0);
                    return AIConnectionResult.Make(AIConnectionStatus.Unknown,
                        $"Unexpected error ({code}).", code);
            }
        }

        // ---- CHAT COMPLETION ----

        public async Task<CreateChatCompletionResponse> CreateChatCompletion(CreateChatCompletionRequest req)
        {
            req.stream = false;
            string json = SimpleJson.SerializeChatRequest(req);
            return await SendRequest<CreateChatCompletionResponse>("chat/completions", json);
        }

        // Streaming - devuelve tokens uno a uno via callback (igual que el paquete original)
        public async void CreateChatCompletionAsync(
            CreateChatCompletionRequest req,
            Action<List<CreateChatCompletionResponse>> onResponse,
            Action onComplete,
            CancellationTokenSource cancellationToken)
        {
            req.stream = true;
            string json = SimpleJson.SerializeChatRequest(req);

            using var request = CreateRequest("chat/completions", "POST", json);
            request.downloadHandler = new DownloadHandlerBuffer();

            var operation = request.SendWebRequest();
            var responses = new List<CreateChatCompletionResponse>();
            int lastPos = 0;

            while (!operation.isDone)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    request.Abort();
                    break;
                }

                string text = request.downloadHandler.text;
                if (text.Length > lastPos)
                {
                    string newText = text.Substring(lastPos);
                    lastPos = text.Length;

                    foreach (string line in newText.Split('\n'))
                    {
                        string trimmed = line.Trim();
                        if (!trimmed.StartsWith("data:")) continue;
                        string data = trimmed.Substring(5).Trim();
                        if (data == "[DONE]") continue;
                        if (string.IsNullOrEmpty(data)) continue;

                        try
                        {
                            var chunk = SimpleJson.Deserialize<CreateChatCompletionResponse>(data);
                            if (chunk != null)
                            {
                                responses.Add(chunk);
                                onResponse?.Invoke(responses);
                            }
                        }
                        catch { /* chunk incompleto, ignorar */ }
                    }
                }

                await Task.Yield();
            }

            onComplete?.Invoke();
        }
    }
}