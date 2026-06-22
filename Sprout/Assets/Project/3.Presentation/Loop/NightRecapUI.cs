using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Sprout.Presentation
{
    /// <summary>
    /// Pantalla de "recap" nocturno estilo resumen del día. Se construye sola (no hay que montar UI):
    /// un fondo oscuro a pantalla completa con el título "Noche · Día X", las líneas de resumen del
    /// gossip y un botón "Continuar". La usa BedSleepPoint al irse a dormir.
    ///
    /// Flujo: FadeIn() -> ShowContent(...) -> el botón Continuar dispara el callback -> FadeOut().
    /// </summary>
    public sealed class NightRecapUI : MonoBehaviour
    {
        public static NightRecapUI Instance { get; private set; }

        [SerializeField] private float fadeDuration = 0.6f;

        private CanvasGroup _group;
        private Text _title;
        private Text _body;
        private GameObject _content;
        private Action _onContinue;

        public static NightRecapUI GetOrCreate()
        {
            if (Instance != null) return Instance;
            return new GameObject("NightRecapUI").AddComponent<NightRecapUI>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Build();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _content.SetActive(false);
        }

        public IEnumerator FadeIn() { yield return Fade(1f); }
        public IEnumerator FadeOut()
        {
            _content.SetActive(false);
            yield return Fade(0f);
        }

        public void ShowContent(int day, List<string> lines, Action onContinue)
        {
            _onContinue = onContinue;
            _title.text = $"Noche · Día {day}";

            var sb = new StringBuilder();
            if (lines != null)
                foreach (var l in lines)
                    if (!string.IsNullOrWhiteSpace(l)) sb.Append("•  ").AppendLine(l).AppendLine();
            if (sb.Length == 0) sb.Append("El pueblo duerme. Esta noche nada se mueve.");
            _body.text = sb.ToString();

            _content.SetActive(true);
        }

        private void OnContinueClicked()
        {
            _content.SetActive(false);
            var cb = _onContinue; _onContinue = null;
            cb?.Invoke();
        }

        private IEnumerator Fade(float target)
        {
            _group.blocksRaycasts = true;
            float start = _group.alpha, t = 0f;
            while (t < fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                _group.alpha = Mathf.Lerp(start, target, t / fadeDuration);
                yield return null;
            }
            _group.alpha = target;
            _group.blocksRaycasts = target > 0.5f;
        }

        // ── construcción de la UI ─────────────────────────────────────────────
        private void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9000;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            gameObject.AddComponent<GraphicRaycaster>();
            _group = gameObject.AddComponent<CanvasGroup>();

            var bg = NewImage("BG", transform, new Color(0.03f, 0.03f, 0.06f, 0.96f));
            Stretch(bg.rectTransform);

            _content = new GameObject("Content");
            _content.transform.SetParent(transform, false);
            Stretch(_content.AddComponent<RectTransform>());

            _title = NewText("Title", _content.transform, 46, TextAnchor.MiddleCenter, FontStyle.Bold,
                new Color(1f, 0.93f, 0.8f));
            Anchor(_title.rectTransform, new Vector2(0.5f, 0.80f), new Vector2(1000, 90));

            _body = NewText("Body", _content.transform, 28, TextAnchor.UpperCenter, FontStyle.Normal, Color.white);
            Anchor(_body.rectTransform, new Vector2(0.5f, 0.50f), new Vector2(1000, 460));

            var btn = NewButton("Continue", _content.transform, "Continuar  ▸");
            Anchor((RectTransform)btn.transform, new Vector2(0.5f, 0.14f), new Vector2(300, 70));
            btn.onClick.AddListener(OnContinueClicked);
        }

        private static Font UiFont()
        {
            var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return f;
        }

        private static Image NewImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        private static Text NewText(string name, Transform parent, int size, TextAnchor anchor, FontStyle style, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = UiFont();
            t.fontSize = size;
            t.alignment = anchor;
            t.fontStyle = style;
            t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        private static Button NewButton(string name, Transform parent, string label)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.85f, 0.45f, 0.35f, 1f);
            var btn = go.AddComponent<Button>();
            var txt = NewText("Label", go.transform, 26, TextAnchor.MiddleCenter, FontStyle.Bold, Color.white);
            Stretch(txt.rectTransform);
            txt.text = label;
            return btn;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        private static void Anchor(RectTransform rt, Vector2 anchor, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;
        }
    }
}
