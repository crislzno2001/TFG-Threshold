using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Sprout.Presentation
{
    /// <summary>
    /// Cámara cinemática de bienvenida al pueblo. Como la casa y la floristería están lejos, la cámara
    /// VIAJA para enseñarlas: florista → CASA (con cartel) → vuelve a la florista → FLORISTERÍA (cartel)
    /// → vuelve. Al acabar devuelve el control y la cámara normal del juego. Se reproduce una sola vez.
    ///
    /// Uso: pon este componente en un objeto de la escena del pueblo, arrastra la casa y la floristería
    /// (el player se busca por tag), y ajusta los textos. Play On Start lo lanza al entrar.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public sealed class VillageIntroTour : MonoBehaviour
    {
        [Header("Objetivo")]
        [SerializeField] private string playerTag = "Player";
        [Tooltip("Si lo dejas vacío, se busca por tag al empezar.")]
        [SerializeField] private Transform player;

        [Header("Pasos del recorrido (añade los que quieras)")]
        [Tooltip("Cada paso: la cámara VIAJA hasta el objeto 'target', muestra el texto, y sigues con clic. " +
                 "Añade tantos como quieras (casa, floristería, plaza, un vecino…).")]
        [SerializeField] private List<Stop> stops = new();

        [System.Serializable]
        public class Stop
        {
            [TextArea(2, 4)] public string text;
            [Tooltip("Objeto que la cámara va a ENSEÑAR (a dónde viaja). Vacío = la cámara no se mueve.")]
            public Transform target;
            [Tooltip("Tras este paso, la cámara VUELVE al jugador antes del siguiente.")]
            public bool returnToPlayerAfter = true;
        }

        [Header("Encuadre de la cámara")]
        [Tooltip("Ángulo horizontal desde el que la cámara mira a cada sitio (ponlo igual que tu cámara del juego).")]
        [SerializeField] private float viewYaw = 0f;
        [SerializeField] private float pitch = 28f;
        [SerializeField] private float distance = 12f;
        [SerializeField] private float lookHeight = 1.4f;
        [Tooltip("Segundos que tarda la cámara en viajar de un sitio a otro.")]
        [SerializeField] private float moveDuration = 2.2f;

        [Header("Cuadro de texto (ajústalo si se ve pequeño)")]
        [SerializeField] private float boxWidth = 660f;
        [SerializeField] private float boxHeight = 140f;
        [SerializeField] private int fontSize = 24;
        [Tooltip("Márgenes internos del texto dentro del cuadro.")]
        [SerializeField] private float textPadding = 26f;

        [Header("Reproducir")]
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private bool playOnce = true;
        [SerializeField] private string saveKey = "tour_pueblo";

        public UnityEngine.Events.UnityEvent onFinished;

        private Camera _cam;
        private bool _running;
        private string _caption = "";
        private GUIStyle _cap, _hint, _panel;

        private void Start()
        {
            if (playOnStart) StartCoroutine(AutoPlayWhenReady());
        }

        private IEnumerator AutoPlayWhenReady()
        {
            // Espera a que el director/guardado estén listos, para no repetir el tour si ya se vio en ESTA
            // partida (el "ya visto" se guarda con la partida y se resetea al empezar una nueva).
            float t = 0f;
            while (Sprout.Application.SproutGameDirector.Instance == null && t < 3f)
            { t += Time.unscaledDeltaTime; yield return null; }
            yield return null; // un frame extra por si el guardado carga los flags
            Play();
        }

        /// <summary>Clic derecho en el componente → olvida que ya se vio (para volver a probarlo en Play).</summary>
        [ContextMenu("Reset 'ya visto' (volver a probar)")]
        private void ResetSeen()
        {
            var d = Sprout.Application.SproutGameDirector.Instance;
            if (d != null && d.Flags != null) { d.Flags.SetFlag(saveKey, false); Debug.Log("[Tour] reseteado: volverá a salir."); }
            else Debug.Log("[Tour] el reset solo funciona en Play (necesita el director). Para probar, desmarca 'Play Once'.");
        }

        private bool Seen()
        {
            var d = Sprout.Application.SproutGameDirector.Instance;
            return d != null && d.Flags != null && d.Flags.GetFlag(saveKey);
        }

        private void MarkSeen()
        {
            var d = Sprout.Application.SproutGameDirector.Instance;
            if (d != null && d.Flags != null) d.Flags.SetFlag(saveKey, true);
        }

        public void Play()
        {
            if (_running) { Debug.Log("[Tour] ya está corriendo."); return; }
            if (playOnce && Seen())
            {
                Debug.Log("[Tour] SALTADO: ya se vio en esta partida. (Se resetea al empezar una partida nueva.)");
                return;
            }
            Debug.Log($"[Tour] iniciando… pasos configurados: {(stops != null ? stops.Count : 0)}");
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            if (player == null)
            {
                var p = GameObject.FindGameObjectWithTag(playerTag);
                if (p != null) player = p.transform;
            }
            if (player == null)
            {
                Debug.LogWarning($"[Tour] NO encuentro al player con tag '{playerTag}'. Comprueba que el player tiene ese Tag.");
                yield break;
            }

            _running = true;
            SetPlayerControl(false);

            // Cámara del tour, por encima de la cámara normal del juego.
            var go = new GameObject("VillageTourCamera");
            _cam = go.AddComponent<Camera>();
            _cam.depth = 100f;
            Pose startPose = ShotFor(player);
            _cam.transform.SetPositionAndRotation(startPose.position, startPose.rotation);

            // Recorre los pasos que hayas configurado en el inspector.
            if (stops != null)
            {
                foreach (var stop in stops)
                {
                    if (stop == null) continue;
                    if (stop.target != null) yield return MoveTo(ShotFor(stop.target));
                    _caption = stop.text;
                    yield return WaitClick();
                    if (stop.returnToPlayerAfter) yield return MoveTo(ShotFor(player));
                }
            }

            if (_cam != null) Destroy(_cam.gameObject);
            _caption = "";
            SetPlayerControl(true);
            if (playOnce) MarkSeen();
            _running = false;
            onFinished?.Invoke();
        }

        /// <summary>Pose de cámara que encuadra un objetivo desde un ángulo fijo (arriba y atrás).</summary>
        private Pose ShotFor(Transform t)
        {
            Vector3 look = t.position + Vector3.up * lookHeight;
            Vector3 dir = Quaternion.Euler(pitch, viewYaw, 0f) * Vector3.forward;
            Vector3 pos = look - dir * distance;
            return new Pose(pos, Quaternion.LookRotation(look - pos, Vector3.up));
        }

        private IEnumerator MoveTo(Pose target)
        {
            Vector3 fromP = _cam.transform.position;
            Quaternion fromR = _cam.transform.rotation;
            float t = 0f;
            while (t < moveDuration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / moveDuration));
                _cam.transform.position = Vector3.Lerp(fromP, target.position, k);
                _cam.transform.rotation = Quaternion.Slerp(fromR, target.rotation, k);
                yield return null;
            }
            _cam.transform.SetPositionAndRotation(target.position, target.rotation);
        }

        private IEnumerator WaitClick()
        {
            yield return null; // ignora el clic del frame actual
            while (!Pressed()) yield return null;
        }

        private IEnumerator WaitClickOrTime(float seconds)
        {
            float t = 0f;
            yield return null;
            while (t < seconds && !Pressed()) { t += Time.unscaledDeltaTime; yield return null; }
        }

        private static bool Pressed()
        {
            var kb = Keyboard.current; var m = Mouse.current;
            return (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)) ||
                   (m != null && m.leftButton.wasPressedThisFrame);
        }

        private void OnGUI()
        {
            if (!_running || string.IsNullOrEmpty(_caption)) return;
            EnsureStyles();

            float w = boxWidth, h = boxHeight;
            float x = (Screen.width - w) / 2f, y = Screen.height - h - 40f;
            float pad = textPadding;

            var prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.22f);
            GUI.Box(new Rect(x + 5, y + 6, w, h), GUIContent.none, _panel);
            GUI.color = SproutPalette.Cream;
            GUI.Box(new Rect(x, y, w, h), GUIContent.none, _panel);
            GUI.color = prev;

            GUI.Label(new Rect(x + pad, y + pad * 0.6f, w - pad * 2f, h - pad - 22f), _caption, _cap);
            GUI.Label(new Rect(x + pad, y + h - 28f, w - pad * 2f, 22f), "clic / Espacio  ▶", _hint);
        }

        private void EnsureStyles()
        {
            if (_cap != null) return;
            _cap = new GUIStyle(GUI.skin.label) { fontSize = fontSize, wordWrap = true, alignment = TextAnchor.UpperLeft };
            _cap.normal.textColor = SproutPalette.TextDark;
            _hint = new GUIStyle(GUI.skin.label) { fontSize = 13, alignment = TextAnchor.MiddleRight, fontStyle = FontStyle.Italic };
            _hint.normal.textColor = SproutPalette.GreenText;
            _panel = new GUIStyle { border = new RectOffset(14, 14, 14, 14) };
            _panel.normal.background = SproutPalette.RoundedRect;
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
