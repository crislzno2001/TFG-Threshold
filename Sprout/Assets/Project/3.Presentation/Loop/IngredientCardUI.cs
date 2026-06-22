using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Sprout.Presentation
{
    /// <summary>
    /// Tarjeta de "Receta de hoy" de Mochi: aparece una carta en pantalla con el ingrediente del día.
    /// Se construye sola. Llámala con IngredientCardUI.GetOrCreate().Show("Receta de hoy", "Champiñón lunar").
    /// Se oculta sola tras unos segundos, o al hacer clic en ella.
    /// </summary>
    public sealed class IngredientCardUI : MonoBehaviour
    {
        public static IngredientCardUI Instance { get; private set; }

        [SerializeField] private float fadeDuration = 0.35f;
        [SerializeField] private float autoHideSeconds = 6f;

        private CanvasGroup _group;
        private Text _title;
        private Text _ingredient;
        private Coroutine _routine;

        public static IngredientCardUI GetOrCreate()
        {
            if (Instance != null) return Instance;
            return new GameObject("IngredientCardUI").AddComponent<IngredientCardUI>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Build();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
        }

        public void Show(string title, string ingredient)
        {
            _title.text = string.IsNullOrEmpty(title) ? "Receta de hoy" : title;
            _ingredient.text = ingredient;
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(ShowRoutine());
        }

        public void Hide()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(Fade(0f));
        }

        private IEnumerator ShowRoutine()
        {
            yield return Fade(1f);
            if (autoHideSeconds > 0f)
            {
                yield return new WaitForSeconds(autoHideSeconds);
                yield return Fade(0f);
            }
        }

        private IEnumerator Fade(float target)
        {
            _group.blocksRaycasts = target > 0.5f;
            float start = _group.alpha, t = 0f;
            while (t < fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                _group.alpha = Mathf.Lerp(start, target, t / fadeDuration);
                yield return null;
            }
            _group.alpha = target;
        }

        private void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 8500;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            gameObject.AddComponent<GraphicRaycaster>();
            _group = gameObject.AddComponent<CanvasGroup>();

            // Carta (panel) arriba a la derecha
            var card = new GameObject("Card");
            card.transform.SetParent(transform, false);
            var cardImg = card.AddComponent<Image>();
            cardImg.color = new Color(0.99f, 0.96f, 0.88f, 1f);
            var crt = card.GetComponent<RectTransform>();
            crt.anchorMin = crt.anchorMax = new Vector2(0.82f, 0.80f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(440, 220);

            // Clic en la carta -> ocultar
            card.AddComponent<Button>().onClick.AddListener(Hide);

            // Banda superior
            var band = new GameObject("Band");
            band.transform.SetParent(card.transform, false);
            var bandImg = band.AddComponent<Image>();
            bandImg.color = new Color(0.78f, 0.30f, 0.28f, 1f);
            var brt = band.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0f, 1f); brt.anchorMax = new Vector2(1f, 1f);
            brt.pivot = new Vector2(0.5f, 1f);
            brt.sizeDelta = new Vector2(0, 60); brt.anchoredPosition = Vector2.zero;

            _title = NewText("Title", band.transform, 28, TextAnchor.MiddleCenter, FontStyle.Bold, Color.white);
            Stretch(_title.rectTransform);
            _title.text = "Receta de hoy";

            _ingredient = NewText("Ingredient", card.transform, 34, TextAnchor.MiddleCenter, FontStyle.Bold,
                new Color(0.25f, 0.18f, 0.12f));
            var irt = _ingredient.rectTransform;
            irt.anchorMin = new Vector2(0f, 0f); irt.anchorMax = new Vector2(1f, 1f);
            irt.offsetMin = new Vector2(16, 16); irt.offsetMax = new Vector2(-16, -70);
        }

        private static Font UiFont()
        {
            var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return f;
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

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }
    }
}
