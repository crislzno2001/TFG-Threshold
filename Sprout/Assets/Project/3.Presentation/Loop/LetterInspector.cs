using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Sprout.Presentation
{
    /// <summary>
    /// Visor 3D de un objeto (la carta/sobre). Al llamar Show() (desde el InteractTrigger de la nota), el
    /// modelo aparece GRANDE en pantalla sobre un fondo oscuro; lo giras arrastrando con el ratón. Un clic
    /// izquierdo (sin arrastrar) "abre" la carta -> dispara onOpened (p. ej. mostrar el LetterPopup con el
    /// texto). Esc o clic derecho cierra. Bloquea al jugador mientras está abierto.
    /// </summary>
    public sealed class LetterInspector : MonoBehaviour
    {
        [Header("Modelo a mostrar (prefab o el de la mesa)")]
        [SerializeField] private GameObject model;
        [SerializeField] private float fitMargin = 1.3f;
        [SerializeField] private float rotateSpeed = 0.3f;
        [Tooltip("Giro suave automático mientras lo miras.")]
        [SerializeField] private float autoSpin = 20f;
        [Tooltip("Rotación inicial del modelo al mostrarlo (ajústala si sale de lado).")]
        [SerializeField] private Vector3 modelEuler = Vector3.zero;
        [Tooltip("Texto que sale abajo mientras miras la carta.")]
        [SerializeField] private string hint = "¡Es de tu abuela!   ·   Arrastra para girar   ·   Enter o clic para abrir";

        [Header("Al abrir")]
        [Tooltip("Se dispara al hacer clic para abrir (engancha aquí LetterPopup.Show()).")]
        public UnityEvent onOpened;
        [Tooltip("Opcional: si lo asignas, se muestra automáticamente al abrir.")]
        [SerializeField] private LetterPopup letter;

        [Header("Control")]
        [SerializeField] private bool lockPlayer = true;
        [SerializeField] private string playerTag = "Player";

        private static readonly Vector3 Stage = new Vector3(0f, -9000f, 0f);
        private Camera _cam;
        private GameObject _inst;
        private bool _open, _dragging;
        private Vector2 _lastMouse;
        private float _dragDist;
        private GUIStyle _hint;

        public void Show()
        {
            if (_open || model == null) return;
            _open = true;
            if (lockPlayer) SetPlayerControl(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Cámara dedicada que solo verá el modelo (está aislado lejos, en el "stage").
            var camGo = new GameObject("LetterInspectCam");
            _cam = camGo.AddComponent<Camera>();
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = new Color(0.12f, 0.10f, 0.13f, 1f);
            _cam.depth = 50f; // se dibuja sobre la cámara principal
            _cam.nearClipPlane = 0.01f;

            // Luz para que no salga oscuro.
            var lightGo = new GameObject("InspectLight");
            lightGo.transform.SetParent(camGo.transform, false);
            var l = lightGo.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 1.2f;
            lightGo.transform.localRotation = Quaternion.Euler(25f, -20f, 0f);

            _inst = Instantiate(model, Stage, Quaternion.Euler(modelEuler));

            // Centrar el modelo en el stage y encuadrar la cámara según su tamaño.
            var rends = _inst.GetComponentsInChildren<Renderer>();
            float size = 1f;
            if (rends.Length > 0)
            {
                var b = rends[0].bounds;
                for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
                _inst.transform.position += (Stage - b.center);
                size = Mathf.Max(0.1f, b.size.magnitude);
            }
            _cam.transform.position = Stage + new Vector3(0f, 0f, -size * fitMargin);
            _cam.transform.LookAt(Stage);
            _cam.farClipPlane = size * 10f + 10f;
        }

        private void Update()
        {
            if (!_open) return;

            var mouse = Mouse.current;
            var kb = Keyboard.current;

            if (_inst != null && autoSpin != 0f && !_dragging)
                _inst.transform.Rotate(Vector3.up, autoSpin * Time.unscaledDeltaTime, Space.World);

            if (mouse != null)
            {
                Vector2 pos = mouse.position.ReadValue();
                if (mouse.leftButton.wasPressedThisFrame) { _dragging = true; _lastMouse = pos; _dragDist = 0f; }

                if (_dragging && mouse.leftButton.isPressed)
                {
                    Vector2 d = pos - _lastMouse; _lastMouse = pos; _dragDist += d.magnitude;
                    if (_inst != null)
                    {
                        _inst.transform.Rotate(_cam.transform.up, -d.x * rotateSpeed, Space.World);
                        _inst.transform.Rotate(_cam.transform.right, d.y * rotateSpeed, Space.World);
                    }
                }

                if (mouse.leftButton.wasReleasedThisFrame)
                {
                    bool wasClick = _dragDist < 6f;
                    _dragging = false;
                    if (wasClick) { OpenLetter(); return; }
                }

                if (mouse.rightButton.wasPressedThisFrame) { Close(); return; }
            }

            if (kb != null)
            {
                if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
                { OpenLetter(); return; }
                if (kb.escapeKey.wasPressedThisFrame) Close();
            }
        }

        private void OpenLetter()
        {
            onOpened?.Invoke();
            Close();
            if (letter != null) letter.Show();
        }

        private void Close()
        {
            _open = false;
            if (_inst != null) Destroy(_inst);
            if (_cam != null) Destroy(_cam.gameObject);
            if (lockPlayer) SetPlayerControl(true);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnGUI()
        {
            if (!_open) return;
            if (_hint == null)
            {
                _hint = new GUIStyle(GUI.skin.label)
                { alignment = TextAnchor.MiddleCenter, fontSize = 16, fontStyle = FontStyle.Bold };
                _hint.normal.textColor = new Color(0.96f, 0.93f, 0.86f);
            }
            GUI.Label(new Rect(0, Screen.height - 60, Screen.width, 26), hint, _hint);
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
