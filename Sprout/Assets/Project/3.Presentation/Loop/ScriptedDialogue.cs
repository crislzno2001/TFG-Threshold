using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Sprout.Presentation
{
    /// <summary>
    /// Diálogo GUIONIZADO (sin que el jugador escriba): muestra una secuencia de frases en un panel abajo
    /// y avanzas con clic / Enter / Espacio. Sirve para la intro del coche (el conductor habla), la nota
    /// de la abuela y los tutoriales. Bloquea el control del jugador mientras dura y, al acabar, lanza
    /// 'onFinished' (para encadenar la siguiente cosa: empezar el tutorial, dar el objetivo, etc.).
    /// </summary>
    public sealed class ScriptedDialogue : MonoBehaviour
    {
        [Serializable]
        public class Line
        {
            [Tooltip("Quién habla (sale arriba en rosa). Puede ir vacío.")]
            public string speaker;
            [TextArea(2, 4)] public string text;
        }

        [SerializeField] private List<Line> lines = new();
        [SerializeField] private bool playOnStart = false;
        [SerializeField] private float charsPerSecond = 40f;
        [Tooltip("Bloquear el movimiento del jugador mientras se reproduce.")]
        [SerializeField] private bool lockPlayer = true;
        [SerializeField] private string playerTag = "Player";

        [Tooltip("Se dispara al terminar TODA la secuencia (encadena tutorial, objetivo, etc.).")]
        public UnityEvent onFinished;

        private bool _running, _panelVisible, _wantAdvance;
        private string _speaker = "", _shown = "";
        private GUIStyle _box, _name, _body, _hint;

        private void Start() { if (playOnStart) Play(); }

        /// <summary>Lanza la secuencia (desde un trigger, un botón, al cargar la escena…).</summary>
        public void Play()
        {
            if (_running || lines == null || lines.Count == 0) return;
            StartCoroutine(Run());
        }

        public bool IsPlaying => _running;

        private void Update()
        {
            if (!_running) return;
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            bool pressed =
                (kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)) ||
                (mouse != null && mouse.leftButton.wasPressedThisFrame);
            if (pressed) _wantAdvance = true;
        }

        private IEnumerator Run()
        {
            _running = true;
            _panelVisible = true;
            if (lockPlayer) SetPlayerControl(false);

            float delay = charsPerSecond > 0f ? 1f / charsPerSecond : 0f;

            foreach (var line in lines)
            {
                if (line == null) continue;
                _speaker = line.speaker ?? "";
                string full = line.text ?? "";

                // Typewriter (un clic mientras escribe -> revela todo de golpe).
                _wantAdvance = false;
                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < full.Length; i++)
                {
                    if (_wantAdvance) { _shown = full; _wantAdvance = false; break; }
                    sb.Append(full[i]);
                    _shown = sb.ToString();
                    if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
                }
                _shown = full;

                // Esperar a que avances a la siguiente frase.
                _wantAdvance = false;
                while (!_wantAdvance) yield return null;
                _wantAdvance = false;
            }

            _panelVisible = false;
            _running = false;
            if (lockPlayer) SetPlayerControl(true);
            onFinished?.Invoke();
        }

        private void SetPlayerControl(bool enabled)
        {
            var player = GameObject.FindGameObjectWithTag(playerTag);
            if (player == null) return;
            foreach (var mb in player.GetComponentsInChildren<MonoBehaviour>())
            {
                if (mb == null) continue;
                var m = mb.GetType().GetMethod("SetControlEnabled", new[] { typeof(bool) });
                if (m != null) m.Invoke(mb, new object[] { enabled });
            }
        }

        private void OnGUI()
        {
            if (!_panelVisible) return;
            EnsureStyles();

            float w = Mathf.Min(900f, Screen.width - 120f);
            float h = 180f;
            float x = (Screen.width - w) / 2f;
            float y = Screen.height - h - 40f;

            // panel crema
            var prev = GUI.color;
            GUI.color = new Color(0.10f, 0.09f, 0.08f, 0.35f);
            GUI.DrawTexture(new Rect(x + 6, y + 8, w, h), Texture2D.whiteTexture); // sombra
            GUI.color = new Color(0.98f, 0.95f, 0.88f, 0.98f);
            GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
            GUI.color = prev;

            if (!string.IsNullOrEmpty(_speaker))
                GUI.Label(new Rect(x + 28, y + 16, w - 56, 28), _speaker, _name);
            _body.fontSize = Mathf.RoundToInt(17 * SproutTextScale.Get()); // tamaño de letra de la config
            GUI.Label(new Rect(x + 28, y + 50, w - 56, h - 80), _shown, _body);
            GUI.Label(new Rect(x + 28, y + h - 30, w - 56, 24), "clic / Enter para continuar  ▶", _hint);
        }

        private void EnsureStyles()
        {
            if (_body != null) return;
            _name = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };
            _name.normal.textColor = new Color(0.78f, 0.36f, 0.45f); // rosa
            _body = new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true };
            _body.normal.textColor = new Color(0.27f, 0.21f, 0.17f); // marrón
            _hint = new GUIStyle(GUI.skin.label) { fontSize = 12, alignment = TextAnchor.MiddleRight, fontStyle = FontStyle.Italic };
            _hint.normal.textColor = new Color(0.5f, 0.44f, 0.38f);
            _box = new GUIStyle();
        }
    }
}
