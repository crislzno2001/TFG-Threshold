using UnityEngine;
using UnityEngine.UIElements;
using ThresholdGame.Core.GameFlow;

namespace ThresholdGame.Presentation.UI.Pause
{
    /// <summary>
    /// Presenter del menú de pausa.
    /// Permanece SIEMPRE activo para que los GameEventListener se registren bien;
    /// la visibilidad se controla por estilo del root (Show/Hide).
    ///
    /// Vive en la escena Game junto a su UIDocument.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class PauseMenuController : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;

        private VisualElement _root;
        private Button _btnResume;
        private Button _btnSave;
        private Button _btnSettings;
        private Button _btnMainMenu;

        private void Reset() => uiDocument = GetComponent<UIDocument>();

        private void Awake()
        {
            if (uiDocument == null)
                uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            _root = uiDocument.rootVisualElement;

            _btnResume = _root.Q<Button>("btn-resume");
            _btnSave = _root.Q<Button>("btn-save");
            _btnSettings = _root.Q<Button>("btn-settings");
            _btnMainMenu = _root.Q<Button>("btn-main-menu");

            _btnResume.clicked += OnResumeClicked;
            if (_btnSave != null) _btnSave.clicked += OnSaveClicked;
            _btnSettings.clicked += OnSettingsClicked;
            _btnMainMenu.clicked += OnMainMenuClicked;

            Hide(); // arranca oculto
        }

        private void OnDisable()
        {
            if (_btnResume != null) _btnResume.clicked -= OnResumeClicked;
            if (_btnSave != null) _btnSave.clicked -= OnSaveClicked;
            if (_btnSettings != null) _btnSettings.clicked -= OnSettingsClicked;
            if (_btnMainMenu != null) _btnMainMenu.clicked -= OnMainMenuClicked;
        }

        // ── API pública (la llaman los GameEventListener) ────────────────────

        public void Show()
        {
            if (_root == null) return;
            _root.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            if (_root == null) return;
            _root.style.display = DisplayStyle.None;
        }

        // ── Handlers ─────────────────────────────────────────────────────────

        private void OnResumeClicked()
        {
            GameStateMachine.Instance?.Resume();
        }

        private void OnSaveClicked()
        {
            var save = Object.FindFirstObjectByType<Sprout.Persistence.SaveSystem>();
            if (save != null)
            {
                save.Save();
                if (_btnSave != null)
                {
                    _btnSave.text = "¡Guardado!";
                    CancelInvoke(nameof(ResetSaveText));
                    Invoke(nameof(ResetSaveText), 1.3f);
                }
            }
            else if (_btnSave != null) _btnSave.text = "No hay SaveSystem";
        }

        private void ResetSaveText()
        {
            if (_btnSave != null) _btnSave.text = "Guardar";
        }

        private void OnSettingsClicked()
        {
            var settings = Object.FindFirstObjectByType<ThresholdGame.Presentation.UI.Settings.SettingsMenuController>(FindObjectsInactive.Include);
            if (settings == null)
            {
                Debug.LogWarning("[PauseMenu] No hay SettingsMenuController en la escena.");
                return;
            }
            Hide(); // ocultar la pausa mientras se ve la configuración
            settings.OnCloseRequested = () => { settings.Hide(); Show(); }; // al cerrar config, volver a pausa
            settings.Show();
        }

        private void OnMainMenuClicked()
        {
            GameStateMachine.Instance?.GoToMainMenu();
        }
    }
}