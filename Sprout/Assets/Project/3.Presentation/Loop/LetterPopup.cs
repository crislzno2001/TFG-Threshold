using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Sprout.Presentation
{
    /// <summary>
    /// Muestra el PAPEL de una carta con su texto (el sobre es un modelo 3D aparte, ver LetterInspector).
    /// El texto se revela poco a poco y se PAGINA: si no cabe en el papel, pasa a la página siguiente con
    /// un clic. En la última página, un clic cierra y lanza 'onFinished'. Respeta el tamaño de letra de la
    /// configuración. Bloquea al jugador mientras está abierta.
    /// </summary>
    public sealed class LetterPopup : MonoBehaviour
    {
        [Header("Papel")]
        [SerializeField] private Texture paper;
        [TextArea(3, 12)] [SerializeField] private string letterText =
            "Mi queridísima florecita:\n\nEsta floristería ahora es tuya.\n\nCon cariño,\nla abuela.";
        [SerializeField] private Color textColor = new Color(0.30f, 0.22f, 0.18f);
        [SerializeField] private int fontSize = 20;
        [Tooltip("Fuente de la carta (arrastra Fredoka-Medium.ttf). Vacío = fuente por defecto.")]
        [SerializeField] private Font font;
        [Tooltip("Velocidad a la que aparecen las letras (caracteres por segundo).")]
        [SerializeField] private float revealSpeed = 45f;

        [Header("Tamaño en pantalla (px)")]
        [SerializeField] private float paperWidth = 520f;
        [SerializeField] private float paperHeight = 640f;
        [Tooltip("Margen del texto dentro del papel en % (izq, arriba, der, abajo).")]
        [SerializeField] private Vector4 textInsetPct = new Vector4(0.16f, 0.20f, 0.16f, 0.16f);

        [Header("Control")]
        [SerializeField] private Key key = Key.A;
        [SerializeField] private bool lockPlayer = true;
        [SerializeField] private string playerTag = "Player";
        [Tooltip("Objeto que se OCULTA al cerrar la carta (arrastra la carta de la mesa para que desaparezca).")]
        [SerializeField] private GameObject hideOnFinish;

        public UnityEvent onFinished;

        private bool _open;
        private float _reveal;
        private List<string> _pages;
        private int _page;
        private GUIStyle _txt, _hint;

        public void Show()
        {
            if (_open) return;
            _open = true;
            _reveal = 0f;
            _page = 0;
            _pages = null; // se pagina en el primer OnGUI (con el tamaño de letra actual)
            if (lockPlayer) SetPlayerControl(false);
        }

        private void Update()
        {
            if (!_open || _pages == null) return; // espera a que OnGUI pagine

            string page = _pages[Mathf.Clamp(_page, 0, _pages.Count - 1)];

            // Mientras salen las letras de la página, ignora el input.
            if (_reveal < page.Length)
            {
                _reveal += Time.unscaledDeltaTime * revealSpeed;
                return;
            }

            var kb = Keyboard.current;
            var mouse = Mouse.current;
            bool pressed = (kb != null && (kb[key].wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)) ||
                           (mouse != null && mouse.leftButton.wasPressedThisFrame);
            if (!pressed) return;

            if (_page < _pages.Count - 1) { _page++; _reveal = 0f; } // siguiente página
            else Close();
        }

        private void Close()
        {
            _open = false;
            if (lockPlayer) SetPlayerControl(true);
            onFinished?.Invoke();
            if (hideOnFinish != null) hideOnFinish.SetActive(false);
        }

        private void OnGUI()
        {
            if (!_open) return;
            EnsureStyles();
            _txt.fontSize = Mathf.RoundToInt(fontSize * SproutTextScale.Get()); // tamaño de letra de la config

            var prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.5f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = prev;

            var r = new Rect(Screen.width / 2f - paperWidth / 2f, Screen.height / 2f - paperHeight / 2f, paperWidth, paperHeight);
            if (paper != null) GUI.DrawTexture(r, paper, ScaleMode.ScaleToFit, true);
            else GUI.Box(r, "");

            var inset = new Rect(
                r.x + paperWidth * textInsetPct.x,
                r.y + paperHeight * textInsetPct.y,
                paperWidth * (1f - textInsetPct.x - textInsetPct.z),
                paperHeight * (1f - textInsetPct.y - textInsetPct.w));

            if (_pages == null) { _pages = Paginate(letterText, inset.width, inset.height); _page = 0; _reveal = 0f; }

            string page = _pages[Mathf.Clamp(_page, 0, _pages.Count - 1)];
            int shown = Mathf.Clamp(Mathf.FloorToInt(_reveal), 0, page.Length);
            GUI.Label(inset, page.Substring(0, shown), _txt);

            string hint = shown < page.Length ? ""
                : (_page < _pages.Count - 1 ? $"Pulsa para continuar  ({_page + 1}/{_pages.Count})  ▶" : "Pulsa para cerrar");
            GUI.Label(new Rect(0, r.yMax + 8, Screen.width, 30), hint, _hint);
        }

        /// <summary>Parte el texto en páginas que caben en el alto disponible del papel.</summary>
        private List<string> Paginate(string text, float width, float height)
        {
            var pages = new List<string>();
            if (string.IsNullOrEmpty(text)) { pages.Add(""); return pages; }

            var words = text.Replace("\r\n", "\n").Split(' ');
            var sb = new StringBuilder();
            foreach (var w in words)
            {
                string trial = sb.Length == 0 ? w : sb.ToString() + " " + w;
                if (sb.Length > 0 && _txt.CalcHeight(new GUIContent(trial), width) > height)
                {
                    pages.Add(sb.ToString());
                    sb.Clear();
                    sb.Append(w);
                }
                else
                {
                    if (sb.Length > 0) sb.Append(' ');
                    sb.Append(w);
                }
            }
            if (sb.Length > 0) pages.Add(sb.ToString());
            if (pages.Count == 0) pages.Add("");
            return pages;
        }

        private void EnsureStyles()
        {
            if (_txt != null) return;
            _txt = new GUIStyle(GUI.skin.label) { fontSize = fontSize, wordWrap = true, alignment = TextAnchor.UpperLeft };
            _txt.normal.textColor = textColor;
            _hint = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Italic };
            _hint.normal.textColor = new Color(0.96f, 0.93f, 0.86f);

            // Fuente cozy (Fredoka) si la has asignado en el inspector.
            if (font != null) { _txt.font = font; _hint.font = font; }
        }

        private void SetPlayerControl(bool enabled)
        {
            var p = GameObject.FindGameObjectWithTag(playerTag);
            if (p == null) return;
            foreach (var mb in p.GetComponentsInChildren<MonoBehaviour>())
            {
                if (mb == null) continue;
                var m = mb.GetType().GetMethod("SetControlEnabled", new[] { typeof(bool) });
                if (m != null) m.Invoke(mb, new object[] { enabled });
            }
        }
    }
}
