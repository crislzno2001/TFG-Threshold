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
            _btnSettings = _root.Q<Button>("btn-settings");
            _btnMainMenu = _root.Q<Button>("btn-main-menu");

            _btnResume.clicked += OnResumeClicked;
            _btnSettings.clicked += OnSettingsClicked;
            _btnMainMenu.clicked += OnMainMenuClicked;

            Hide(); // arranca oculto
        }

        private void OnDisable()
        {
            if (_btnResume != null) _btnResume.clicked -= OnResumeClicked;
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

        private void OnSettingsClicked()
        {
            Debug.Log("[PauseMenu] Settings — pendiente de implementar");
        }

        private void OnMainMenuClicked()
        {
            GameStateMachine.Instance?.GoToMainMenu();
        }
    }
}