using UnityEngine;
using OpenAI.Dialogue;

namespace Sprout.Presentation
{
    /// <summary>
    /// Muestra la cara de la florista en un recuadro abajo (estilo videollamada) MIENTRAS hablas con un
    /// vecino, para que se vean sus expresiones (que en 3ª persona no se ven). Ponlo en el Player.
    /// Crea su propia cámara que enfoca la cabeza y la dibuja en una esquina solo durante el diálogo.
    /// </summary>
    public sealed class DialogueFaceCam : MonoBehaviour
    {
        [Header("A quién enfocar")]
        [Tooltip("Raíz del personaje (su forward = hacia dónde mira). Si lo dejas vacío, usa este objeto.")]
        [SerializeField] private Transform characterRoot;
        [Tooltip("Hueso/punto de la cabeza. Si lo dejas vacío, busca un hueso que contenga 'head'.")]
        [SerializeField] private Transform headAnchor;
        [Tooltip("OPCIONAL pero recomendado: pon aquí un objeto vacío DELANTE de la cara y la cámara se " +
                 "coloca ahí (control total del encuadre). Si lo dejas vacío, se coloca sola delante de la cabeza.")]
        [SerializeField] private Transform camAnchor;

        [Header("Encuadre")]
        [SerializeField] private float distance = 0.65f;
        [SerializeField] private float heightOffset = 0.05f;
        [SerializeField] private float fieldOfView = 28f;
        [Tooltip("Color del fondo del recuadro.")]
        [SerializeField] private Color background = new Color(0.96f, 0.93f, 0.85f);
        [Tooltip("Capas que VE la face cam. Ponla solo en la capa de la florista para que NO salga nada más " +
                 "detrás (ni objetos que se metan delante). Déjala en Everything para ver la escena.")]
        [SerializeField] private LayerMask renderLayers = ~0;
        [Tooltip("OPCIONAL: imagen de fondo del recuadro. Para que se vea, pon 'Render Layers' SOLO en la " +
                 "capa de la florista (si no, la escena tapa el fondo).")]
        [SerializeField] private Texture backgroundImage;

        [Header("Recuadro en pantalla")]
        [SerializeField] private int boxSize = 220;
        [SerializeField] private Vector2 marginFromCorner = new Vector2(24, 24);
        [SerializeField] private bool bottomLeft = true;
        [SerializeField] private string caption = "Tú";

        [Header("Tocar para reaccionar")]
        [Tooltip("Tocar tu foto abre las emociones para reaccionar a lo que te dicen.")]
        [SerializeField] private bool tapToReact = true;
        [Tooltip("Si lo dejas vacío, busca FaceGameplayExpressions en el Player.")]
        [SerializeField] private FaceGameplayExpressions faceController;
        [SerializeField] private float reactSeconds = 3f;
        [Tooltip("Emociones que salen al tocar tu foto: cada una es una CELDA de tu hoja de EXPRESIONES (0..7). " +
                 "Se muestran como miniaturas de tu propia cara.")]
        [SerializeField] private EmotionOption[] emotions =
        {
            new EmotionOption { cell = 0 }, new EmotionOption { cell = 1 },
            new EmotionOption { cell = 2 }, new EmotionOption { cell = 3 },
            new EmotionOption { cell = 4 }, new EmotionOption { cell = 5 },
            new EmotionOption { cell = 6 }, new EmotionOption { cell = 7 },
        };
        [Tooltip("Cuántas caritas por fila en el picker.")]
        [SerializeField] private int emotionsPerRow = 4;
        [Tooltip("Tamaño de cada emoji del picker en píxeles. Súbelo para verlos más grandes.")]
        [SerializeField] private float emotionIconSize = 90f;

        [System.Serializable]
        public struct EmotionOption
        {
            [Tooltip("Celda de tu hoja de EXPRESIONES que se pone al elegir esta emoción (0..7).")]
            public int cell;
            [Tooltip("Icono/emoji (PNG con fondo transparente) que se muestra en el picker.")]
            public Texture icon;
            [Tooltip("Opcional, no se muestra.")]
            public string label;
        }

        private Camera _cam;
        private RenderTexture _rt;
        private GUIStyle _capStyle;
        private bool _pickerOpen;

        private void Awake()
        {
            if (characterRoot == null) characterRoot = transform;
            if (headAnchor == null) headAnchor = FindHead(transform);
            if (faceController == null) faceController = GetComponentInParent<FaceGameplayExpressions>();
            if (faceController == null) faceController = GetComponentInChildren<FaceGameplayExpressions>(true);
            if (faceController == null) faceController = FindFirstObjectByType<FaceGameplayExpressions>();

            _rt = new RenderTexture(320, 320, 16, RenderTextureFormat.ARGB32) { name = "FaceCamRT" };

            var go = new GameObject("FaceCam");
            go.transform.SetParent(transform, false);
            _cam = go.AddComponent<Camera>();
            _cam.fieldOfView = fieldOfView;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            // Con imagen de fondo, la cámara limpia en transparente para que se vea la imagen detrás de la cara.
            _cam.backgroundColor = backgroundImage != null
                ? new Color(background.r, background.g, background.b, 0f)
                : background;
            _cam.targetTexture = _rt;
            _cam.cullingMask = renderLayers;  // así no la tapan otros objetos
            _cam.nearClipPlane = 0.03f;
            _cam.depth = -20;        // que no interfiera con la principal
            _cam.enabled = false;    // solo durante el diálogo
        }

