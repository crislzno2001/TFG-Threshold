using UnityEngine;

namespace Sprout.Presentation.UI
{
    /// <summary>
    /// Al entrar la NOCHE, avisa al jugador de que debe ir a dormir: un mensaje grande unos segundos
    /// y un recordatorio pequeño arriba que se queda hasta que duerme (cambia de fase/día).
    /// Autónomo: se engancha solo al DayCycleService. No hace falta cablear nada.
    /// </summary>
    public sealed class NightSleepNotice : MonoBehaviour
    {
        [SerializeField] private Sprout.Application.DayCycleService dayCycle;

        [TextArea]
        [SerializeField] private string message = "Se hace de noche... deberías ir a dormir.";
        [SerializeField] private string persistentHint = "Es de noche — ve a la cama a descansar";
        [Tooltip("Segundos que dura el mensaje grande al entrar la noche.")]
        [SerializeField] private float toastSeconds = 4f;

        private bool _isNight;
        private float _toast;
        private GUIStyle _toastStyle, _hintStyle;
        private Texture2D _bg;

        private void Start()
        {
            if (dayCycle == null) dayCycle = FindFirstObjectByType<Sprout.Application.DayCycleService>();
            if (dayCycle != null) dayCycle.onPhaseChanged.AddListener(OnPhase);
        }

        private void OnDestroy()
        {
            if (dayCycle != null) dayCycle.onPhaseChanged.RemoveListener(OnPhase);
        }

        private void OnPhase(int day, string phase)
        {
            bool nowNight = phase == "Night";
            if (nowNight && !_isNight) _toast = toastSeconds; // acaba de entrar la noche
            _isNight = nowNight;
        }

        private void Update()
        {
            if (_toast > 0f) _toast -= Time.unscaledDeltaTime;
        }

        private void OnGUI()
        {
            if (!_isNight) return;
            EnsureStyles();

            // Recordatorio pequeño arriba (mientras sea de noche).
            DrawPill(new Rect(Screen.width / 2f - 190f, 22f, 380f, 40f), persistentHint, _hintStyle, 0.72f);

            // Mensaje grande al entrar la noche.
            if (_toast > 0f)
            {
                float a = Mathf.Clamp01(_toast); // se desvanece al final
                DrawPill(new Rect(Screen.width / 2f - 270f, Screen.height * 0.40f, 540f, 78f),
                         message, _toastStyle, 0.82f * a);
            }
        }

        private void DrawPill(Rect r, string text, GUIStyle style, float alpha)
        {
            var prev = GUI.color;
            GUI.color = new Color(0.14f, 0.16f, 0.26f, alpha);   // azul noche translúcido
            GUI.DrawTexture(r, _bg, ScaleMode.StretchToFill, true);
            GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha + 0.15f));
            GUI.Label(r, text, style);
            GUI.color = prev;
        }

        private void EnsureStyles()
        {
            if (_bg == null)
            {
                _bg = new Texture2D(1, 1);
                _bg.SetPixel(0, 0, Color.white);
                _bg.Apply();
            }
            if (_toastStyle == null)
            {
                _toastStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 24, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, wordWrap = true
                };
                _toastStyle.normal.textColor = new Color(0.97f, 0.95f, 0.88f);
            }
            if (_hintStyle == null)
            {
                _hintStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 16, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Italic
                };
                _hintStyle.normal.textColor = new Color(0.90f, 0.92f, 1f);
            }
        }
    }
}
