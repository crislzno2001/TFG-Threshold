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
    [SerializeField] private float curveStrength = 0.01f;

    [Tooltip("Recoge automáticamente todos los materiales CurvedWorld de la escena (sin lista a mano).")]
    [SerializeField] private bool autoCollect = true;

    private static readonly int CurveOriginID = Shader.PropertyToID("_CurveOrigin");
    private static readonly int CurveStrengthID = Shader.PropertyToID("_CurveStrength");

    private readonly List<Material> _materials = new();
    private float _refreshTimer;

    private void OnEnable() => Collect();

    private void LateUpdate()
    {
        if (player == null) return;
        Vector3 origin = player.position;

        // 1) Global: llega a todo material con _CurveOrigin/_CurveStrength en Scope = Global.
        Shader.SetGlobalVector(CurveOriginID, origin);
        Shader.SetGlobalFloat(CurveStrengthID, curveStrength);

        // 2) Per-material: en EJECUCIÓN el conjunto de materiales curvados no cambia, así que se recoge
        //    una sola vez (en OnEnable) y no se vuelve a escanear. El re-escaneo periódico se limita al
        //    EDITOR, como comodidad para recoger los materiales que vas convirtiendo al componer la escena.
        if (autoCollect && !Application.isPlaying)
        {
            _refreshTimer += 0.05f;
            if (_refreshTimer >= 1f) { _refreshTimer = 0f; Collect(); }
        }

        for (int i = 0; i < _materials.Count; i++)
        {
            var m = _materials[i];
            if (m == null) continue;
            m.SetVector(CurveOriginID, origin);
            m.SetFloat(CurveStrengthID, curveStrength);
        }
    }

    /// <summary>Busca en la escena todos los materiales que usan un shader CurvedWorld.</summary>
    [ContextMenu("Recolectar materiales curved")]
    public void Collect()
    {
        _materials.Clear();
        var seen = new HashSet<Material>();

        foreach (var r in FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            foreach (var m in r.sharedMaterials)
                if (IsCurved(m) && seen.Add(m)) _materials.Add(m);

        foreach (var t in FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.materialTemplate != null && IsCurved(t.materialTemplate) && seen.Add(t.materialTemplate))
                _materials.Add(t.materialTemplate);
    }

    private static bool IsCurved(Material m)
        => m != null && m.shader != null && m.shader.name.ToLowerInvariant().Contains("curvedworld");
}
