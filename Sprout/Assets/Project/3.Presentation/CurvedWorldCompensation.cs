using UnityEngine;

namespace Sprout.Presentation
{
    /// <summary>
    /// El shader CurvedWorld dobla la MALLA de los objetos hacia abajo según su distancia a la florista
    /// (_CurveOrigin), pero los billboards/UI (bola de glow, nombre del NPC) NO se doblan → quedan cada vez
    /// más "flotando" arriba con la distancia. Este helper calcula cuánto baja la curva en un punto, para
    /// compensar esos objetos y que sigan pegados a la cabeza del personaje.
    /// </summary>
    public static class CurvedWorldCompensation
    {
        private static readonly int OriginId = Shader.PropertyToID("_CurveOrigin");
        private static readonly int StrengthId = Shader.PropertyToID("_CurveStrength");

        /// <summary>Desplazamiento en Y que la curva del mundo aplica en 'worldPos' (normalmente negativo).</summary>
        public static float OffsetY(Vector3 worldPos, float multiplier = 1f)
        {
            Vector4 origin = Shader.GetGlobalVector(OriginId);
            float strength = Shader.GetGlobalFloat(StrengthId);
            float dx = worldPos.x - origin.x;
            float dz = worldPos.z - origin.z;
            return -strength * (dx * dx + dz * dz) * multiplier;
        }
    }
}
