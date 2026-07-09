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
        private Image _resultIcon;
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
            _resultIcon = root.Q<Image>("result-icon");
            if (_resultIcon != null) _resultIcon.style.display = DisplayStyle.None;

            BuildRow(root.Q<VisualElement>("row-a"), _aButtons, k => { _a = k; RefreshHighlights(); Refresh(); });
            BuildRow(root.Q<VisualElement>("row-b"), _bButtons, k => { _b = k; RefreshHighlights(); Refresh(); });

            var craft = root.Q<Button>("craft");
            if (craft != null) { craft.clicked -= Craft; craft.clicked += Craft; }
            var close = root.Q<Button>("close");
            if (close != null) { close.clicked -= Close; close.clicked += Close; }

            _a = _b = FlowerKind.None;
            if (_result != null) _result.text = "";
            if (_resultIcon != null) _resultIcon.style.display = DisplayStyle.None;
            RefreshHighlights();
            Refresh();
        }

        private Sprite IconFor(FlowerKind k) => flowerService != null ? flowerService.DefOf(k)?.icon : null;

        private void BuildRow(VisualElement row, List<Button> store, Action<FlowerKind> onPick)
        {
            if (row == null) return;
            row.Clear();
            store.Clear();
            for (int i = 0; i < Names.Length; i++)
            {
                var k = (FlowerKind)(i + 1);
                var b = new Button(() => onPick(k));
                b.Clear(); // el ctor de Button mete un Label interno para el texto; lo quitamos, ponemos foto + nombre nosotras.

                var icon = new Image
                {
                    sprite = IconFor(k),
                    scaleMode = ScaleMode.ScaleToFit
                };
                icon.style.width = 88;
                icon.style.height = 88;
                icon.style.alignSelf = Align.Center;
                b.Add(icon);

                var label = new Label(Names[i]) { style = { fontSize = 15, unityTextAlign = TextAnchor.MiddleCenter, marginTop = 4 } };
                b.Add(label);

                Style(b, BtnState.Normal);
                row.Add(b);
                store.Add(b);
            }
        }

        private enum BtnState { Normal, Selected, Possible }

        private static void Style(Button b, BtnState state)
        {
            b.style.width = 130; b.style.height = 130;
            b.style.marginLeft = 6; b.style.marginRight = 6; b.style.marginTop = 6; b.style.marginBottom = 6;
            b.style.justifyContent = Justify.Center;
            b.style.alignItems = Align.Center;
            b.style.fontSize = 14;
            float r = 12;
            b.style.borderTopLeftRadius = r; b.style.borderTopRightRadius = r;
            b.style.borderBottomLeftRadius = r; b.style.borderBottomRightRadius = r;

            // Borde verde para las flores que SÍ forman un ramo con la otra seleccionada.
            float bw = state == BtnState.Possible ? 3f : 0f;
            b.style.borderLeftWidth = bw; b.style.borderRightWidth = bw; b.style.borderTopWidth = bw; b.style.borderBottomWidth = bw;
            var green = new Color(0.36f, 0.62f, 0.42f);
            b.style.borderLeftColor = green; b.style.borderRightColor = green; b.style.borderTopColor = green; b.style.borderBottomColor = green;

            switch (state)
            {
                case BtnState.Selected:
                    b.style.backgroundColor = new Color(0.84f, 0.55f, 0.62f);
                    b.style.color = Color.white;
                    break;
                case BtnState.Possible:
                    b.style.backgroundColor = new Color(0.80f, 0.91f, 0.78f);
                    b.style.color = new Color(0.18f, 0.30f, 0.20f);
                    break;
                default:
                    b.style.backgroundColor = new Color(0.96f, 0.86f, 0.72f);
                    b.style.color = new Color(0.23f, 0.18f, 0.16f);
                    break;
            }
        }

        /// <summary>Re-pinta ambas filas: la flor elegida se marca, y en la fila contraria se resaltan
        /// (borde verde) las que formarían un ramo con ella.</summary>
        private void RefreshHighlights()
        {
            for (int i = 0; i < _aButtons.Count; i++)
                Style(_aButtons[i], StateFor((FlowerKind)(i + 1), _a, _b));
            for (int i = 0; i < _bButtons.Count; i++)
                Style(_bButtons[i], StateFor((FlowerKind)(i + 1), _b, _a));
        }

        private static BtnState StateFor(FlowerKind k, FlowerKind mine, FlowerKind other)
        {
            if (k == mine && mine != FlowerKind.None) return BtnState.Selected;
            if (other != FlowerKind.None && BouquetResolver.IsValidCombination(k, other)) return BtnState.Possible;
            return BtnState.Normal;
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

            if (_resultIcon != null)
            {
                var sprite = result != BouquetKind.None ? flowerService.DefOf(result)?.icon : null;
                if (sprite != null)
                {
                    _resultIcon.sprite = sprite;
                    _resultIcon.style.display = DisplayStyle.Flex;
                }
                else
                {
                    _resultIcon.style.display = DisplayStyle.None;
                }
            }
            Refresh();
        }

        private void Close() => gameObject.SetActive(false);
    }
}
