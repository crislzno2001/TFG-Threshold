using UnityEngine;

namespace Sprout.Presentation
{
    /// <summary>
    /// Paleta cozy COMPARTIDA para que todo el UI por código (IMGUI: carteles, carta, recuadro de la cara,
    /// avisos, HUD) use los MISMOS colores que el menú de inicio (crema + verde salvia). Así, con la fuente
    /// Fredoka + estos colores, todo el juego se ve del mismo estilo sin reescribir nada.
    /// </summary>
    public static class SproutPalette
    {
        // Mismos valores que MainMenu.uss (crema de botones, verde salvia, texto).
        public static readonly Color Cream     = new Color(1f, 0.988f, 0.965f);   // rgb(255,252,246) paneles/pastillas
        public static readonly Color CreamWarm = new Color(0.96f, 0.93f, 0.85f);  // marcos/foto
        public static readonly Color Green     = new Color(0.486f, 0.663f, 0.510f); // rgb(124,169,130)
        public static readonly Color GreenDark = new Color(0.337f, 0.490f, 0.361f); // rgb(86,125,92)
        public static readonly Color TextDark  = new Color(0.290f, 0.243f, 0.212f); // rgb(74,62,54)
        public static readonly Color GreenText = new Color(0.361f, 0.502f, 0.329f); // rgb(92,128,84)
        public static readonly Color PanelDark = new Color(0.14f, 0.12f, 0.11f, 0.82f); // pastilla oscura translúcida
        public static readonly Color Dim       = new Color(0f, 0f, 0f, 0.5f);      // fondo oscurecido de popups

        private static Texture2D _rounded;
        /// <summary>Textura blanca con esquinas redondeadas (para pastillas/paneles con 9-slice, border 14).</summary>
        public static Texture2D RoundedRect => _rounded != null ? _rounded : (_rounded = MakeRounded(48, 14));

        private static Texture2D MakeRounded(int size, int radius)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int dx = Mathf.Min(x, size - 1 - x);
                    int dy = Mathf.Min(y, size - 1 - y);
                    float a = 1f;
                    if (dx < radius && dy < radius)
                    {
                        float d = Mathf.Sqrt((radius - dx) * (radius - dx) + (radius - dy) * (radius - dy));
                        a = Mathf.Clamp01(radius - d + 0.5f);
                    }
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            tex.Apply();
            return tex;
        }
    }
}
