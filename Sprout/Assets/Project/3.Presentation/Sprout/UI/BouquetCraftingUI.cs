using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sprout.Application;
using Sprout.Domain.Flowers;

namespace Sprout.Presentation.UI
{
    /// <summary>
    /// Bouquet crafting station UI. The player chooses two flowers and crafts;
    /// the result is added to the inventory and previewed. Selection is driven by
    /// buttons that call SelectA/SelectB with a FlowerKind index, keeping it simple
    /// to wire without a custom item-grid framework.
    /// </summary>
    public class BouquetCraftingUI : MonoBehaviour
    {
        [SerializeField] private FlowerService flowerService;
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text selectionText;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private Button craftButton;
        [SerializeField] private Button closeButton;

        private FlowerKind _a = FlowerKind.None;
        private FlowerKind _b = FlowerKind.None;

        private void Awake()
        {
            if (craftButton != null) craftButton.onClick.AddListener(Craft);
            if (closeButton != null) closeButton.onClick.AddListener(() => Show(false));
            if (panel != null) panel.SetActive(false);
        }


        public void Show(bool on)
        {
            if (panel != null) panel.SetActive(on);
            if (on) { _a = _b = FlowerKind.None; Refresh(); if (resultText) resultText.text = ""; }
        }

        // Button hooks: pass the FlowerKind enum value (1..7).
        public void SelectA(int kind) { _a = (FlowerKind)kind; Refresh(); }
        public void SelectB(int kind) { _b = (FlowerKind)kind; Refresh(); }

        private void Refresh()
        {
            if (selectionText != null)
                selectionText.text = $"Selected: {_a} + {_b}\n" +
                    (BouquetResolver.IsValidCombination(_a, _b)
                        ? $"→ {BouquetResolver.Resolve(_a, _b)}"
                        : "→ (no recipe)");
        }

        private void Craft()
        {
            if (flowerService == null) return;
            var result = flowerService.Craft(_a, _b);
            if (resultText != null)
                resultText.text = result != BouquetKind.None
                    ? $"You arranged a {result}!"
                    : "Those flowers don't make a bouquet, or you don't have them.";
            Refresh();
        }
    }
}
