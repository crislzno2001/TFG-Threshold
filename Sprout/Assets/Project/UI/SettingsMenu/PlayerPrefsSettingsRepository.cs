using UnityEngine;

namespace ThresholdGame.Infrastructure.Settings
{
    using ThresholdGame.Core.Settings;

    /// <summary>
    /// Implementación del repositorio usando PlayerPrefs.
    /// Si quieres migrar a JSON, cloud save o un servicio externo, sustituye
    /// esta clase sin tocar nada más.
    /// </summary>
    public sealed class PlayerPrefsSettingsRepository : ISettingsRepository
    {
        // Las claves se centralizan para evitar errores y facilitar el rename.
        private const string KMaster   = "settings.master";
        private const string KMusic    = "settings.music";
        private const string KSfx      = "settings.sfx";
        private const string KAiVoice  = "settings.aiVoice";
        private const string KTextScale = "settings.textScale";

        public GameSettings Load()
        {
            return new GameSettings
            {
                MasterVolume  = PlayerPrefs.GetFloat(KMaster,   1f),
                MusicVolume   = PlayerPrefs.GetFloat(KMusic,    1f),
                SfxVolume     = PlayerPrefs.GetFloat(KSfx,      1f),
                AiVoiceVolume = PlayerPrefs.GetFloat(KAiVoice,  1f),
                TextScale     = PlayerPrefs.GetFloat(KTextScale, 1f)
            };
        }

        public void Save(GameSettings settings)
        {
            if (settings == null) return;

            PlayerPrefs.SetFloat(KMaster,    settings.MasterVolume);
            PlayerPrefs.SetFloat(KMusic,     settings.MusicVolume);
            PlayerPrefs.SetFloat(KSfx,       settings.SfxVolume);
            PlayerPrefs.SetFloat(KAiVoice,   settings.AiVoiceVolume);
            PlayerPrefs.SetFloat(KTextScale, settings.TextScale);
            PlayerPrefs.Save();
        }
    }
}
