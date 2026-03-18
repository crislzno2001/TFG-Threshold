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

        // cargar desde archivo ~/.openai/auth.json 
        public OpenAIApi()
        {
            LoadAuthFromFile();
        }

        private void LoadAuthFromFile()
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".openai", "auth.json"
            );

            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                var auth = JsonUtility.FromJson<OpenAIAuth>(json);
                apiKey = auth.apiKey;
                organization = auth.organization;
            }
            else
            {
                Debug.LogWarning("[OpenAI] No se encontró ~/.openai/auth.json. Usa new OpenAIApi(\"tu-api-key\") o crea el archivo.");
            }
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