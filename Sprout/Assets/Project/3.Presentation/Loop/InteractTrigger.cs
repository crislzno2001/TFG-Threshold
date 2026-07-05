using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

namespace Sprout.Presentation
{
    /// <summary>
    /// Interacción genérica "acércate y pulsa E". Ponlo en la nota/carta, un cartel, un objeto…
    /// Al acercarte, el objeto se RESALTA (flota + palpita) y sale una chispita ✦ con el texto ("pulsa E
    /// para inspeccionar"). Al pulsar E lanza 'onInteract' (p. ej. LetterInspector.Show()).
    /// </summary>
    public sealed class InteractTrigger : MonoBehaviour
    {
        [Header("Interacción")]
        [SerializeField] private string prompt = "inspeccionar";
        [SerializeField] private float radius = 2f;
        [SerializeField] private Key interactKey = Key.E;
        [SerializeField] private string playerTag = "Player";
        [Tooltip("Si está marcado, solo se puede usar una vez.")]
        [SerializeField] private bool once = true;
        [Tooltip("Ocultar el objeto tras interactuar. OJO: si este objeto tiene otros scripts que deben " +
                 "seguir corriendo (LetterInspector), déjalo DESMARCADO y oculta luego desde el evento.")]
        [SerializeField] private bool hideAfter = false;

        [Header("Resaltado al acercarse")]
        [SerializeField] private bool highlight = true;
        [SerializeField] private float bobAmount = 0.08f;
        [SerializeField] private float pulseAmount = 0.06f;
        [SerializeField] private float pulseSpeed = 2.6f;
        [SerializeField] private Camera cam;
        [Tooltip("Altura (en unidades) a la que flota la chispa y el texto sobre el objeto.")]
        [SerializeField] private float markerHeight = 0.6f;
        [Tooltip("Ajuste fino del texto en píxeles (X = derecha, Y = abajo).")]
        [SerializeField] private Vector2 textPixelOffset = new Vector2(0f, 10f);

        public UnityEvent onInteract;

        private Transform _player;
        private bool _near, _used, _hasBase;
        private Vector3 _baseScale, _baseLocalPos;
        private GUIStyle _spark, _promptStyle, _pill;

        private void Start()
        {
            _baseScale = transform.localScale;
            _baseLocalPos = transform.localPosition;
            _hasBase = true;
        }

        private void Update()
        {
            if (_used && once) { RestoreBase(); return; }

            if (_player == null)
            {
                var p = GameObject.FindGameObjectWithTag(playerTag);
                if (p == null) return;
                _player = p.transform;
            }

            _near = (_player.position - transform.position).sqrMagnitude <= radius * radius;

            // Resaltado: flota + palpita al estar cerca.
            if (highlight && _hasBase)
            {
                if (_near)
                {
                    float t = Time.time * pulseSpeed;
                    transform.localScale = _baseScale * (1f + Mathf.Sin(t) * pulseAmount);
                    transform.localPosition = _baseLocalPos + Vector3.up * (Mathf.Abs(Mathf.Sin(t)) * bobAmount);
                }
                else RestoreBase();
            }

            if (!_near) return;

            var kb = Keyboard.current;
            if (kb != null && kb[interactKey].wasPressedThisFrame)
            {
                _used = true;
                RestoreBase();
                onInteract?.Invoke();
                if (hideAfter) gameObject.SetActive(false);
            }
        }

        private void RestoreBase()
        {
            if (!_hasBase) return;
            transform.localScale = _baseScale;
            transform.localPosition = _baseLocalPos;
        }

        private void OnGUI()
        {
            if (!_near || (_used && once)) return;
            EnsureStyles();
            if (cam == null) cam = Camera.main;
            if (cam == null) return;

            Vector3 sp = cam.WorldToScreenPoint(transform.position + Vector3.up * markerHeight);
            if (sp.z <= 0f) return;

            float gx = sp.x;
            float gy = Screen.height - sp.y;
            float bob = Mathf.Sin(Time.unscaledTime * 3f) * 5f;

            var prev = GUI.color;
            var bg = new Color(SproutPalette.Cream.r, SproutPalette.Cream.g, SproutPalette.Cream.b, 0.95f); // pastilla crema (estilo menú)

            // Estrellita con su pastilla redondeada.
            var starRect = new Rect(gx - 22, gy - 40 + bob, 44, 40);
            GUI.color = bg; GUI.Box(starRect, GUIContent.none, _pill); GUI.color = prev;
            GUI.Label(starRect, "✦", _spark);

            // Texto con pastilla redondeada (para que se lea sobre cualquier escena).
            var content = new GUIContent($"{interactKey} · {prompt}");
            Vector2 sz = _promptStyle.CalcSize(content);
            float pw = sz.x + 26f, ph = sz.y + 14f;
            var pr = new Rect(gx - pw / 2f + textPixelOffset.x, gy + textPixelOffset.y, pw, ph);
            GUI.color = bg; GUI.Box(pr, GUIContent.none, _pill); GUI.color = prev;
            GUI.Label(pr, content, _promptStyle);
        }

        private void EnsureStyles()
        {
            if (_spark != null) return;
            _spark = new GUIStyle(GUI.skin.label) { fontSize = 30, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            _spark.normal.textColor = SproutPalette.GreenDark;  // estrellita verde salvia (estilo menú)
            _promptStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            _promptStyle.normal.textColor = SproutPalette.TextDark;
            _pill = new GUIStyle { border = new RectOffset(14, 14, 14, 14) };
            _pill.normal.background = RoundedRect(48, 14, Color.white);
        }

        /// <summary>Textura blanca con esquinas redondeadas (para pastillas de fondo con 9-slice).</summary>
        private static Texture2D RoundedRect(int size, int radius, Color col)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int dx = Mathf.Min(x, size - 1 - x);
                    int dy = Mathf.Min(y, size - 1 - y);
                    float a = 1f;
                    if (dx < radius && dy < radius)
                    {
                        float d = Mathf.Sqrt((radius - dx) * (radius - dx) + (radius - dy) * (radius - dy));
                        a = Mathf.Clamp01(radius - d + 0.5f);
                    }
                    tex.SetPixel(x, y, new Color(col.r, col.g, col.b, a));
                }
            tex.Apply();
            return tex;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.95f, 0.85f, 0.5f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
