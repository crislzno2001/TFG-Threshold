using UnityEngine;
using UnityEngine.UIElements;
using ThresholdGame.Application.Settings;

namespace ThresholdGame.Presentation.UI.Settings
{
    /// <summary>
    /// Presenter del menú de configuración.
    /// Lee el estado actual de SettingsService, escucha cambios de los sliders
    /// y delega en el servicio. No contiene lógica de negocio.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class SettingsMenuController : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;

        private VisualElement _root;

        private Slider _sliderMaster;
        private Slider _sliderMusic;
        private Slider _sliderSfx;
        private Slider _sliderAiVoice;
        private Slider _sliderText;

        private Label _valueMaster;
        private Label _valueMusic;
        private Label _valueSfx;
        private Label _valueAiVoice;
        private Label _valueText;

        private Button _btnReset;
        private Button _btnBack;

        // Callback que se ejecuta al pulsar "Volver" — lo asigna quien abre el menú.
        public System.Action OnCloseRequested;

        private void Reset() => uiDocument = GetComponent<UIDocument>();

        private void Awake()
        {
            if (uiDocument == null)
                uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            _root = uiDocument.rootVisualElement;

            // Sliders
            _sliderMaster  = _root.Q<Slider>("slider-master");
            _sliderMusic   = _root.Q<Slider>("slider-music");
            _sliderSfx     = _root.Q<Slider>("slider-sfx");
            _sliderAiVoice = _root.Q<Slider>("slider-aivoice");
            _sliderText    = _root.Q<Slider>("slider-text");

            // Labels de valor
            _valueMaster  = _root.Q<Label>("value-master");
            _valueMusic   = _root.Q<Label>("value-music");
            _valueSfx     = _root.Q<Label>("value-sfx");
            _valueAiVoice = _root.Q<Label>("value-aivoice");
            _valueText    = _root.Q<Label>("value-text");

            // Botones
            _btnReset = _root.Q<Button>("btn-reset");
            _btnBack  = _root.Q<Button>("btn-back");

            BindFromService();

            _sliderMaster.RegisterValueChangedCallback(e =>
            {
                SettingsService.Instance.SetMasterVolume(e.newValue);
                _valueMaster.text = FormatPercent(e.newValue);
            });

            _sliderMusic.RegisterValueChangedCallback(e =>
            {
                SettingsService.Instance.SetMusicVolume(e.newValue);
                _valueMusic.text = FormatPercent(e.newValue);
            });

            _sliderSfx.RegisterValueChangedCallback(e =>
            {
                SettingsService.Instance.SetSfxVolume(e.newValue);
                _valueSfx.text = FormatPercent(e.newValue);
            });

            _sliderAiVoice.RegisterValueChangedCallback(e =>
            {
                SettingsService.Instance.SetAiVoiceVolume(e.newValue);
                _valueAiVoice.text = FormatPercent(e.newValue);
            });

            _sliderText.RegisterValueChangedCallback(e =>
            {
                SettingsService.Instance.SetTextScale(e.newValue);
                _valueText.text = FormatPercent(e.newValue);
            });

            _btnReset.clicked += OnResetClicked;
            _btnBack.clicked  += OnBackClicked;

            Hide();
        }

        // ── API pública (la llaman GameEventListeners o presenters externos) ─

        public void Show()
        {
            if (_root == null) return;
            BindFromService();
            _root.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            if (_root == null) return;
            _root.style.display = DisplayStyle.None;
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        private void BindFromService()
        {
            if (SettingsService.Instance == null) return;
            var s = SettingsService.Instance.Current;

            _sliderMaster.SetValueWithoutNotify(s.MasterVolume);
            _sliderMusic.SetValueWithoutNotify(s.MusicVolume);
            _sliderSfx.SetValueWithoutNotify(s.SfxVolume);
            _sliderAiVoice.SetValueWithoutNotify(s.AiVoiceVolume);
            _sliderText.SetValueWithoutNotify(s.TextScale);

            _valueMaster.text  = FormatPercent(s.MasterVolume);
            _valueMusic.text   = FormatPercent(s.MusicVolume);
            _valueSfx.text     = FormatPercent(s.SfxVolume);
            _valueAiVoice.text = FormatPercent(s.AiVoiceVolume);
            _valueText.text    = FormatPercent(s.TextScale);
        }

        private string FormatPercent(float value) => $"{Mathf.RoundToInt(value * 100f)}%";

        private void OnResetClicked()
        {
            SettingsService.Instance.ResetToDefaults();
            BindFromService();
        }

        private void OnBackClicked()
        {
            Hide();
            OnCloseRequested?.Invoke();
        }
    }
}
