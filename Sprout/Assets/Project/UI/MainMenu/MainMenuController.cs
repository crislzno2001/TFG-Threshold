using UnityEngine;
using UnityEngine.UIElements;
using ThresholdGame.Core.GameFlow;
using ThresholdGame.Presentation.UI.Settings;

namespace ThresholdGame.Presentation.UI.MainMenu
{
    /// <summary>
    /// Presenter del menú principal.
    /// Conecta los botones del UXML con las acciones del juego.
    /// Delega en SettingsMenuController para mostrar/ocultar configuración.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class MainMenuController : MonoBehaviour
    {
        [Header("UI Toolkit")]
        [SerializeField] private UIDocument uiDocument;

        [Header("Referencias a otros paneles")]
        [SerializeField] private SettingsMenuController settingsMenu;

        private VisualElement _root;

        private Button _btnNewGame;
        private Button _btnContinue;
        private Button _btnSettings;
        private Button _btnCredits;
        private Button _btnQuit;

        private void Reset() => uiDocument = GetComponent<UIDocument>();

        private void OnEnable()
        {
            if (uiDocument == null)
                uiDocument = GetComponent<UIDocument>();

            _root = uiDocument.rootVisualElement;

            _btnNewGame = _root.Q<Button>("btn-new-game");
            _btnContinue = _root.Q<Button>("btn-continue");
            _btnSettings = _root.Q<Button>("btn-settings");
            _btnCredits = _root.Q<Button>("btn-credits");
            _btnQuit = _root.Q<Button>("btn-quit");

            _btnNewGame.clicked += OnNewGameClicked;
            _btnSettings.clicked += OnSettingsClicked;
            _btnCredits.clicked += OnCreditsClicked;
            _btnQuit.clicked += OnQuitClicked;

            if (settingsMenu != null)
                settingsMenu.OnCloseRequested = ShowMainMenu;
        }

        private void OnDisable()
        {
            if (_btnNewGame != null) _btnNewGame.clicked -= OnNewGameClicked;
            if (_btnSettings != null) _btnSettings.clicked -= OnSettingsClicked;
            if (_btnCredits != null) _btnCredits.clicked -= OnCreditsClicked;
            if (_btnQuit != null) _btnQuit.clicked -= OnQuitClicked;
        }

        // ── Visibilidad ───────────────────────────────────────────────────────

        private void HideMainMenu()
        {
            if (_root != null) _root.style.display = DisplayStyle.None;
        }

        private void ShowMainMenu()
        {
            if (_root != null) _root.style.display = DisplayStyle.Flex;
        }

        // ── Handlers ─────────────────────────────────────────────────────────

        private void OnNewGameClicked()
        {
            GameStateMachine.Instance?.StartGame();
        }

        private void OnSettingsClicked()
        {
            if (settingsMenu == null)
            {
                Debug.LogWarning("[MainMenu] SettingsMenu reference is missing.", this);
                return;
            }

            HideMainMenu();
            settingsMenu.Show();
        }

        private void OnCreditsClicked()
        {
            Debug.Log("[MainMenu] Credits — pendiente de implementar");
        }

        private void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}