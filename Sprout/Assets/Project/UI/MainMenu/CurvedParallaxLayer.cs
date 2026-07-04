using UnityEngine;
using UnityEngine.Rendering;

namespace ThresholdGame.Presentation.UI.MainMenu
{
    /// <summary>
    /// Capa de parallax CURVADA tipo "mundo redondeado" (estilo Savoring the Breeze). A partir de una
    /// textura RECTA y tileable (p. ej. la hilera de casas), genera un plano subdividido, lo curva en
    /// parábola (como la superficie de un planeta) y desliza la textura para simular que el mundo avanza.
    ///
    /// Pon una por capa (cielo, casas, suelo) a distinta Z y distinta 'scrollSpeed' → efecto parallax.
    /// La curva va aquí (no en la imagen) para que la capa pueda repetirse/moverse sin fin.
    ///
    /// Uso: GameObject vacío → añade este componente → arrastra la textura → ajusta width/curvature/speed.
    /// La textura debe ser Wrap Mode = Repeat (se fuerza en Awake).
    /// </summary>
    [RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
    public sealed class CurvedParallaxLayer : MonoBehaviour
    {
        [Header("Arte")]
        [Tooltip("Textura RECTA y tileable en horizontal (fondo transparente).")]
        [SerializeField] private Texture2D texture;
        [Tooltip("Cuántas veces se repite la textura a lo ancho del plano.")]
        [SerializeField] private float tiling = 1f;

        [Header("Tamaño (unidades de mundo)")]
        [SerializeField] private float width = 24f;
        [SerializeField] private float height = 6f;
        [Tooltip("Subdivisiones horizontales: más = curva más suave.")]
        [SerializeField] private int segments = 48;

        [Header("Curva de planeta")]
        [Tooltip("Cuánto se hunden los bordes hacia abajo (0 = plano, 2-4 = curvado cozy).")]
        [SerializeField] private float curvature = 3f;

        [Header("Movimiento")]
        [Tooltip("Velocidad de scroll de la textura. Capas de fondo = lento; suelo = rápido.")]
        [SerializeField] private float scrollSpeed = 0.05f;

        private Material _mat;
        private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");

        private void Awake()
        {
            BuildMesh();

            var sh = Shader.Find("Universal Render Pipeline/Unlit");
            if (sh == null) sh = Shader.Find("Unlit/Transparent");
            _mat = new Material(sh);

            if (texture != null)
            {
                texture.wrapMode = TextureWrapMode.Repeat;
                _mat.mainTexture = texture;
                if (_mat.HasProperty(BaseMap)) _mat.SetTexture(BaseMap, texture);
            }

            // Transparencia URP (mezcla alfa, sin escribir profundidad, doble cara).
            _mat.SetOverrideTag("RenderType", "Transparent");
            _mat.SetFloat("_Surface", 1);
            _mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            _mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            _mat.SetInt("_ZWrite", 0);
            _mat.SetFloat("_Cull", (int)CullMode.Off);
            _mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            _mat.renderQueue = (int)RenderQueue.Transparent;

            GetComponent<MeshRenderer>().sharedMaterial = _mat;
        }

        private void Update()
        {
            if (_mat == null) return;
            Vector2 o = _mat.mainTextureOffset;
            o.x += scrollSpeed * Time.deltaTime;
            _mat.mainTextureOffset = o;
            if (_mat.HasProperty(BaseMap)) _mat.SetTextureOffset(BaseMap, o);
        }

        private void BuildMesh()
        {
            var mesh = new Mesh { name = "CurvedParallax" };
            int vx = segments + 1;
            var verts = new Vector3[vx * 2];
            var uv = new Vector2[vx * 2];
            var tris = new int[segments * 6];

            for (int i = 0; i < vx; i++)
            {
                float t = (float)i / segments;      // 0..1 a lo ancho
                float x = (t - 0.5f) * width;        // -w/2 .. w/2
                float nx = (t - 0.5f) * 2f;          // -1..1
                float dip = -curvature * nx * nx;    // parábola: bordes hacia abajo = planeta

                verts[i]      = new Vector3(x,  height * 0.5f + dip, 0f); // fila superior
                verts[i + vx] = new Vector3(x, -height * 0.5f + dip, 0f); // fila inferior
                uv[i]      = new Vector2(t * tiling, 1f);
                uv[i + vx] = new Vector2(t * tiling, 0f);
            }

            for (int i = 0; i < segments; i++)
            {
                int ti = i * 6;
                int a = i, b = i + 1, c = i + vx, d = i + 1 + vx;
                tris[ti] = a; tris[ti + 1] = b; tris[ti + 2] = c;
                tris[ti + 3] = b; tris[ti + 4] = d; tris[ti + 5] = c;
            }

            mesh.vertices = verts;
            mesh.uv = uv;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            GetComponent<MeshFilter>().mesh = mesh;
        }
    }
}
