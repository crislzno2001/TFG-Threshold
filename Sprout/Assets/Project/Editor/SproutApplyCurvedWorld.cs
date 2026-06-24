#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Sprout.EditorTools
{
    /// <summary>
    /// Uso único: cambia el shader de los materiales al CurvedWorld (shader graph) y les vuelve a poner
    /// sus texturas (albedo, normal, metallic/smoothness) y color. Además mete cada material en la lista
    /// 'curvedWorldMaterials' del CurvedWorldOriginSetter de la escena, porque si no, NO se curva.
    ///
    /// - Lee las texturas de los datos guardados del material (sirve aunque ahora esté en URP/Lit).
    /// - SALTA agua/cristal (transparentes) y vegetación de recorte (grass/leaf/ivy…), porque el
    ///   CurvedWorld es opaco y les rompería la transparencia/recorte. Esas se quedan en URP/Lit.
    ///
    /// Carpeta seleccionada en Project, o "Assets/Suntail Village" por defecto.
    /// Menú:  Tools/Sprout/Apply Curved World to Objects
    /// </summary>
    public static class SproutApplyCurvedWorld
    {
        [MenuItem("Tools/Sprout/Apply Curved World to Objects")]
        public static void Apply()
        {
            // 1) Shader CurvedWorld
            Shader curved = FindCurvedShader();
            if (curved == null) { Dlg("No encuentro el shader CurvedWorld (.shadergraph). ¿Está en el proyecto?"); return; }

            // 2) Propiedades del shader (clasificadas por nombre)
            string baseProp = null, normalProp = null, metalProp = null, colorProp = null;
            int count = ShaderUtil.GetPropertyCount(curved);
            for (int i = 0; i < count; i++)
            {
                string n = ShaderUtil.GetPropertyName(curved, i);
                string l = n.ToLowerInvariant();
                var type = ShaderUtil.GetPropertyType(curved, i);
                if (type == ShaderUtil.ShaderPropertyType.TexEnv)
                {
                    if (normalProp == null && (l.Contains("normal") || l.Contains("bump"))) normalProp = n;
                    else if (metalProp == null && (l.Contains("metal") || l.Contains("smooth") || l.Contains("mask") || l.Contains("gloss") || l.Contains("spec"))) metalProp = n;
                    else if (baseProp == null && (l.Contains("tex") || l.Contains("base") || l.Contains("albedo") || l.Contains("main") || l.Contains("color") || l.Contains("diffuse"))) baseProp = n;
                }
                else if (type == ShaderUtil.ShaderPropertyType.Color)
                {
                    if (colorProp == null && (l.Contains("base") || l.Contains("color") || l.Contains("tint"))) colorProp = n;
                }
            }

            // 3) Carpeta
            string folder = null;
            if (Selection.activeObject != null)
            {
                string p = AssetDatabase.GetAssetPath(Selection.activeObject);
                if (!string.IsNullOrEmpty(p) && AssetDatabase.IsValidFolder(p)) folder = p;
            }
            if (folder == null && AssetDatabase.IsValidFolder("Assets/Suntail Village")) folder = "Assets/Suntail Village";
            if (folder == null) { Dlg("Selecciona la carpeta del pack en Project."); return; }

            // 4) Convertir
            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { folder });
            var converted = new List<Material>();
            int skipped = 0;
            try
            {
                AssetDatabase.StartAssetEditing();
                for (int gi = 0; gi < guids.Length; gi++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[gi]);
                    if (EditorUtility.DisplayCancelableProgressBar("Sprout · Curved World",
                        System.IO.Path.GetFileName(path), (float)gi / Mathf.Max(1, guids.Length))) break;

                    var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (mat == null) continue;

                    string sh = mat.shader != null ? mat.shader.name.ToLowerInvariant() : "";
                    string nm = mat.name.ToLowerInvariant();

                    // saltar transparentes y vegetación de recorte
                    if (sh.Contains("water") || nm.Contains("water") || sh.Contains("glass") || nm.Contains("glass") ||
                        nm.Contains("window") || HasAny(nm, "grass", "leaf", "leaves", "ivy", "foliage", "fern", "bush", "plant", "flower"))
                    { skipped++; continue; }

                    var tex = new Dictionary<string, Texture>();
                    var col = new Dictionary<string, Color>();
                    ReadSaved(mat, tex, col);

                    Texture albedo = First(tex, "_BaseMap", "_Albedo", "_MainTex", "_BaseColorMap");
                    Texture normal = First(tex, "_BumpMap", "_Normal", "_NormalMap");
                    Texture metal = First(tex, "_MetallicGlossMap", "_MetallicSmoothness", "_MaskMap");
                    Color color = FirstCol(col, "_BaseColor", "_Color", "_MainColor");
                    color.a = 1f;

                    mat.shader = curved;
                    if (baseProp != null && albedo != null) mat.SetTexture(baseProp, albedo);
                    if (normalProp != null && normal != null) mat.SetTexture(normalProp, normal);
                    if (metalProp != null && metal != null) mat.SetTexture(metalProp, metal);
                    if (colorProp != null) mat.SetColor(colorProp, color);

                    EditorUtility.SetDirty(mat);
                    converted.Add(mat);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
            }

            // 5) Meterlos en la lista del CurvedWorldOriginSetter (si no, no se curvan)
            int addedToSetter = AddToSetter(converted);

            Dlg($"Pasados a CurvedWorld: {converted.Count}\n" +
                $"Saltados (agua/cristal/vegetación): {skipped}\n" +
                $"Añadidos a CurvedWorldOriginSetter: {addedToSetter}\n\n" +
                $"Props detectadas -> base:{baseProp ?? "—"}  normal:{normalProp ?? "—"}  metal:{metalProp ?? "—"}\n\n" +
                (addedToSetter == 0 ? "⚠ No encontré CurvedWorldOriginSetter en la escena: añádelo y vuelve a darle, o no se curvarán." : "Listo: deberían curvarse como el resto."));
            Debug.Log($"[Sprout] CurvedWorld aplicado a {converted.Count}, en setter {addedToSetter}, saltados {skipped}.");
        }

        private static Shader FindCurvedShader()
        {
            Shader best = null;
            foreach (var g in AssetDatabase.FindAssets("CurvedWorld t:Shader"))
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                if (!p.EndsWith(".shadergraph")) continue;
                var s = AssetDatabase.LoadAssetAtPath<Shader>(p);
                if (s == null) continue;
                if (p.EndsWith("/CurvedWorld.shadergraph")) return s; // preferida exacta
                best = s;
            }
            return best;
        }

        private static int AddToSetter(List<Material> mats)
        {
            if (mats.Count == 0) return 0;
            foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (mb == null || mb.GetType().Name != "CurvedWorldOriginSetter") continue;
                var so = new SerializedObject(mb);
                var arr = so.FindProperty("curvedWorldMaterials");
                if (arr == null || !arr.isArray) return 0;

                var existing = new HashSet<Object>();
                for (int i = 0; i < arr.arraySize; i++) existing.Add(arr.GetArrayElementAtIndex(i).objectReferenceValue);

                int added = 0;
                foreach (var m in mats)
                {
                    if (existing.Contains(m)) continue;
                    arr.arraySize++;
                    arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = m;
                    existing.Add(m);
                    added++;
                }
                so.ApplyModifiedProperties();
                return added;
            }
            return 0;
        }

        // ── saved props ──
        private static void ReadSaved(Material mat, Dictionary<string, Texture> tex, Dictionary<string, Color> col)
        {
            var so = new SerializedObject(mat);
            var te = so.FindProperty("m_SavedProperties.m_TexEnvs");
            if (te != null)
                for (int i = 0; i < te.arraySize; i++)
                {
                    var e = te.GetArrayElementAtIndex(i);
                    string name = e.FindPropertyRelative("first").stringValue;
                    var t = e.FindPropertyRelative("second.m_Texture").objectReferenceValue as Texture;
                    if (!string.IsNullOrEmpty(name) && t != null && !tex.ContainsKey(name)) tex[name] = t;
                }
            var cs = so.FindProperty("m_SavedProperties.m_Colors");
            if (cs != null)
                for (int i = 0; i < cs.arraySize; i++)
                {
                    var e = cs.GetArrayElementAtIndex(i);
                    string name = e.FindPropertyRelative("first").stringValue;
                    if (!string.IsNullOrEmpty(name) && !col.ContainsKey(name)) col[name] = e.FindPropertyRelative("second").colorValue;
                }
        }

        private static Texture First(Dictionary<string, Texture> d, params string[] keys)
        { foreach (var k in keys) if (d.TryGetValue(k, out var v) && v != null) return v; return null; }
        private static Color FirstCol(Dictionary<string, Color> d, params string[] keys)
        { foreach (var k in keys) if (d.TryGetValue(k, out var v) && v.maxColorComponent > 0.01f) return v; return Color.white; }
        private static bool HasAny(string s, params string[] keys)
        { foreach (var k in keys) if (s.Contains(k)) return true; return false; }
        private static void Dlg(string m) => EditorUtility.DisplayDialog("Sprout · Curved World", m, "OK");
    }
}
#endif
