using UnityEngine;
using UnityEngine.UIElements;
using ThresholdGame.Core.GameFlow;

namespace ThresholdGame.Presentation.UI.Loading
{
    /// <summary>
    /// Presenter de la pantalla de carga.
    /// El GameObject permanece SIEMPRE activo para que los GameEventListener
    /// se registren correctamente. La visibilidad se controla por estilo del root.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class LoadingScreenController : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;

        private VisualElement _root;
        private VisualElement _progressFill;
        private Label _percentageLabel;

        private void Reset() => uiDocument = GetComponent<UIDocument>();

        private void Awake()
        {
            if (uiDocument == null)
                uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            // El rootVisualElement puede no existir aún en Awake — esperamos a OnEnable
            _root = uiDocument.rootVisualElement;
            _progressFill = _root.Q<VisualElement>("progress-fill");
            _percentageLabel = _root.Q<Label>("percentage");

            SetProgress(0f);
            Hide(); // arranca oculto
        }

        private void Update()
        {
            if (GameStateMachine.Instance == null) return;
            SetProgress(GameStateMachine.Instance.CurrentLoadProgress);
        }

        // ── API pública (llamada por GameEventListener mediante UnityEvents) ─

        public void Show()
        {
            Debug.Log("[LoadingScreen] Show() llamado");
            if (_root == null)
            {
                Debug.LogWarning("[LoadingScreen] _root es null!");
                return;
            }
            _root.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            Debug.Log("[LoadingScreen] Hide() llamado");
            if (_root == null) return;
            _root.style.display = DisplayStyle.None;
        }

        // ── Interno ──────────────────────────────────────────────────────────

        private void SetProgress(float progress01)
        {
            float percent = Mathf.Clamp01(progress01) * 100f;

            if (_progressFill != null)
                _progressFill.style.width = new StyleLength(new Length(percent, LengthUnit.Percent));

            if (_percentageLabel != null)
                _percentageLabel.text = $"{Mathf.RoundToInt(percent)}%";
        }
    }
}