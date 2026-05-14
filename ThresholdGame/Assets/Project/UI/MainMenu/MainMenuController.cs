using UnityEngine;
using UnityEngine.UIElements;
using ThresholdGame.Core.GameFlow;

namespace ThresholdGame.Presentation.UI.MainMenu
{
    /// <summary>
    /// Presenter del menú principal.
    /// Conecta los botones del UXML con las acciones de la GameStateMachine.
    /// No contiene lógica de juego — solo enlaza UI con dominio.
    ///
    /// Colócalo en el mismo GameObject que el UIDocument del menú.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class MainMenuController : MonoBehaviour
    {
        [Header("UI Toolkit")]
        [SerializeField] private UIDocument uiDocument;

        // Cacheamos los botones para no buscarlos en cada interacción
        private Button _btnNewGame;
        private Button _btnContinue;
        private Button _btnSettings;
        private Button _btnCredits;
        private Button _btnQuit;

        private void Reset()
        {
            uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            if (uiDocument == null)
                uiDocument = GetComponent<UIDocument>();

            VisualElement root = uiDocument.rootVisualElement;

            _btnNewGame  = root.Q<Button>("btn-new-game");
            _btnContinue = root.Q<Button>("btn-continue");
            _btnSettings = root.Q<Button>("btn-settings");
            _btnCredits  = root.Q<Button>("btn-credits");
            _btnQuit     = root.Q<Button>("btn-quit");

            _btnNewGame.clicked  += OnNewGameClicked;
            _btnSettings.clicked += OnSettingsClicked;
            _btnCredits.clicked  += OnCreditsClicked;
            _btnQuit.clicked     += OnQuitClicked;
        }

        private void OnDisable()
        {
            if (_btnNewGame  != null) _btnNewGame.clicked  -= OnNewGameClicked;
            if (_btnSettings != null) _btnSettings.clicked -= OnSettingsClicked;
            if (_btnCredits  != null) _btnCredits.clicked  -= OnCreditsClicked;
            if (_btnQuit     != null) _btnQuit.clicked     -= OnQuitClicked;
        }

        // ── Handlers ─────────────────────────────────────────────────────────

        private void OnNewGameClicked()
        {
            GameStateMachine.Instance?.StartGame();
        }

        private void OnSettingsClicked()
        {
            Debug.Log("[MainMenu] Settings — pendiente de implementar");
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
