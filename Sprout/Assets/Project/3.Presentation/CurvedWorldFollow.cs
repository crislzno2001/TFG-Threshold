using UnityEngine;

namespace Sprout.Presentation
{
    /// <summary>
    /// Compensa la curva del mundo en un objeto (p. ej. la etiqueta de NOMBRE de un NPC): baja su Y lo
    /// mismo que la curva baja la malla, para que quede sobre la cabeza a cualquier distancia.
    /// Ponlo en el objeto del texto/nombre. Si no cuadra del todo, ajusta 'multiplier'.
    /// </summary>
    public sealed class CurvedWorldFollow : MonoBehaviour
    {
        [Tooltip("Ajuste fino: si el nombre queda un pelín alto/bajo, sube/baja esto (1 = igual que la malla).")]
        [SerializeField] private float multiplier = 1f;

        private Vector3 _baseLocal;
        private bool _has;

        private void OnEnable() { _baseLocal = transform.localPosition; _has = true; }

        private void LateUpdate()
        {
            if (!_has) return;
            // Usamos su PROPIA posición horizontal (x,z) para la distancia a la florista: robusto aunque
            // el nombre esté anidado. Solo movemos la Y, así que x,z no cambian (sin realimentación).
            float curveY = CurvedWorldCompensation.OffsetY(transform.position, multiplier);
            transform.localPosition = _baseLocal + Vector3.up * curveY;
        }

        /// <summary>Clic derecho (en Play) → imprime todos los datos de la curva para diagnosticar.</summary>
        [ContextMenu("Log datos de la curva")]
        private void LogNow()
        {
            Vector4 origin = Shader.GetGlobalVector(Shader.PropertyToID("_CurveOrigin"));
            float strength = Shader.GetGlobalFloat(Shader.PropertyToID("_CurveStrength"));
            Vector3 p = transform.position;
            float dx = p.x - origin.x, dz = p.z - origin.z;
            float d2 = dx * dx + dz * dz;
            Debug.Log($"[CWF {name}] origin={origin}  strength={strength}  pos={p}  " +
                      $"dist²={d2:0.0}  dist={Mathf.Sqrt(d2):0.0}  curveY(mult=1)={-strength * d2:0.00}", this);
        }
    }
}
