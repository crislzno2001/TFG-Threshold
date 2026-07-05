using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Sprout.Presentation
{
    /// <summary>
    /// Diálogo GUIONIZADO (sin IA): muestra una secuencia de frases en un panel abajo y avanzas con
    /// clic / Enter / Espacio. Ideal para la INTRO del coche (el conductor habla y tú eliges respuestas),
    /// la nota de la abuela y los tutoriales.
    ///
    /// Soporta OPCIONES del jugador: una frase con 'choices' muestra botones; al elegir uno, salta a la
    /// frase con ese 'goTo' (o sigue a la siguiente si está vacío) y puede activar un flag. Si una frase
    /// no tiene choices, es lineal (avanzas con clic) — compatible con los usos que ya tenías.
    ///
    /// Bloquea el control del jugador mientras dura y, al acabar, lanza 'onFinished'.
    /// </summary>
    public sealed class ScriptedDialogue : MonoBehaviour
    {
        [Serializable]
        public class Choice
        {
            [Tooltip("Lo que elige/responde el jugador (texto del botón).")]
            public string text;
            [Tooltip("Id de la frase a la que salta al elegir esta opción. Vacío = sigue a la siguiente.")]
            public string goTo;
            [Tooltip("Opcional: flag que se activa al elegir esta opción (p. ej. 'jugadora_timida').")]
            public string setFlag;
        }

        [Serializable]
        public class Line
        {
            [Tooltip("Id opcional, para poder SALTAR aquí desde una opción.")]
            public string id;
            [Tooltip("Quién habla (sale arriba). Puede ir vacío.")]
            public string speaker;
            [TextArea(2, 4)] public string text;
            [Tooltip("Opciones del jugador tras esta frase. Vacío = frase normal (avanzas con clic).")]
            public List<Choice> choices = new();
        }

        [SerializeField] private List<Line> lines = new();
        [SerializeField] private bool playOnStart = false;
        [SerializeField] private float charsPerSecond = 40f;
        [Tooltip("Bloquear el movimiento del jugador mientras se reproduce.")]
        [SerializeField] private bool lockPlayer = true;
        [SerializeField] private string playerTag = "Player";

        [Header("Cuadro de texto")]
        [SerializeField] private int fontSize = 20;
        [SerializeField] private float boxHeight = 190f;

        [Tooltip("Se dispara al terminar TODA la secuencia (encadena tutorial, tour, objetivo, etc.).")]
        public UnityEvent onFinished;

        /// <summary>True mientras haya CUALQUIER diálogo guionizado en marcha (lo lee el SproutCursorGuard
        /// para mantener el cursor libre y poder clicar las opciones).</summary>
        public static bool AnyActive { get; private set; }

        private bool _running, _panelVisible, _wantAdvance;
        private string _speaker = "", _shown = "";
        private List<Choice> _choices;     // opciones activas ahora mismo (o null)
        private int _pickedIndex = -1;
        private GUIStyle _name, _body, _hint, _panel, _choiceLabel;

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
            if (!_running || _choices != null) return; // en una elección, el clic NO avanza (solo los botones)
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
            AnyActive = true;
            if (lockPlayer) SetPlayerControl(false);

            // Libera el cursor para poder CLICAR las opciones (en gameplay está bloqueado/oculto).
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            float delay = charsPerSecond > 0f ? 1f / charsPerSecond : 0f;

            int i = 0;
            while (i >= 0 && i < lines.Count)
            {
                var line = lines[i];
                if (line == null) { i++; continue; }

                _speaker = line.speaker ?? "";
                string full = line.text ?? "";

                // Typewriter (un clic mientras escribe -> revela todo de golpe).
                _wantAdvance = false;
                var sb = new System.Text.StringBuilder();
                for (int c = 0; c < full.Length; c++)
                {
                    if (_wantAdvance) { _shown = full; _wantAdvance = false; break; }
                    sb.Append(full[c]);
                    _shown = sb.ToString();
                    if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
                }
                _shown = full;

                bool hasChoices = line.choices != null && line.choices.Count > 0;
                if (hasChoices)
                {
                    // Muestra las opciones y espera a que la jugadora elija una.
                    _choices = line.choices;
                    _pickedIndex = -1;
                    while (_pickedIndex < 0) yield return null;

                    var picked = _choices[Mathf.Clamp(_pickedIndex, 0, _choices.Count - 1)];
                    _choices = null;
                    _pickedIndex = -1;

                    if (!string.IsNullOrWhiteSpace(picked.setFlag))
                    {
                        var d = Sprout.Application.SproutGameDirector.Instance;
                        d?.Flags?.SetFlag(picked.setFlag.Trim(), true);
                    }

                    i = string.IsNullOrWhiteSpace(picked.goTo) ? i + 1 : IndexOfId(picked.goTo);
                    if (i < 0) break; // goTo inválido → termina
                }
                else
                {
                    // Frase normal: esperar a que avances.
                    _wantAdvance = false;
                    while (!_wantAdvance) yield return null;
                    _wantAdvance = false;
                    i++;
                }
            }

            _panelVisible = false;
            _choices = null;
            _running = false;
            AnyActive = false;
            if (lockPlayer) SetPlayerControl(true);

            // Vuelve a bloquear el cursor para el gameplay.
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            onFinished?.Invoke();
        }

        private int IndexOfId(string id)
        {
            for (int k = 0; k < lines.Count; k++)
                if (lines[k] != null && lines[k].id == id) return k;
            Debug.LogWarning($"[ScriptedDialogue] No hay ninguna frase con id '{id}'.");
            return -1;
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
            float h = boxHeight;
            float x = (Screen.width - w) / 2f;
            float y = Screen.height - h - 40f;

            // Panel crema redondeado (estilo menú).
            var prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.22f);
            GUI.Box(new Rect(x + 6, y + 8, w, h), GUIContent.none, _panel);
            GUI.color = SproutPalette.Cream;
            GUI.Box(new Rect(x, y, w, h), GUIContent.none, _panel);
            GUI.color = prev;

            if (!string.IsNullOrEmpty(_speaker))
                GUI.Label(new Rect(x + 28, y + 16, w - 56, 30), _speaker, _name);

            _body.fontSize = Mathf.RoundToInt(fontSize * SproutTextScale.Get());
            bool hasChoices = _choices != null && _choices.Count > 0;

            if (hasChoices)
            {
                // Fila de opciones (cuadraditos) DEBAJO del texto de Paco, todos en la misma fila.
                int n = _choices.Count;
                float gap = 10f, rowH = 52f;
                float rowY = y + h - rowH - 16f;
                float bwEach = (w - 56f - (n - 1) * gap) / n;

                GUI.Label(new Rect(x + 28, y + 52, w - 56, rowY - (y + 58)), _shown, _body);

                for (int k = 0; k < n; k++)
                {
                    var r = new Rect(x + 28 + k * (bwEach + gap), rowY, bwEach, rowH);
                    bool hover = r.Contains(Event.current.mousePosition);
                    GUI.color = hover ? new Color(0.62f, 0.80f, 0.58f) : new Color(0.82f, 0.90f, 0.78f); // verde salvia claro
                    GUI.Box(r, GUIContent.none, _panel);
                    GUI.color = Color.white;
                    GUI.Label(r, _choices[k].text, _choiceLabel);
                    if (GUI.Button(r, GUIContent.none, GUIStyle.none)) _pickedIndex = k;
                }
            }
            else
            {
                GUI.Label(new Rect(x + 28, y + 52, w - 56, h - 84), _shown, _body);
                GUI.Label(new Rect(x + 28, y + h - 30, w - 56, 24), "clic / Enter para continuar  ▶", _hint);
            }
        }

        private void EnsureStyles()
        {
            if (_body != null) return;
            _name = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };
            _name.normal.textColor = new Color(0.78f, 0.36f, 0.45f); // rosa
            _body = new GUIStyle(GUI.skin.label) { fontSize = fontSize, wordWrap = true };
            _body.normal.textColor = SproutPalette.TextDark;
            _hint = new GUIStyle(GUI.skin.label) { fontSize = 12, alignment = TextAnchor.MiddleRight, fontStyle = FontStyle.Italic };
            _hint.normal.textColor = SproutPalette.GreenText;
            _panel = new GUIStyle { border = new RectOffset(14, 14, 14, 14) };
            _panel.normal.background = SproutPalette.RoundedRect;

            _choiceLabel = new GUIStyle(GUI.skin.label)
            { fontSize = fontSize, alignment = TextAnchor.MiddleCenter, wordWrap = true, fontStyle = FontStyle.Bold };
            _choiceLabel.normal.textColor = SproutPalette.TextDark;
        }
    }
}
