using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sprout.Data;
using Sprout.Application;

namespace Sprout.Presentation.UI
{
    /// <summary>
    /// Displays the resolved ending. Subscribes to EndingService.onEndingResolved.
    /// </summary>
    public class EndingScreenUI : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private Image background;
        [SerializeField] private Button quitButton;
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        [Header("Auto-subscribe source (optional)")]
        [SerializeField] private EndingService endingService;

        private void Awake()
        {
            if (panel != null) panel.SetActive(false);
            if (quitButton != null) quitButton.onClick.AddListener(ToMenu);
            if (endingService != null) endingService.onEndingResolved.AddListener(Show);
        }

        public void Show(EndingDefinitionSO ending)
        {
            if (panel != null) panel.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (ending == null)
            {
                if (titleText) titleText.text = "Fin";
                if (bodyText) bodyText.text = "Tu historia en el pueblo llega a su fin.";
                return;
            }
            if (titleText) titleText.text = ending.title;
            if (bodyText) bodyText.text = ending.epilogueText;
            if (background != null)
            {
                background.color = ending.tint;
                if (ending.backgroundImage != null) background.sprite = ending.backgroundImage;
            }
        }

        private void ToMenu()
        {
            // Respect the existing GameManager (HSFM) if present; fall back to a
            // direct scene load only if there's no state machine in the build.
            var gsm = ThresholdGame.Core.GameFlow.GameStateMachine.Instance;
            if (gsm != null) { gsm.GoToMainMenu(); return; }
            if (!string.IsNullOrEmpty(mainMenuSceneName))
                UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}
