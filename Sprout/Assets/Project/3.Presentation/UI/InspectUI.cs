using System;
using UnityEngine;
using UnityEngine.UIElements;
using ThresholdGame.Core.Interaction;

namespace ThresholdGame.Presentation.UI
{
    /// <summary>
    /// Panel de inspección global, ahora en UI Toolkit (UIDocument + InspectUI.uxml).
    /// Mantiene la MISMA API pública (Open/Close) que la versión uGUI, así que
    /// ObjectInteractionTrigger y PlayerInspectState siguen funcionando igual.
    ///
    /// Configura el GameObject con: Tools ▸ Sprout ▸ Setup InspectUI (UI Toolkit),
    /// que le pone un UIDocument con InspectUI.uxml + el PanelSettings del proyecto.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class InspectUI : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;

        private VisualElement _root;
        private Label _title;
        private Label _description;
        private VisualElement _image;
        private Button _close;

        private Action _onClose;
        private bool _isOpen;

        private void Awake()
        {
            if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable() => Query();

        private void OnDisable()
        {
            if (_close != null) _close.clicked -= Close;
        }

        private void Query()
        {
            var root = uiDocument != null ? uiDocument.rootVisualElement : null;
            if (root == null) return;

            _root = root.Q<VisualElement>("inspect-root") ?? root;
            _title = root.Q<Label>("inspect-title");
            _description = root.Q<Label>("inspect-description");
            _image = root.Q<VisualElement>("inspect-image");

            var close = root.Q<Button>("inspect-close");
            if (close != null && close != _close)
            {
                if (_close != null) _close.clicked -= Close;
                _close = close;
                _close.clicked += Close;
            }

            SetVisible(false);
        }

        /// <summary>Abre el panel con los datos del objeto.</summary>
        public void Open(InspectableObjectSO data, Action onClose = null)
        {
            if (data == null) return;
            if (_root == null) Query();

            _onClose = onClose;

            if (_title != null) _title.text = data.title;
            if (_description != null) _description.text = data.description;

            if (_image != null)
            {
                bool has = data.image != null;
                _image.style.display = has ? DisplayStyle.Flex : DisplayStyle.None;
                if (has) _image.style.backgroundImage = new StyleBackground(data.image);
            }

            SetVisible(true);
            _isOpen = true;
        }

        /// <summary>Cierra el panel y restaura el estado del jugador vía callback.</summary>
        public void Close()
        {
            if (!_isOpen) return;
            _isOpen = false;
            SetVisible(false);
            _onClose?.Invoke();
            _onClose = null;
        }

        private void SetVisible(bool on)
        {
            if (_root != null) _root.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
