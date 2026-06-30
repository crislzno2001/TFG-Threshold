using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Centraliza la curvatura del mundo. Cada frame fija el origen de la curva (la posición del jugador) y
/// la fuerza global en TODOS los materiales CurvedWorld, de dos formas a la vez para que funcione siempre:
///   1) Como propiedades GLOBAL (Shader.SetGlobal*) -> llega a cualquier material cuyo _CurveOrigin /
///      _CurveStrength tengan Scope = Global, sin lista ni mantenimiento.
///   2) Recolectando automáticamente los materiales CurvedWorld de la escena y fijándoselos uno a uno ->
///      cubre los que todavía tengan esas propiedades como Per Material.
///
/// Resultado: cuando cambias un material a un shader CurvedWorld, se curva SOLO (no hay que añadirlo a
/// ninguna lista), y el CurveStrength se controla desde aquí.
/// </summary>
[ExecuteAlways]
public class CurvedWorldOriginSetter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;

    [Header("Curve")]
    [Tooltip("Curvatura en el EDITOR (déjala en 0 para colocar casas/props en plano).")]
    [SerializeField] private float editModeCurveStrength = 0f;
    [Tooltip("Curvatura en PLAY (p. ej. 0.01 para el efecto curvado).")]
    [SerializeField] private float playModeCurveStrength = 0.01f;

    [Tooltip("Materiales curvados GUARDADOS. Se rellenan SOLOS al aplicar el shader con " +
             "'Tools/Sprout/Apply Curved World to Objects'. NO se escanea la escena al arrancar (cargaba lento).")]
    [SerializeField] private List<Material> curvedWorldMaterials = new();

    private static readonly int CurveOriginID = Shader.PropertyToID("_CurveOrigin");
    private static readonly int CurveStrengthID = Shader.PropertyToID("_CurveStrength");

    private void LateUpdate()
    {
        if (player == null) return;
        Vector3 origin = player.position;

        // Curvatura según el modo: en el editor usa Edit Mode (0 = plano para colocar), en Play usa Play Mode.
        float strength = UnityEngine.Application.isPlaying ? playModeCurveStrength : editModeCurveStrength;

        // Global: llega a todo material con _CurveOrigin/_CurveStrength en Scope = Global.
        Shader.SetGlobalVector(CurveOriginID, origin);
        Shader.SetGlobalFloat(CurveStrengthID, strength);

        // Per-material (los que sean Per Material). La lista está GUARDADA: se rellena sola al aplicar el
        // shader con la herramienta. Ya NO se escanea la escena (ni al arrancar ni en bucle).
        for (int i = 0; i < curvedWorldMaterials.Count; i++)
        {
            var m = curvedWorldMaterials[i];
            if (m == null) continue;
            m.SetVector(CurveOriginID, origin);
            m.SetFloat(CurveStrengthID, strength);
        }
    }

    /// <summary>
    /// Botón MANUAL de emergencia (clic derecho en el componente). Normalmente NO hace falta: la lista se
    /// rellena sola al aplicar el shader con la herramienta. Úsalo solo si quieres re-escanear toda la escena.
    /// </summary>
    [ContextMenu("Recolectar materiales curved (manual)")]
    public void Collect()
    {
        curvedWorldMaterials.Clear();
        var seen = new HashSet<Material>();

        foreach (var r in FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            foreach (var m in r.sharedMaterials)
                if (IsCurved(m) && seen.Add(m)) curvedWorldMaterials.Add(m);

        foreach (var t in FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.materialTemplate != null && IsCurved(t.materialTemplate) && seen.Add(t.materialTemplate))
                curvedWorldMaterials.Add(t.materialTemplate);

#if UNITY_EDITOR
        if (!UnityEngine.Application.isPlaying) UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    private static bool IsCurved(Material m)
        => m != null && m.shader != null && m.shader.name.ToLowerInvariant().Contains("curvedworld");
}
