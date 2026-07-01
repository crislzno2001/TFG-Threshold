using UnityEngine;

namespace Sprout.Presentation
{
    /// <summary>
    /// Brújula / marcador de objetivo. Pon un solo ObjectiveCompass en la escena (en el hub) y llama a
    /// SetTarget(transform, "Floristería") para que aparezca un marcador flotando sobre el destino; si el
    /// destino queda fuera de pantalla, sale una flecha en el borde apuntando hacia él. Llama a Clear()
    /// cuando se cumple el objetivo. Guía al jugador por el pueblo (floristería, NPCs, etc.).
    /// </summary>
    public sealed class ObjectiveCompass : MonoBehaviour
    {
        public static ObjectiveCompass Instance { get; private set; }

        [SerializeField] private Camera cam;
        [SerializeField] private Transform target;
        [SerializeField] private string label = "Objetivo";
        [SerializeField] private float worldYOffset = 2.4f;
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private bool showDistance = true;

        private Transform _player;
        private GUIStyle _pill, _arrow;
        private Texture2D _bg;

        private void Awake() { Instance = this; }
        private void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>Apunta a un nuevo objetivo. label corto (sale en el cartelito).</summary>
        public void SetTarget(Transform t, string lbl) { target = t; label = lbl; }
        public void Clear() { target = null; }
        public bool HasTarget => target != null;

        private void OnGUI()
        {
            if (target == null) return;
            if (cam == null) cam = Camera.main;
            if (cam == null) return;
            EnsureStyles();

            Vector3 sp = cam.WorldToScreenPoint(target.position + Vector3.up * worldYOffset);
            bool behind = sp.z < 0f;
            float gx = sp.x;
            float gy = Screen.height - sp.y; // GUI: Y hacia abajo

            if (behind) { gx = Screen.width - gx; gy = Screen.height - gy; }

            float margin = 60f;
            bool offScreen = behind || gx < margin || gx > Screen.width - margin || gy < margin || gy > Screen.height - margin;

            string dist = "";
            if (showDistance)
            {
                if (_player == null) { var p = GameObject.FindGameObjectWithTag(playerTag); if (p) _player = p.transform; }
                if (_player != null) dist = "  " + Mathf.RoundToInt(Vector3.Distance(_player.position, target.position)) + "m";
            }

            if (!offScreen)
            {
                // Marcador flotando sobre el objetivo (con un bobeo suave).
                float bob = Mathf.Sin(Time.unscaledTime * 3f) * 6f;
                DrawPill(gx, gy + bob, "▼ " + label + dist);
            }
            else
            {
                // Flecha en el borde apuntando hacia el objetivo.
                float cx = Screen.width / 2f, cy = Screen.height / 2f;
                gx = Mathf.Clamp(gx, margin, Screen.width - margin);
                gy = Mathf.Clamp(gy, margin, Screen.height - margin);
                float ang = Mathf.Atan2(gy - cy, gx - cx) * Mathf.Rad2Deg;
                DrawPill(gx, gy, ArrowFor(ang) + " " + label + dist);
            }
        }

        private static string ArrowFor(float angleDeg)
        {
            // angleDeg: 0 = derecha, 90 = abajo (GUI). Mapeo a 8 flechas.
            float a = (angleDeg + 360f) % 360f;
            if (a < 22.5f || a >= 337.5f) return "►";
            if (a < 67.5f) return "↘";
            if (a < 112.5f) return "▼";
            if (a < 157.5f) return "↙";
            if (a < 202.5f) return "◄";
            if (a < 247.5f) return "↖";
            if (a < 292.5f) return "▲";
            return "↗";
        }

        private void DrawPill(float cx, float cy, string text)
        {
            var size = _pill.CalcSize(new GUIContent(text));
            float w = size.x + 24f, h = 30f;
            var r = new Rect(cx - w / 2f, cy - h / 2f, w, h);
            var prev = GUI.color;
            GUI.color = new Color(0.20f, 0.30f, 0.22f, 0.92f); // verde oscuro cozy
            GUI.DrawTexture(r, _bg);
            GUI.color = prev;
            GUI.Label(r, text, _pill);
        }

        private void EnsureStyles()
        {
            if (_pill != null) return;
            _pill = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 15, fontStyle = FontStyle.Bold };
            _pill.normal.textColor = new Color(0.98f, 0.96f, 0.90f);
            _bg = Texture2D.whiteTexture;
        }
    }
}
