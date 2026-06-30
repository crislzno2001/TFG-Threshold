using System.Collections;
using UnityEngine;
using Sprout.Application;
using Sprout.Domain.Narrative;
using OpenAI.Dialogue;

namespace Sprout.Presentation
{
    /// <summary>
    /// Engancha la cara (FaceExpressionController) al juego, estilo Animal Crossing:
    ///   • PARPADEO automático cada pocos segundos.
    ///   • SONRÍE cuando te acercas a un NPC.
    ///   • Cara TRISTE un rato cuando salta un cotilleo/evento malo (una flag negativa se pone a true).
    ///
    /// Ponlo en el MISMO objeto que tiene la cara (o en el Player que la contiene). Los índices dependen
    /// de TU hoja de expresiones (2x4 = celdas 0..7), así que se configuran en el inspector.
    /// </summary>
    public sealed class FaceGameplayExpressions : MonoBehaviour
    {
        [Header("Cara")]
        [Tooltip("Si lo dejas vacío, lo busca en este objeto o sus hijos.")]
        [SerializeField] private FaceExpressionController face;

        [Header("Índices en tu hoja de EXPRESIONES (0..7)")]
        [SerializeField] private int neutralIndex = 0;
        [SerializeField] private int happyIndex = 1;
        [SerializeField] private int sadIndex = 2;
        [Tooltip("Celda de ojos cerrados para el parpadeo.")]
        [SerializeField] private int blinkIndex = 3;

        [Header("Parpadeo")]
        [Tooltip("Actívalo SOLO si tu hoja tiene una celda de ojos cerrados (apunta blinkIndex a ella).")]
        [SerializeField] private bool enableBlink = false;
        [Tooltip("Espera aleatoria entre parpadeos (min, max) en segundos.")]
        [SerializeField] private Vector2 blinkEverySeconds = new Vector2(3f, 6f);
        [SerializeField] private float blinkDuration = 0.12f;

        [Header("Sonreír cerca de un NPC")]
        [Tooltip("Si está activo, sonríe (Happy Index) al acercarse a un NPC. Desactívalo para quedar neutral.")]
        [SerializeField] private bool smileNearNpc = false;
        [SerializeField] private string npcTag = "NPC";
        [SerializeField] private float smileRadius = 4f;
        [Tooltip("Cada cuánto comprueba si hay un NPC cerca (segundos).")]
        [SerializeField] private float npcScanInterval = 0.3f;

        [Header("Triste con cotilleo malo")]
        [Tooltip("Si la flag que se pone a TRUE contiene alguna de estas palabras, pone cara triste un rato.")]
        [SerializeField] private string[] sadFlagKeywords =
            { "ofendid", "enfadad", "odia", "discut", "velada", "triste", "dolor", "te_odia" };
        [SerializeField] private float sadHoldSeconds = 3f;

        [Header("Fonemas al escribir (en diálogo)")]
        [Tooltip("Mueve la boca con fonemas mientras escribes tu mensaje (como 'escribiendo...').")]
        [SerializeField] private bool mouthWhileTyping = true;
        [Tooltip("Celdas de boca por las que pasa al escribir, EN ORDEN. Pon pocas (p. ej. cerrada y abierta) " +
                 "para un movimiento natural. Mejor que tengan los MISMOS ojos para que no parezcan moverse.")]
        [SerializeField] private int[] phonemeCells = { 0, 1 };
        [Tooltip("Tiempo entre cambios de boca. Súbelo si va muy rápido.")]
        [SerializeField] private float phonemeInterval = 0.14f;

        private Transform[] _npcs;
        private float _scanTimer;
        private bool _nearNpc;
        private float _sadTimer;
        private int _currentBase = -1;
        private NarrativeFlagStore _flags;

        // Reacción manual (tocar la foto) y temporización de fonemas.
        private float _reactTimer;
        private int _reactIndex;
        private float _phonemeTimer;
        private int _phonemeIdx;

        private void Awake()
        {
            if (face == null) face = GetComponentInChildren<FaceExpressionController>();
        }

        private void OnEnable()
        {
            StartCoroutine(BlinkLoop());
            TryHookFlags();
        }

        private void OnDisable()
        {
            if (_flags != null) _flags.OnChanged -= OnFlagChanged;
            _flags = null;
        }

        private void TryHookFlags()
        {
            var d = SproutGameDirector.Instance;
            if (d != null && d.Flags != null && _flags == null)
            {
                _flags = d.Flags;
                _flags.OnChanged += OnFlagChanged;
            }
        }

