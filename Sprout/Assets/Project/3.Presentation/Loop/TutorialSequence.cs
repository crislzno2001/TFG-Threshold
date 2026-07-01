using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Sprout.Presentation
{
    /// <summary>
    /// Tutorial por pasos: cada paso muestra un texto en un panel abajo y una FLECHA apuntando a un objeto
    /// del mundo (la mesa de crafteo, las estanterías, etc.). Avanzas con clic / Espacio / Enter. Al acabar
    /// lanza 'onFinished'. Llama a Play() (p. ej. desde el onFinished de la carta de la abuela).
    /// </summary>
    public sealed class TutorialSequence : MonoBehaviour
    {
        [Serializable]
        public class Step
        {
            [TextArea(2, 4)] public string text;
            [Tooltip("Objeto al que apunta la flecha (opcional). Vacío = sin flecha.")]
            public Transform pointAt;
            [Tooltip("Altura sobre el objeto a la que apunta la flecha.")]
            public float pointYOffset = 1.2f;
        }

        [SerializeField] private List<Step> steps = new();
        [SerializeField] private bool playOnStart = false;
        [SerializeField] private Camera cam;
        [SerializeField] private bool lockPlayer = true;
        [SerializeField] private string playerTag = "Player";

        [Header("Cuadro de texto")]
        [SerializeField] private float boxWidth = 420f;
        [SerializeField] private float boxHeight = 130f;
        [Tooltip("Si el paso tiene flecha, pone el cuadro de texto ENCIMA de la flecha (junto al objeto). " +
                 "Si lo desmarcas, el cuadro sale abajo del todo.")]
        [SerializeField] private bool boxAbovePointer = true;

        public UnityEvent onFinished;

        private int _i = -1;
        private bool _active;
        private int _playFrame = -1;
        private GUIStyle _body, _hint, _arrow;

        private void Start() { if (playOnStart) Play(); }

        /// <summary>Lanza el tutorial desde el primer paso.</summary>
        public void Play()
        {
            if (_active || steps == null || steps.Count == 0) return;
            _active = true;
            _i = 0;
            _playFrame = Time.frameCount; // para ignorar el clic que lanzó el tutorial
            if (lockPlayer) SetPlayerControl(false);
        }

        private void Update()
        {
            if (!_active) return;
            if (Time.frameCount == _playFrame) return; // ignora el clic del mismo frame en que empezó
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            bool next =
                (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)) ||
                (mouse != null && mouse.leftButton.wasPressedThisFrame);
            if (!next) return;

            _i++;
            if (_i >= steps.Count) Finish();
        }

        private void Finish()
        {
            _active = false;
            _i = -1;
            if (lockPlayer) SetPlayerControl(true);
            onFinished?.Invoke();
        }

        private void OnGUI()
        {
            if (!_active || _i < 0 || _i >= steps.Count) return;
            EnsureStyles();
            var step = steps[_i];

            // Posición de la flecha (sobre el objeto del paso).
            bool haveTarget = false;
            Vector2 arrow = default;
            if (step.pointAt != null)
            {
                if (cam == null) cam = Camera.main;
                if (cam != null)
                {
                    Vector3 sp = cam.WorldToScreenPoint(step.pointAt.position + Vector3.up * step.pointYOffset);
                    if (sp.z > 0f) { arrow = new Vector2(sp.x, Screen.height - sp.y); haveTarget = true; }
                }
            }

            if (haveTarget)
            {
                float bob = Mathf.Sin(Time.unscaledTime * 4f) * 8f;
                GUI.Label(new Rect(arrow.x - 50, arrow.y - 24 + bob, 100, 50), "▼", _arrow);
            }

            // Cuadro de texto: encima de la flecha (junto al objeto) o abajo si no hay flecha.
            float w = boxWidth, h = boxHeight;
            float x, y;
            if (haveTarget && boxAbovePointer)
            {
                x = Mathf.Clamp(arrow.x - w / 2f, 10f, Screen.width - w - 10f);
                y = Mathf.Clamp(arrow.y - 64f - h, 10f, Screen.height - h - 10f);
            }
            else
            {
                x = (Screen.width - w) / 2f;
                y = Screen.height - h - 40f;
            }

            var prev = GUI.color;
            GUI.color = new Color(0.10f, 0.09f, 0.08f, 0.35f);
            GUI.DrawTexture(new Rect(x + 5, y + 6, w, h), Texture2D.whiteTexture);
            GUI.color = new Color(0.98f, 0.95f, 0.88f, 0.98f);
            GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
            GUI.color = prev;

            _body.fontSize = Mathf.RoundToInt(18 * SproutTextScale.Get()); // tamaño de letra de la config
            GUI.Label(new Rect(x + 20, y + 16, w - 40, h - 46), step.text, _body);
            GUI.Label(new Rect(x + 20, y + h - 26, w - 40, 22),
                $"Paso {_i + 1}/{steps.Count}  ·  clic / Espacio  ▶", _hint);
        }

        private void EnsureStyles()
        {
            if (_body != null) return;
            _body = new GUIStyle(GUI.skin.label) { fontSize = 18, wordWrap = true, alignment = TextAnchor.UpperLeft };
            _body.normal.textColor = new Color(0.27f, 0.21f, 0.17f);
            _hint = new GUIStyle(GUI.skin.label) { fontSize = 13, alignment = TextAnchor.MiddleRight, fontStyle = FontStyle.Italic };
            _hint.normal.textColor = new Color(0.5f, 0.44f, 0.38f);
            _arrow = new GUIStyle(GUI.skin.label) { fontSize = 44, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            _arrow.normal.textColor = new Color(0.84f, 0.36f, 0.45f); // rosa cozy
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
