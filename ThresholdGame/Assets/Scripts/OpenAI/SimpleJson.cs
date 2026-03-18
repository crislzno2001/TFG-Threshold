using System;
using System.Collections.Generic;
using System.Text;

namespace OpenAI
{
    /// <summary>
    /// Serializador/deserializador JSON hecho a mano.
    /// Sin Newtonsoft, sin System.Text.Json. 100% compatible con Unity/Mono.
    /// </summary>
    internal static class SimpleJson
    {
        // ---- SERIALIZACIÓN ----

        public static string SerializeChatRequest(CreateChatCompletionRequest req)
        {
            var sb = new StringBuilder();
            sb.Append("{");
            sb.Append($"\"model\":\"{req.model}\",");
            sb.Append($"\"temperature\":{req.temperature.ToString(System.Globalization.CultureInfo.InvariantCulture)},");
            sb.Append($"\"max_tokens\":{req.max_tokens},");
            sb.Append($"\"stream\":{(req.stream ? "true" : "false")},");
            sb.Append("\"messages\":[");

            for (int i = 0; i < req.messages.Count; i++)
            {
                var msg = req.messages[i];
                sb.Append("{");
                sb.Append($"\"role\":\"{Escape(msg.role)}\",");
                sb.Append($"\"content\":\"{Escape(msg.content)}\"");
                sb.Append("}");
                if (i < req.messages.Count - 1) sb.Append(",");
            }

            sb.Append("]}");
            return sb.ToString();
        }

     

        // ---- DESERIALIZACIÓN ----

        public static T Deserialize<T>(string json) where T : new()
        {
            if (string.IsNullOrEmpty(json)) return default;

            var type = typeof(T);

            if (type == typeof(CreateChatCompletionResponse))
                return (T)(object)ParseChatResponse(json);

            return default;
        }

        // ---- PARSERS ESPECÍFICOS ----

        private static CreateChatCompletionResponse ParseChatResponse(string json)
        {
            var res = new CreateChatCompletionResponse();
            res.id = GetString(json, "id");
            res.model = GetString(json, "model");
            res.choices = new List<ChatChoice>();

            string choicesArr = GetArray(json, "choices");
            if (choicesArr == null) return res;

            foreach (string item in SplitObjects(choicesArr))
            {
                var choice = new ChatChoice();
                choice.finish_reason = GetString(item, "finish_reason");

                // Respuesta normal (message)
                string msgObj = GetObject(item, "message");
                if (msgObj != null)
                {
                    choice.message = new ChatMessage
                    {
                        role = GetString(msgObj, "role"),
                        content = GetString(msgObj, "content")
                    };
                }

                // Streaming (delta)
                string deltaObj = GetObject(item, "delta");
                if (deltaObj != null)
                {
                    choice.delta = new ChatMessageDelta
                    {
                        role = GetString(deltaObj, "role"),
                        content = GetString(deltaObj, "content")
                    };
                }

                res.choices.Add(choice);
            }

            return res;
        }

    

    

        // ---- UTILIDADES DE PARSING ----

        public static string GetString(string json, string key)
        {
            string search = $"\"{key}\"";
            int keyIdx = json.IndexOf(search, StringComparison.Ordinal);
            if (keyIdx < 0) return null;

            int colonIdx = json.IndexOf(':', keyIdx + search.Length);
            if (colonIdx < 0) return null;

            int start = colonIdx + 1;
            while (start < json.Length && json[start] == ' ') start++;

            if (start >= json.Length) return null;

            if (json[start] == '"')
            {
                int end = start + 1;
                while (end < json.Length)
                {
                    if (json[end] == '"' && json[end - 1] != '\\') break;
                    end++;
                }
                return UnescapeJson(json.Substring(start + 1, end - start - 1));
            }
            else if (json[start] == 'n')
            {
                return null;
            }
            else
            {
                int end = start;
                while (end < json.Length && json[end] != ',' && json[end] != '}' && json[end] != ']')
                    end++;
                return json.Substring(start, end - start).Trim();
            }
        }

        private static string GetArray(string json, string key)
        {
            string search = $"\"{key}\"";
            int keyIdx = json.IndexOf(search, StringComparison.Ordinal);
            if (keyIdx < 0) return null;

            int bracketIdx = json.IndexOf('[', keyIdx + search.Length);
            if (bracketIdx < 0) return null;

            return ExtractBalanced(json, bracketIdx, '[', ']');
        }

        private static string GetObject(string json, string key)
        {
            string search = $"\"{key}\"";
            int keyIdx = json.IndexOf(search, StringComparison.Ordinal);
            if (keyIdx < 0) return null;

            int braceIdx = json.IndexOf('{', keyIdx + search.Length);
            if (braceIdx < 0) return null;

            return ExtractBalanced(json, braceIdx, '{', '}');
        }

        private static string ExtractBalanced(string json, int start, char open, char close)
        {
            int depth = 0;
            bool inString = false;

            for (int i = start; i < json.Length; i++)
            {
                char c = json[i];
                if (c == '"' && (i == 0 || json[i - 1] != '\\'))
                    inString = !inString;
                if (inString) continue;
                if (c == open) depth++;
                else if (c == close)
                {
                    depth--;
                    if (depth == 0)
                        return json.Substring(start, i - start + 1);
                }
            }
            return null;
        }

        private static List<string> SplitObjects(string arrayJson)
        {
            var result = new List<string>();
            string inner = arrayJson.Trim();
            if (inner.StartsWith("[")) inner = inner.Substring(1);
            if (inner.EndsWith("]")) inner = inner.Substring(0, inner.Length - 1);

            int depth = 0;
            bool inString = false;
            int objStart = -1;

            for (int i = 0; i < inner.Length; i++)
            {
                char c = inner[i];
                if (c == '"' && (i == 0 || inner[i - 1] != '\\'))
                    inString = !inString;
                if (inString) continue;

                if (c == '{')
                {
                    if (depth == 0) objStart = i;
                    depth++;
                }
                else if (c == '}')
                {
                    depth--;
                    if (depth == 0 && objStart >= 0)
                    {
                        result.Add(inner.Substring(objStart, i - objStart + 1));
                        objStart = -1;
                    }
                }
            }
            return result;
        }

        // ---- ESCAPE / UNESCAPE ----

        public static string Escape(string s) =>
            s?.Replace("\\", "\\\\")
              .Replace("\"", "\\\"")
              .Replace("\n", "\\n")
              .Replace("\r", "\\r")
              .Replace("\t", "\\t") ?? "";

        private static string UnescapeJson(string s) =>
            s?.Replace("\\\"", "\"")
              .Replace("\\n", "\n")
              .Replace("\\r", "\r")
              .Replace("\\t", "\t")
              .Replace("\\\\", "\\") ?? "";
    }
}