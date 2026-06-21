using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Sprout.Application;
using Sprout.Domain.Flowers;

namespace Sprout.Presentation.UI
{
    /// <summary>
    /// Estación de crear ramos, en UI Toolkit (UIDocument + BouquetCrafting.uxml).
    /// El jugador elige dos flores y crea un ramo. El layout (flexbox) coloca todo
    /// centrado y ordenado solo, sin pelear con anclas como en uGUI.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class BouquetCraftingUI : MonoBehaviour
    {
        [SerializeField] private FlowerService flowerService;
        [SerializeField] private UIDocument doc;

        private static readonly string[] Names = { "Acuariana", "Brasa", "Velada", "Sol", "Inquieta", "Crisálida", "Ánima" };

        private Label _selection, _result;
        private readonly List<Button> _aButtons = new();
        private readonly List<Button> _bButtons = new();
        private FlowerKind _a = FlowerKind.None, _b = FlowerKind.None;

        private void Awake()
        {
            if (doc == null) doc = GetComponent<UIDocument>();
        }

        private void OnEnable() => Build();

        private void Build()
        {
            var root = doc != null ? doc.rootVisualElement : null;
            if (root == null) return;

            _selection = root.Q<Label>("selection");
            _result = root.Q<Label>("result");

            BuildRow(root.Q<VisualElement>("row-a"), _aButtons, k => { _a = k; Highlight(_aButtons, k); Refresh(); });
            BuildRow(root.Q<VisualElement>("row-b"), _bButtons, k => { _b = k; Highlight(_bButtons, k); Refresh(); });

            var craft = root.Q<Button>("craft");
            if (craft != null) { craft.clicked -= Craft; craft.clicked += Craft; }
            var close = root.Q<Button>("close");
            if (close != null) { close.clicked -= Close; close.clicked += Close; }

            _a = _b = FlowerKind.None;
            if (_result != null) _result.text = "";
            Highlight(_aButtons, FlowerKind.None);
            Highlight(_bButtons, FlowerKind.None);
            Refresh();
        }

        private void BuildRow(VisualElement row, List<Button> store, Action<FlowerKind> onPick)
        {
            if (row == null) return;
            row.Clear();
            store.Clear();
            for (int i = 0; i < Names.Length; i++)
            {
                var k = (FlowerKind)(i + 1);
                var b = new Button(() => onPick(k)) { text = Names[i] };
                Style(b, false);
                row.Add(b);
                store.Add(b);
            }
        }

        private static void Style(Button b, bool selected)
        {
            b.style.width = 96; b.style.height = 38;
            b.style.marginLeft = 4; b.style.marginRight = 4; b.style.marginTop = 4; b.style.marginBottom = 4;
            b.style.fontSize = 14;
            float r = 12;
            b.style.borderTopLeftRadius = r; b.style.borderTopRightRadius = r;
            b.style.borderBottomLeftRadius = r; b.style.borderBottomRightRadius = r;
            b.style.borderLeftWidth = 0; b.style.borderRightWidth = 0; b.style.borderTopWidth = 0; b.style.borderBottomWidth = 0;
            b.style.backgroundColor = selected ? new Color(0.84f, 0.55f, 0.62f) : new Color(0.96f, 0.86f, 0.72f);
            b.style.color = selected ? Color.white : new Color(0.23f, 0.18f, 0.16f);
        }

        private void Highlight(List<Button> store, FlowerKind selected)
        {
            for (int i = 0; i < store.Count; i++)
                Style(store[i], (FlowerKind)(i + 1) == selected);
        }

        private void Refresh()
        {
            if (_selection == null) return;
            _selection.text = $"Seleccionado: {_a} + {_b}\n" +
                (BouquetResolver.IsValidCombination(_a, _b)
                    ? $"→ {BouquetResolver.Resolve(_a, _b)}"
                    : "→ (no hay receta)");
        }

        private void Craft()
        {
            if (flowerService == null) return;
            var result = flowerService.Craft(_a, _b);
            if (_result != null)
                _result.text = result != BouquetKind.None
                    ? $"¡Has hecho un ramo: {result}!"
                    : "Esas flores no forman un ramo, o no las tienes.";
            Refresh();
        }

        private void Close() => gameObject.SetActive(false);
    }
}
