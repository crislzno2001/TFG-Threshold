using System;
using System.IO;
using UnityEngine;

namespace OpenAI
{
    /// <summary>
    /// Resolves the OpenAI API key from several safe sources, in priority order.
    /// NEVER hardcodes a key. Resolution order:
    ///   1. Environment variable  OPENAI_API_KEY   (+ optional OPENAI_ORG)
    ///   2. &lt;persistentDataPath&gt;/sprout_ai.json   (per-user, writable at runtime)
    ///   3. &lt;StreamingAssets&gt;/openai_auth.json       (shipped config, optional)
    ///   4. ~/.openai/auth.json                       (legacy / dev machine)
    /// JSON files use the shape: { "apiKey": "sk-...", "organization": "" }
    /// </summary>
    public static class AIConfig
    {
        private static string _cachedKey;
        private static string _cachedOrg;
        private static bool _resolved;

        public static string ApiKey { get { EnsureResolved(); return _cachedKey; } }
        public static string Organization { get { EnsureResolved(); return _cachedOrg; } }
        public static bool HasKey => !string.IsNullOrWhiteSpace(ApiKey);

        /// <summary>Where this build expects a per-user key file to live.</summary>
        public static string UserKeyPath =>
            Path.Combine(Application.persistentDataPath, "sprout_ai.json");

        public static void Invalidate() => _resolved = false;

        private static void EnsureResolved()
        {
            if (_resolved) return;
            _resolved = true;
            _cachedKey = null;
            _cachedOrg = null;

            // 1. Environment variable.
            try
            {
                string envKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
                if (!string.IsNullOrWhiteSpace(envKey))
                {
                    _cachedKey = envKey.Trim();
                    _cachedOrg = Environment.GetEnvironmentVariable("OPENAI_ORG")?.Trim();
                    Debug.Log("[AIConfig] API key loaded from environment variable.");
                    return;
                }
            }
            catch { /* sandboxed platforms may block env access */ }

            // 2. persistentDataPath (set by the in-game AI setup screen).
            if (TryReadKeyFile(UserKeyPath, "persistentDataPath")) return;

            // 3. StreamingAssets.
            string streaming = Path.Combine(Application.streamingAssetsPath, "openai_auth.json");
            if (TryReadKeyFile(streaming, "StreamingAssets")) return;

            // 4. Legacy ~/.openai/auth.json.
            try
            {
                string legacy = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".openai", "auth.json");
                if (TryReadKeyFile(legacy, "~/.openai/auth.json")) return;
            }
            catch { }

            Debug.LogWarning("[AIConfig] No API key found in any source. " +
                             "Set OPENAI_API_KEY or use the in-game AI setup screen.");
        }

        private static bool TryReadKeyFile(string path, string label)
        {
            try
            {
                if (!File.Exists(path)) return false;
                var auth = JsonUtility.FromJson<OpenAIAuth>(File.ReadAllText(path));
                if (auth == null || string.IsNullOrWhiteSpace(auth.apiKey)) return false;
                _cachedKey = auth.apiKey.Trim();
                _cachedOrg = auth.organization?.Trim();
                Debug.Log($"[AIConfig] API key loaded from {label}.");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AIConfig] Failed reading key file at {label}: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Persists a key entered via the in-game setup screen to persistentDataPath.
        /// </summary>
        public static bool SaveUserKey(string apiKey, string organization = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(apiKey)) return false;
                var auth = new OpenAIAuth { apiKey = apiKey.Trim(), organization = organization?.Trim() ?? "" };
                File.WriteAllText(UserKeyPath, JsonUtility.ToJson(auth));
                Invalidate();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[AIConfig] Could not save key: {e.Message}");
                return false;
            }
        }
    }
}
