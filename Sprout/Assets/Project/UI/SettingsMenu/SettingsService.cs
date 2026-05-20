using UnityEngine;
using UnityEngine.Audio;
using ThresholdGame.Architecture.Events;
using ThresholdGame.Core.Settings;
using ThresholdGame.Infrastructure.Settings;

namespace ThresholdGame.Application.Settings
{
    /// <summary>
    /// Servicio global de ajustes.
    /// Carga ajustes al arrancar, los aplica al AudioMixer y al texto,
    /// y notifica cambios mediante un evento ScriptableObject.
    ///
    /// Colócalo en el GameManager de la escena Bootstrap.
    /// </summary>
    public sealed class SettingsService : MonoBehaviour
    {
        public static SettingsService Instance { get; private set; }

        [Header("Audio")]
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private string masterParam  = "MasterVolume";
        [SerializeField] private string musicParam   = "MusicVolume";
        [SerializeField] private string sfxParam     = "SfxVolume";
        [SerializeField] private string aiVoiceParam = "AiVoiceVolume";

        [Header("Eventos")]
        [Tooltip("Se dispara cuando los ajustes cambian, para que la UI reactive el tamaño de texto, etc.")]
        [SerializeField] private GameEventSO onSettingsChanged;

        public GameSettings Current { get; private set; }

        private ISettingsRepository _repository;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            _repository = new PlayerPrefsSettingsRepository();
            Current = _repository.Load();

            ApplyAll();
        }

        // ── API pública ──────────────────────────────────────────────────────

        public void SetMasterVolume(float value01)
        {
            Current.MasterVolume = Mathf.Clamp01(value01);
            ApplyVolume(masterParam, Current.MasterVolume);
            PersistAndNotify();
        }

        public void SetMusicVolume(float value01)
        {
            Current.MusicVolume = Mathf.Clamp01(value01);
            ApplyVolume(musicParam, Current.MusicVolume);
            PersistAndNotify();
        }

        public void SetSfxVolume(float value01)
        {
            Current.SfxVolume = Mathf.Clamp01(value01);
            ApplyVolume(sfxParam, Current.SfxVolume);
            PersistAndNotify();
        }

        public void SetAiVoiceVolume(float value01)
        {
            Current.AiVoiceVolume = Mathf.Clamp01(value01);
            ApplyVolume(aiVoiceParam, Current.AiVoiceVolume);
            PersistAndNotify();
        }

        public void SetTextScale(float scale)
        {
            Current.TextScale = Mathf.Clamp(scale, 0.5f, 2f);
            PersistAndNotify();
        }

        public void ResetToDefaults()
        {
            Current = GameSettings.Default;
            ApplyAll();
            PersistAndNotify();
        }

        // ── Internals ────────────────────────────────────────────────────────

        private void ApplyAll()
        {
            ApplyVolume(masterParam,  Current.MasterVolume);
            ApplyVolume(musicParam,   Current.MusicVolume);
            ApplyVolume(sfxParam,     Current.SfxVolume);
            ApplyVolume(aiVoiceParam, Current.AiVoiceVolume);
        }

        /// <summary>
        /// Convierte el rango lineal [0..1] a decibelios.
        /// Mute total cuando es 0 (silencia con -80 dB).
        /// </summary>
        private void ApplyVolume(string param, float value01)
        {
            if (audioMixer == null || string.IsNullOrWhiteSpace(param)) return;

            float db = value01 <= 0.0001f ? -80f : Mathf.Log10(value01) * 20f;
            audioMixer.SetFloat(param, db);
        }

        private void PersistAndNotify()
        {
            _repository.Save(Current);
            onSettingsChanged?.Raise();
        }
    }
}
