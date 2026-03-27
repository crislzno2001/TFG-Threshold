using System;
using System.Collections.Generic;

namespace OpenAI
{
    // ---- AUTH ----

    [Serializable]
    public class OpenAIAuth
    {
        public string apiKey;
        public string organization;
    }

    // ---- CHAT ----

    [Serializable]
    public class ChatMessage
    {
        public string role;
        public string content;
    }

    [Serializable]
    public class CreateChatCompletionRequest
    {
        public string model = "gpt-4o-mini";
        public List<ChatMessage> messages;
        public float temperature = 1f;
        public int max_tokens = 256;
        public bool stream = false;
    }

    [Serializable]
    public class ChatChoice
    {
        public ChatMessage message;
        public ChatMessageDelta delta;
        public string finish_reason;
        public int index;
    }

    [Serializable]
    public class ChatMessageDelta
    {
        public string role;
        public string content;
    }

    [Serializable]
    public class Usage
    {
        public int prompt_tokens;
        public int completion_tokens;
        public int total_tokens;
    }

    [Serializable]
    public class CreateChatCompletionResponse
    {
        public string id;
        public string Object;
        public long created;
        public string model;
        public List<ChatChoice> choices;
        public Usage usage;
    }
}