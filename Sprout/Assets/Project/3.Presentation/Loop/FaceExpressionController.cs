using UnityEngine;

namespace Sprout.Presentation
{
    /// <summary>
    /// Controla la CARA por desplazamiento de UV (técnica del vídeo). Soporta DOS hojas con la misma
    /// rejilla: EXPRESIONES (emociones) y FONEMAS (bocas para hablar). El UV de la cara está en la celda 0;
    /// el componente cambia de hoja y mueve el offset para elegir la celda.
    ///
    ///   SetExpression(0..N) -> emoción (hoja de expresiones)
    ///   SetPhoneme(0..N)    -> boca al hablar (hoja de fonemas)
    ///
    /// [ExecuteAlways] + OnValidate: puedes previsualizar cambiando "Start Expression" en el inspector
    /// SIN darle a Play. Si una celda sale mal, ajusta columns/rows, baseOffset o flipY.
    /// </summary>
    [ExecuteAlways]
    public sealed class FaceExpressionController : MonoBehaviour
    {
        [Header("Renderer / material de la cara")]
        [SerializeField] private Renderer faceRenderer;
        [SerializeField] private int materialIndex = 1;
        [SerializeField] private string textureProperty = "_BaseMap";

        [Header("Las dos hojas (misma rejilla)")]
        [SerializeField] private Texture expressionSheet;
        [SerializeField] private Texture phonemeSheet;

        [Header("Rejilla (tus hojas son 2 columnas x 4 filas)")]
        [SerializeField] private int columns = 2;
        [SerializeField] private int rows = 4;
        [Tooltip("Offset de la celda 0 (ajústalo si tu UV no está en la esquina superior-izquierda).")]
        [SerializeField] private Vector2 baseOffset = Vector2.zero;
        [Tooltip("Invierte la dirección vertical si las filas salen al revés.")]
        [SerializeField] private bool flipY = true;
        [Tooltip("Vista previa: cambia este número para ver cada cara (también en el editor).")]
        [SerializeField] private int startExpression = 0;
        [Tooltip("Si está marcado, la vista previa usa la hoja de FONEMAS en vez de la de expresiones.")]
        [SerializeField] private bool previewPhonemes = false;

        private void Awake() => Preview();

        // Previsualización en el editor al cambiar valores en el inspector.
        private void OnValidate() => Preview();

        private void Preview()
        {
            if (previewPhonemes) SetPhoneme(startExpression);
            else SetExpression(startExpression);
        }

        /// <summary>Cara de emoción (hoja de expresiones).</summary>
        public void SetExpression(int index) => SetCell(expressionSheet, index);

        /// <summary>Boca al hablar (hoja de fonemas).</summary>
        public void SetPhoneme(int index) => SetCell(phonemeSheet, index);

        private void SetCell(Texture sheet, int index)
        {
            var mat = GetMaterial();
            if (mat == null) return;
            if (sheet != null) mat.SetTexture(textureProperty, sheet);

            index = Mathf.Clamp(index, 0, columns * rows - 1);
            int col = index % columns;
            int row = index / columns;
            float ox = col * (1f / columns);
            float oy = (flipY ? -1f : 1f) * row * (1f / rows);
            mat.SetTextureOffset(textureProperty, baseOffset + new Vector2(ox, oy));
        }

        /// <summary>Da la textura y el UV de una celda de la hoja de EXPRESIONES (para dibujar miniaturas en GUI).</summary>
        public bool TryGetExpressionCellUV(int index, out Texture sheet, out Rect uv)
        {
            sheet = expressionSheet;
            uv = default;
            if (expressionSheet == null || columns <= 0 || rows <= 0) return false;
            index = Mathf.Clamp(index, 0, columns * rows - 1);
            int col = index % columns;
            int row = index / columns;
            float sx = 1f / columns, sy = 1f / rows;
            float ox = baseOffset.x + col * sx;
            float oy = baseOffset.y + (flipY ? -1f : 1f) * row * sy;
            uv = new Rect(ox, oy, sx, sy);
            return true;
        }

        private Material GetMaterial()
        {
            if (faceRenderer == null) faceRenderer = GetComponentInChildren<Renderer>();
            if (faceRenderer == null) return null;
            // En Play usamos la instancia; en el editor el sharedMaterial (para no instanciar/leak).
            var mats = UnityEngine.Application.isPlaying ? faceRenderer.materials : faceRenderer.sharedMaterials;
            return (materialIndex >= 0 && materialIndex < mats.Length) ? mats[materialIndex] : null;
        }
    }
}
