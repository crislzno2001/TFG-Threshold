#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Sprout.EditorTools
{
    /// <summary>
    /// Uso único: aplica el shader CurvedWorld_Cutout (variante con recorte alfa + doble cara) a la
    /// VEGETACIÓN (grass/leaf/ivy/bush/fern/plant/flower), para que se curve con el suelo igual que el
    /// resto SIN perder el recorte de las hojas. Conserva textura, normal, metallic y el tinte (verde).
    /// Mete los materiales en la lista del CurvedWorldOriginSetter para que se curven.
    ///
    /// Necesita que antes hayas creado el shader 'CurvedWorld_Cutout' (duplicado de CurvedWorld con
    /// Alpha Clipping ON, Render Face Both, y A del albedo -> Alpha).
    ///
    /// Menú:  Tools/Sprout/Apply Curved World (Cutout) to Foliage
    /// </summary>
    public static class SproutApplyCurvedFoliage
    {
        private static readonly string[] Foliage =
            { "grass", "leaf", "leaves", "ivy", "foliage", "fern", "bush", "plant", "flower", "weed", "reed" };

        [MenuItem("Tools/Sprout/Apply Curved World (Cutout) to Foliage")]
        public static void Apply()
        {
            Shader curved = FindShader("CurvedWorld_Cutout", "cutout");
            if (curved == null)
            {
                Dlg("No encuentro el shader 'CurvedWorld_Cutout'.\n\n" +
                    "Créalo primero: duplica CurvedWorld.shadergraph, renómbralo CurvedWorld_Cutout, " +
                    "activa Alpha Clipping, Render Face = Both, y conecta la A del albedo al bloque Alpha.");
                return;
            }

            // props
            string baseProp = null, normalProp = null, metalProp = null, colorProp = null;
            int count = ShaderUtil.GetPropertyCount(curved);
            for (int i = 0; i < count; i++)
            {
                string n = ShaderUtil.GetPropertyName(curved, i);
                string l = n.ToLowerInvariant();
                var t = ShaderUtil.GetPropertyType(curved, i);
                if (t == ShaderUtil.ShaderPropertyType.TexEnv)
                {
                    if (normalProp == null && (l.Contains("normal") || l.Contains("bump"))) normalProp = n;
                    else if (metalProp == null && (l.Contains("metal") || l.Contains("smooth") || l.Contains("mask") || l.Contains("gloss"))) metalProp = n;
                    else if (baseProp == null && (l.Contains("tex") || l.Contains("base") || l.Contains("albedo") || l.Contains("main") || l.Contains("color") || l.Contains("diffuse"))) baseProp = n;
                }
                else if (t == ShaderUtil.ShaderPropertyType.Color && colorProp == null && (l.Contains("base") || l.Contains("color") || l.Contains("tint")))
                    colorProp = n;
            }

            string folder = SelectedFolder();
            if (folder == null) { Dlg("Selecciona la carpeta del pack en Project."); return; }

            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { folder });
            var converted = new List<Material>();
            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (var g in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(g);
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (mat == null) continue;

                    string nm = mat.name.ToLowerInvariant();
                    if (!HasAny(nm, Foliage)) continue; // SOLO vegetación

                    var tex = new Dictionary<string, Texture>();
                    var col = new Dictionary<string, Color>();
                    ReadSaved(mat, tex, col);

                    Texture albedo = First(tex, "_BaseMap", "_Albedo", "_MainTex");
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
                AssetDatabase.SaveAssets();
            }

            int added = AddToSetter(converted);
            Dlg($"Vegetación pasada a CurvedWorld_Cutout: {converted.Count}\n" +
                $"Añadidos al CurvedWorldOriginSetter: {added}\n\n" +
                $"Props -> base:{baseProp ?? "—"} normal:{normalProp ?? "—"} metal:{metalProp ?? "—"}\n\n" +
                (added == 0 ? "⚠ No encontré CurvedWorldOriginSetter en la escena." : "Ahora la hierba/hojas se curvan y mantienen el recorte."));
            Debug.Log($"[Sprout] CurvedWorld cutout aplicado a {converted.Count}, en setter {added}.");
        }

        // ── helpers (compartidos en espíritu con SproutApplyCurvedWorld) ──
        private static Shader FindShader(string exactName, params string[] contains)
        {
            Shader best = null;
            foreach (var g in AssetDatabase.FindAssets(exactName + " t:Shader"))
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                if (!p.EndsWith(".shadergraph")) continue;
                var s = AssetDatabase.LoadAssetAtPath<Shader>(p);
                if (s == null) continue;
                if (p.EndsWith("/" + exactName + ".shadergraph")) return s;
                best = s;
            }
            if (best == null)
                foreach (var g in AssetDatabase.FindAssets("t:Shader"))
                {
                    string p = AssetDatabase.GUIDToAssetPath(g).ToLowerInvariant();
                    if (!p.EndsWith(".shadergraph")) continue;
                    foreach (var c in contains) if (p.Contains(c) && p.Contains("curved")) return AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(g));
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
                var have = new HashSet<Object>();
                for (int i = 0; i < arr.arraySize; i++) have.Add(arr.GetArrayElementAtIndex(i).objectReferenceValue);
                int added = 0;
                foreach (var m in mats)
                {
                    if (have.Contains(m)) continue;
                    arr.arraySize++;
                    arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = m;
                    have.Add(m); added++;
                }
                so.ApplyModifiedProperties();
                return added;
            }
            return 0;
        }

        private static string SelectedFolder()
        {
            if (Selection.activeObject != null)
            {
                string p = AssetDatabase.GetAssetPath(Selection.activeObject);
                if (!string.IsNullOrEmpty(p) && AssetDatabase.IsValidFolder(p)) return p;
            }
            return AssetDatabase.IsValidFolder("Assets/Suntail Village") ? "Assets/Suntail Village" : null;
        }

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
        private static void Dlg(string m) => EditorUtility.DisplayDialog("Sprout · Curved Foliage", m, "OK");
    }
}
#endif
