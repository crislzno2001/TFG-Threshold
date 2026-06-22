#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Sprout.EditorTools
{
    /// <summary>
    /// Uso único: arregla AGUA y VENTANAS/CRISTAL de un pack Built-in que usan GrabPass (refracción),
    /// algo que NO existe en URP -> salen rosas y llenan la consola de errores
    /// "ShaderLab::GrabPasses::ApplyGrabPassMainThread can't be called from a job thread".
    ///
    /// No se pueden portar con refracción, así que los pasa a "Universal Render Pipeline/Lit" en modo
    /// TRANSPARENTE (sin refracción, pero con buen aspecto y SIN errores):
    ///   - Agua  -> azul translúcido, brillante, con su normal de olas si la tiene.
    ///   - Cristal/ventana -> tinte claro muy translúcido y brillante.
    ///
    /// Detecta por nombre del shader o del material: water/glass/window/refract/grab/ice/crystal.
    /// Trabaja sobre la carpeta seleccionada en Project; si no, sobre "Assets/Suntail Village".
    /// Menú:  Tools/Sprout/Fix Water and Glass (URP transparent)
    /// </summary>
    public static class SproutFixWaterGlass
    {
        [MenuItem("Tools/Sprout/Fix Water and Glass (URP transparent)")]
        public static void Fix()
        {
            string folder = null;
            if (Selection.activeObject != null)
            {
                string p = AssetDatabase.GetAssetPath(Selection.activeObject);
                if (!string.IsNullOrEmpty(p) && AssetDatabase.IsValidFolder(p)) folder = p;
            }
            if (folder == null && AssetDatabase.IsValidFolder("Assets/Suntail Village")) folder = "Assets/Suntail Village";
            if (folder == null)
            {
                EditorUtility.DisplayDialog("Sprout",
                    "Selecciona en Project la carpeta del pack y vuelve a darle.", "OK");
                return;
            }

            var urp = Shader.Find("Universal Render Pipeline/Lit");
            if (urp == null) { EditorUtility.DisplayDialog("Sprout", "No encuentro URP/Lit.", "OK"); return; }

            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { folder });
            int water = 0, glass = 0;

            try
            {
                AssetDatabase.StartAssetEditing();
                for (int gi = 0; gi < guids.Length; gi++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[gi]);
                    if (EditorUtility.DisplayCancelableProgressBar("Sprout · Water/Glass",
                        System.IO.Path.GetFileName(path), (float)gi / Mathf.Max(1, guids.Length))) break;

                    var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (mat == null) continue;

                    string sh = mat.shader != null ? mat.shader.name.ToLowerInvariant() : "";
                    string nm = mat.name.ToLowerInvariant();
                    bool isWater = sh.Contains("water") || nm.Contains("water");
                    bool isGlass = sh.Contains("glass") || nm.Contains("glass") || nm.Contains("window")
                                || sh.Contains("window") || nm.Contains("refract") || sh.Contains("refract")
                                || nm.Contains("ice") || nm.Contains("crystal");
                    if (!isWater && !isGlass) continue;

                    // Recuperar tinte/normal de los datos guardados
                    var tex = new Dictionary<string, Texture>();
                    var col = new Dictionary<string, Color>();
                    ReadSaved(mat, tex, col);
                    Texture normal = First(tex, "_WavesNormal", "_Normal", "_BumpMap", "_NormalMap");

                    mat.shader = urp;
                    MakeTransparent(mat);
                    mat.SetFloat("_Metallic", 0f);

                    if (isWater)
                    {
                        Color c = FirstCol(col, new Color(0.10f, 0.45f, 0.65f), "_Color1", "_Color2", "_Color", "_BaseColor");
                        c.a = 0.75f;
                        mat.SetColor("_BaseColor", c);
                        mat.SetFloat("_Smoothness", 0.92f);
                        if (normal != null) { mat.SetTexture("_BumpMap", normal); mat.EnableKeyword("_NORMALMAP"); }
                        water++;
                    }
                    else
                    {
                        Color c = FirstCol(col, new Color(0.85f, 0.92f, 1f), "_Color", "_BaseColor", "_TintColor");
                        c.a = 0.28f;
                        mat.SetColor("_BaseColor", c);
                        mat.SetFloat("_Smoothness", 0.95f);
                        glass++;
                    }

                    EditorUtility.SetDirty(mat);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
            }

            EditorUtility.DisplayDialog("Sprout",
                $"Agua convertida: {water}\nCristal/ventanas convertidos: {glass}\n\n" +
                "Ya no son GrabPass -> los 724 errores de la consola deberían parar (dale a Clear).\n" +
                "Pierden la refracción real, pero se ven como agua/cristal translúcido.", "OK");
            Debug.Log($"[Sprout] Water/Glass -> URP transparent. Agua {water}, cristal {glass}");
        }

        private static void MakeTransparent(Material mat)
        {
            mat.SetFloat("_Surface", 1f);             // 0 opaco, 1 transparente
            mat.SetFloat("_Blend", 0f);               // alpha
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent; // 3000
        }

        private static void ReadSaved(Material mat, Dictionary<string, Texture> tex, Dictionary<string, Color> col)
        {
            var so = new SerializedObject(mat);
            var texEnvs = so.FindProperty("m_SavedProperties.m_TexEnvs");
            if (texEnvs != null)
                for (int i = 0; i < texEnvs.arraySize; i++)
                {
                    var e = texEnvs.GetArrayElementAtIndex(i);
                    string name = e.FindPropertyRelative("first").stringValue;
                    var t = e.FindPropertyRelative("second.m_Texture").objectReferenceValue as Texture;
                    if (!string.IsNullOrEmpty(name) && t != null && !tex.ContainsKey(name)) tex[name] = t;
                }
            var colors = so.FindProperty("m_SavedProperties.m_Colors");
            if (colors != null)
                for (int i = 0; i < colors.arraySize; i++)
                {
                    var e = colors.GetArrayElementAtIndex(i);
                    string name = e.FindPropertyRelative("first").stringValue;
                    if (!string.IsNullOrEmpty(name) && !col.ContainsKey(name))
                        col[name] = e.FindPropertyRelative("second").colorValue;
                }
        }

        private static Texture First(Dictionary<string, Texture> d, params string[] keys)
        { foreach (var k in keys) if (d.TryGetValue(k, out var v) && v != null) return v; return null; }

        private static Color FirstCol(Dictionary<string, Color> d, Color fallback, params string[] keys)
        { foreach (var k in keys) if (d.TryGetValue(k, out var v) && v.maxColorComponent > 0.01f) return v; return fallback; }
    }
}
#endif