        private static Transform FindHead(Transform root)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                string n = t.name.ToLowerInvariant();
                if (n.Contains("head") && !n.Contains("headtop") && !n.Contains("end")) return t;
            }
            return root;
        }

        private static bool DialogueOpen => DialogueUI.Active != null && DialogueUI.Active.IsOpen;

        private void LateUpdate()
        {
            bool show = DialogueOpen;
            if (_cam.enabled != show) _cam.enabled = show;
            if (!show || headAnchor == null) return;

            Vector3 head = headAnchor.position + Vector3.up * heightOffset;
            if (camAnchor != null)
            {
                _cam.transform.position = camAnchor.position;
                _cam.transform.rotation = Quaternion.LookRotation(head - camAnchor.position, Vector3.up);
            }
            else
            {
                Vector3 fwd = characterRoot != null ? characterRoot.forward : transform.forward;
                _cam.transform.position = head + fwd * distance;
                _cam.transform.rotation = Quaternion.LookRotation(head - _cam.transform.position, Vector3.up);
            }
        }

        private void OnGUI()
        {
            if (!DialogueOpen || _rt == null) return;

            float x = bottomLeft ? marginFromCorner.x : Screen.width - boxSize - marginFromCorner.x;
            float y = Screen.height - boxSize - marginFromCorner.y;

            var prev = GUI.color;
            GUI.color = new Color(0.96f, 0.93f, 0.85f); // marco crema
            GUI.DrawTexture(new Rect(x - 4, y - 4, boxSize + 8, boxSize + 8), Texture2D.whiteTexture);
            GUI.color = prev;

            var inner = new Rect(x, y, boxSize, boxSize);
            if (backgroundImage != null) GUI.DrawTexture(inner, backgroundImage, ScaleMode.ScaleAndCrop);
            GUI.DrawTexture(inner, _rt, ScaleMode.ScaleAndCrop, true); // alphaBlend: compone la cara sobre el fondo

            if (!string.IsNullOrEmpty(caption))
            {
                if (_capStyle == null)
                    _capStyle = new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 14,
                        normal = { textColor = new Color(0.30f, 0.22f, 0.18f) }
                    };
                GUI.Label(new Rect(x, y + boxSize - 24, boxSize, 22), caption, _capStyle);
            }

            // Tocar tu foto -> abrir las emociones para reaccionar a lo que te dicen.
            if (tapToReact && faceController != null)
            {
                if (GUI.Button(inner, GUIContent.none, GUIStyle.none)) _pickerOpen = !_pickerOpen;

                if (_pickerOpen && emotions != null && emotions.Length > 0)
                {
                    int perRow = Mathf.Max(1, emotionsPerRow);
                    float gap = 3f;
                    float cellSize = emotionIconSize;
                    float gridW = perRow * cellSize + (perRow - 1) * gap;
                    int gridRows = Mathf.CeilToInt(emotions.Length / (float)perRow);
                    float gridH = gridRows * cellSize + (gridRows - 1) * gap;
                    float gridTop = y - 12f - gridH;
                    float gridLeft = x + (boxSize - gridW) * 0.5f;   // centrado sobre el recuadro

                    var pc = GUI.color;
                    GUI.color = new Color(0.96f, 0.93f, 0.85f, 0.96f); // panelito crema detrás de las caritas
                    GUI.DrawTexture(new Rect(gridLeft - 8, gridTop - 8, gridW + 16, gridH + 16), Texture2D.whiteTexture);
                    GUI.color = pc;

                    for (int i = 0; i < emotions.Length; i++)
                    {
                        int col = i % perRow, row = i / perRow;
                        var r = new Rect(gridLeft + col * (cellSize + gap), gridTop + row * (cellSize + gap), cellSize, cellSize);

                        if (emotions[i].icon != null)
                            GUI.DrawTexture(r, emotions[i].icon, ScaleMode.ScaleAndCrop, true);  // llena la celda (recorta margen transparente)
                        else if (faceController.TryGetExpressionCellUV(emotions[i].cell, out var sheet, out var uvr) && sheet != null)
                            GUI.DrawTextureWithTexCoords(r, sheet, uvr);   // si no hay emoji, miniatura de tu cara
                        else
                            GUI.Box(r, emotions[i].cell.ToString());

                        if (GUI.Button(r, GUIContent.none, GUIStyle.none))
                        {
                            faceController.ReactWithExpression(emotions[i].cell, reactSeconds);
                            _pickerOpen = false;
                        }
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (_rt != null) { _rt.Release(); Destroy(_rt); }
        }
    }
}
