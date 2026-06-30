using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace Sprout.Presentation
{
    /// <summary>
    /// Intro sencilla tipo "cinemática": al empezar la escena, pantalla negra con la CARTA DE LA ABUELA
    /// y un botón para empezar. Da contexto narrativo y sensación de inicio de juego sin coste.
    /// Desactiva el control del jugador durante la intro y lo reactiva al pulsar Empezar.
    ///
    /// Ponlo en un objeto de la escena de juego. Edita el texto en el inspector.
    /// </summary>
    public sealed class IntroLetterUI : MonoBehaviour
    {
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private string title = "Sprout";
        [TextArea(5, 12)]
        [SerializeField] private string letter =
            "Querida nieta:\n\n" +
            "Si estás leyendo esto, la floristería ya es tuya. Cuida de las flores... y de los vecinos.\n\n" +
            "Algunos cargan más de lo que cuentan. Escúchalos. Sabrás qué darles.\n\n" +
            "Con cariño,\nla abuela.";
        [SerializeField] private float fadeDuration = 0.8f;
        [Tooltip("Segundos mínimos que la carta queda en pantalla antes de poder cerrarla (evita cierres accidentales).")]
        [SerializeField] private float minReadTime = 1.5f;

        private CanvasGroup _group;
        private bool _busy;
        private float _shownAt;

        private void Start()
        {
            Build();
            SetPlayerControl(false);
            _group.alpha = 1f;
            _shownAt = Time.unscaledTime;
        }

        private void Update()
        {
            if (_busy) return;
            if (Time.unscaledTime - _shownAt < minReadTime) return; // deja leer la carta un momento
            // A prueba de balas: cerrar la intro con clic o Enter/Espacio, sin depender del EventSystem de uGUI.
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            bool go = (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
                   || (mouse != null && mouse.leftButton.wasPressedThisFrame);
            if (go) OnStartClicked();
        }

        private void OnStartClicked()
        {
            if (_busy) return;
            _busy = true;
            StartCoroutine(StartGame());
        }

        private IEnumerator StartGame()
        {
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                _group.alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
                yield return null;
            }
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            SetPlayerControl(true);
            gameObject.SetActive(false);
        }

        private void SetPlayerControl(bool enabled)
        {
            var player = GameObject.FindGameObjectWithTag(playerTag);
            if (player == null) return;
            foreach (var mb in player.GetComponentsInChildren<MonoBehaviour>())
            {
                var m = mb.GetType().GetMethod("SetControlEnabled");
                if (m != null) m.Invoke(mb, new object[] { enabled });
            }
        }

        private void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9500;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            gameObject.AddComponent<GraphicRaycaster>();
            _group = gameObject.AddComponent<CanvasGroup>();

            // Fondo: escritorio cálido (como una mesa de madera oscura)
            var bg = NewImage("BG", transform, new Color(0.16f, 0.13f, 0.11f, 1f));
            Stretch(bg.rectTransform);

            // Carta de papel (tarjeta crema centrada)
            var paper = NewImage("Paper", transform, new Color(0.97f, 0.95f, 0.88f, 1f));
            Anchor(paper.rectTransform, new Vector2(0.5f, 0.54f), new Vector2(760, 560));

            // Sombra/borde sutil del papel
            var edge = NewImage("PaperEdge", transform, new Color(0f, 0f, 0f, 0.18f));
            Anchor(edge.rectTransform, new Vector2(0.5f, 0.535f), new Vector2(772, 572));
            edge.transform.SetSiblingIndex(paper.transform.GetSiblingIndex());

            // Título (nombre del juego) encima de la carta
            var t = NewText("Title", transform, 46, TextAnchor.MiddleCenter, FontStyle.Bold, new Color(0.34f, 0.49f, 0.36f));
            Anchor(t.rectTransform, new Vector2(0.5f, 0.88f), new Vector2(600, 70));
            t.text = title;

            // Texto de la carta DENTRO del papel, con márgenes, tinta marrón
            var l = NewText("Letter", paper.transform, 27, TextAnchor.UpperLeft, FontStyle.Italic, new Color(0.30f, 0.24f, 0.19f));
            var lrt = l.rectTransform;
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(60, 60); lrt.offsetMax = new Vector2(-60, -60);
            l.lineSpacing = 1.15f;
            l.text = letter;

            var btn = NewButton("Start", transform, "Empezar   ·   clic o Enter");
            Anchor((RectTransform)btn.transform, new Vector2(0.5f, 0.10f), new Vector2(300, 70));
            btn.onClick.AddListener(OnStartClicked);
        }

        // helpers
        private static Font UiFont()
        {
            var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return f;
        }
        private static Image NewImage(string n, Transform p, Color c)
        { var go = new GameObject(n); go.transform.SetParent(p, false); var i = go.AddComponent<Image>(); i.color = c; return i; }
        private static Text NewText(string n, Transform p, int size, TextAnchor a, FontStyle st, Color c)
        {
            var go = new GameObject(n); go.transform.SetParent(p, false); var t = go.AddComponent<Text>();
            t.font = UiFont(); t.fontSize = size; t.alignment = a; t.fontStyle = st; t.color = c;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow; return t;
        }
        private static Button NewButton(string n, Transform p, string label)
        {
            var go = new GameObject(n); go.transform.SetParent(p, false); var img = go.AddComponent<Image>();
            img.color = new Color(0.85f, 0.45f, 0.35f, 1f); var b = go.AddComponent<Button>();
            var txt = NewText("Label", go.transform, 26, TextAnchor.MiddleCenter, FontStyle.Bold, Color.white);
            Stretch(txt.rectTransform); txt.text = label; return b;
        }
        private static void Stretch(RectTransform rt)
        { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; }
        private static void Anchor(RectTransform rt, Vector2 a, Vector2 size)
        { rt.anchorMin = rt.anchorMax = a; rt.pivot = new Vector2(0.5f, 0.5f); rt.anchoredPosition = Vector2.zero; rt.sizeDelta = size; }
    }
}
