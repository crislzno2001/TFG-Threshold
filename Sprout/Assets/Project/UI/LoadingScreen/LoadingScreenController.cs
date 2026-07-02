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

        [Tooltip("Velocidad a la que sube la barra (fracción por segundo). 0.35 = tarda ~3s en llenarse, acorde con la duración del loading.")]
        [SerializeField] private float fillSpeed = 0.35f;

        private VisualElement _root;
        private VisualElement _progressFill;
        private Label _percentageLabel;
        private float _shown;   // valor mostrado, se acerca suavemente al real

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

            // La barra sube SUAVE: en vez del valor real (que salta de 0 a 90 de golpe),
            // acercamos el valor mostrado poco a poco al objetivo.
            float target = Mathf.Clamp01(GameStateMachine.Instance.CurrentLoadProgress);
            _shown = Mathf.MoveTowards(_shown, target, Time.unscaledDeltaTime * fillSpeed);
            SetProgress(_shown);
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
            _shown = 0f;   // cada carga empieza la barra desde 0
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