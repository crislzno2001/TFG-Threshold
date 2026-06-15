using UnityEngine;

namespace Sprout.Data
{
    /// <summary>
    /// Runtime AI configuration. NEVER store an API key in this asset — the key is
    /// resolved at runtime by AIConfig from environment variable, StreamingAssets,
    /// or ~/.openai/auth.json. This asset only holds model/behaviour settings.
    /// </summary>
    [CreateAssetMenu(fileName = "AISettings", menuName = "Sprout/AI Settings")]
    public class AISettingsSO : ScriptableObject
    {
        [Header("Model")]
        public string chatModel = "gpt-4o-mini";
        public string classifierModel = "gpt-4o-mini";

        [Header("Connection test")]
        [Tooltip("Model pinged on startup to verify the key works.")]
        public string connectionTestModel = "gpt-4o-mini";

        [Min(1f)]
        public float requestTimeoutSeconds = 20f;

        [Header("DEV ONLY")]
        [Tooltip("If true, NPC replies are mocked and NO network calls are made. " +
                 "MUST be false for the real playable game. Use only for offline tests.")]
        public bool useMockAI = false;
    }
}
