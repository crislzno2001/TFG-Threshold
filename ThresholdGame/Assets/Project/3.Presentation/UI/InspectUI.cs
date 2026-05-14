using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ThresholdGame.Core.Interaction;

namespace ThresholdGame.Presentation.UI
{
    /// <summary>
    /// Panel de inspección global. Un único objeto en escena reutilizado por todos los objetos.
    /// Se abre con Open() y al cerrar invoca el callback para restaurar el estado del jugador.
    ///
    /// Jerarquía UI recomendada:
    ///   [GameObject "InspectUI"] → este componente
    ///   └─ Panel
    ///      ├─ TitleText        (TMP_Text)
    ///      ├─ DescriptionText  (TMP_Text)
    ///      ├─ ObjectImage      (Image)    ← opcional
    ///      └─ CloseButton      (Button)
    /// </summary>
    public class InspectUI : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private Image objectImage;
        [SerializeField] private Button closeButton;

        private Action _onClose;
        private bool _isOpen;

        private void Awake()
        {
            if (panel != null) panel.SetActive(false);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
        }

        /// <summary>Abre el panel con los datos del objeto.</summary>
        public void Open(InspectableObjectSO data, Action onClose = null)
        {
            if (data == null) return;

            _onClose = onClose;

            if (titleText != null) titleText.text = data.title;
            if (descriptionText != null) descriptionText.text = data.description;

            if (objectImage != null)
            {
                objectImage.gameObject.SetActive(data.image != null);
                if (data.image != null) objectImage.sprite = data.image;
            }

            if (panel != null) panel.SetActive(true);
            _isOpen = true;
        }

        /// <summary>Cierra el panel y notifica al origen para restaurar el estado del jugador.</summary>
        public void Close()
        {
            if (!_isOpen) return;
            _isOpen = false;
            if (panel != null) panel.SetActive(false);
            _onClose?.Invoke();
            _onClose = null;
        }
    }
}