        private void Update()
        {
            if (face == null) return;
            if (_flags == null) TryHookFlags(); // el director puede crearse después que nosotros

            if (_sadTimer > 0f) _sadTimer -= Time.deltaTime;
            if (_reactTimer > 0f) _reactTimer -= Time.deltaTime;

            _scanTimer -= Time.deltaTime;
            if (_scanTimer <= 0f)
            {
                _scanTimer = npcScanInterval;
                _nearNpc = AnyNpcNear();
            }

            DriveFace();
        }

        /// <summary>
        /// Prioridad de la cara:
        ///   1) Reacción manual (tocar tu foto) manda mientras dura.
        ///   2) Fonemas mientras escribes tu mensaje.
        ///   3) Cara base automática: triste > sonríe cerca de NPC > neutral.
        /// </summary>
        private void DriveFace()
        {
            // 1) Reacción manual elegida por la jugadora.
            if (_reactTimer > 0f) { SetExpr(_reactIndex); return; }

            // 2) Boca moviéndose mientras escribes (en diálogo).
            var dlg = DialogueUI.Active;
            if (mouthWhileTyping && phonemeCells != null && phonemeCells.Length > 0 && dlg != null && dlg.IsTyping)
            {
                _phonemeTimer -= Time.deltaTime;
                if (_phonemeTimer <= 0f)
                {
                    _phonemeTimer = phonemeInterval;
                    _phonemeIdx = (_phonemeIdx + 1) % phonemeCells.Length; // EN ORDEN, no aleatorio
                    face.SetPhoneme(phonemeCells[_phonemeIdx]);
                }
                _currentBase = -1; // al dejar de escribir, fuerza re-aplicar la expresión
                return;
            }

            // 3) Cara base automática: triste manda; si no, neutral (o sonríe cerca si lo activas).
            SetExpr(_sadTimer > 0f ? sadIndex : ((smileNearNpc && _nearNpc) ? happyIndex : neutralIndex));
        }

        private void SetExpr(int index)
        {
            if (index != _currentBase)
            {
                _currentBase = index;
                face.SetExpression(index);
            }
        }

        private bool AnyNpcNear()
        {
            if (_npcs == null || _npcs.Length == 0)
            {
                GameObject[] gos;
                try { gos = GameObject.FindGameObjectsWithTag(npcTag); }
                catch { return false; } // tag no existe todavía
                _npcs = new Transform[gos.Length];
                for (int i = 0; i < gos.Length; i++) _npcs[i] = gos[i].transform;
            }

            float r2 = smileRadius * smileRadius;
            foreach (var n in _npcs)
            {
                if (n == null) continue;
                if ((n.position - transform.position).sqrMagnitude <= r2) return true;
            }
            return false;
        }

        private void OnFlagChanged(string key)
        {
            if (string.IsNullOrEmpty(key) || _flags == null) return;
            if (!_flags.GetFlag(key)) return; // solo reaccionamos cuando se pone a true
            string k = key.ToLowerInvariant();
            foreach (var w in sadFlagKeywords)
                if (!string.IsNullOrEmpty(w) && k.Contains(w)) { _sadTimer = sadHoldSeconds; return; }
        }

        /// <summary>Forzar tristeza desde fuera (eventos, reacción a un ramo, etc.).</summary>
        public void ReactSad(float seconds) => _sadTimer = Mathf.Max(_sadTimer, seconds);

        /// <summary>Mostrar una expresión concreta un rato (la llama la face cam al tocar tu foto).</summary>
        public void ReactWithExpression(int expressionIndex, float seconds)
        {
            _reactIndex = expressionIndex;
            _reactTimer = Mathf.Max(_reactTimer, seconds);
        }

        /// <summary>Miniatura (textura + UV) de una celda de expresión, para el picker de la face cam.</summary>
        public bool TryGetExpressionCellUV(int index, out Texture sheet, out Rect uv)
        {
            sheet = null; uv = default;
            if (face == null) face = GetComponentInChildren<FaceExpressionController>();
            return face != null && face.TryGetExpressionCellUV(index, out sheet, out uv);
        }

        private IEnumerator BlinkLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(blinkEverySeconds.x, blinkEverySeconds.y));
                // no parpadea sin celda de parpadeo, ni triste, ni reaccionando, ni escribiendo
                bool typing = mouthWhileTyping && DialogueUI.Active != null && DialogueUI.Active.IsTyping;
                if (!enableBlink || face == null || _sadTimer > 0f || _reactTimer > 0f || typing) continue;
                face.SetExpression(blinkIndex);
                yield return new WaitForSeconds(blinkDuration);
                _currentBase = -1; // fuerza re-aplicar la cara base
                DriveFace();
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.8f, 0.4f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, smileRadius);
        }
    }
}
