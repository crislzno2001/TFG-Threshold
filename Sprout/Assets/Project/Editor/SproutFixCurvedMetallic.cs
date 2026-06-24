#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Sprout.EditorTools
{
    /// <summary>
    /// Uso único: arregla los materiales CurvedWorld que se ven NEGROS porque su hueco de
    /// _MetallicSmoothness está vacío. En Shader Graph, un Sample Texture 2D sin textura devuelve BLANCO,
    /// así que Metallic=1 y Smoothness=1 -> metal puro sin reflejo -> negro.
    ///
    /// Solución: crea una textura 1x1 (0,0,0,0) = metallic 0 / smoothness 0 (mate) y la pone en el hueco
    /// _MetallicSmoothness de todos los materiales curved que lo tengan vacío.
    ///
    /// Carpeta seleccionada en Project, o "Assets/Suntail Village" por defecto.
    /// Menú:  Tools/Sprout/Fix Curved Black (empty metallic)
    /// </summary>
    public static class SproutFixCurvedMetallic
    {
        private const string TexRel = "Assets/Project/Settings/EmptyMetallicSmoothness.png";

        [MenuItem("Tools/Sprout/Fix Curved Black (empty metallic)")]
        public static void Fix()
        {
            var emptyMet = EnsureEmptyTexture();
            if (emptyMet == null) { Dlg("No pude crear la textura de relleno."); return; }

            string folder = SelectedFolder();
            if (folder == null) { Dlg("Selecciona la carpeta del pack en Project."); return; }

            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { folder });
            int fixedN = 0, scanned = 0;
            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (var g in guids)
                {
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g));
                    if (mat == null || mat.shader == null) continue;
                    if (!mat.shader.name.ToLowerInvariant().Contains("curvedworld")) continue;
                    scanned++;

                    // buscar la propiedad de metallic/smoothness del shader
                    string prop = FindMetalProp(mat.shader);
                    if (prop == null) continue;
                    if (mat.GetTexture(prop) != null) continue; // ya tiene -> no tocar

                    mat.SetTexture(prop, emptyMet);
                    EditorUtility.SetDirty(mat);
                    fixedN++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
            }

            Dlg($"Materiales curved revisados: {scanned}\nArreglados (metallic vacío -> negro): {fixedN}\n\n" +
                "Deberían dejar de verse negros. Los que ya tenían su textura de metallic no se han tocado.");
            Debug.Log($"[Sprout] Fix curved black: {fixedN}/{scanned}");
        }

        private static Texture2D EnsureEmptyTexture()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(TexRel);
            if (existing != null) return existing;

            if (!AssetDatabase.IsValidFolder("Assets/Project")) AssetDatabase.CreateFolder("Assets", "Project");
            if (!AssetDatabase.IsValidFolder("Assets/Project/Settings")) AssetDatabase.CreateFolder("Assets/Project", "Settings");

            var t = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            t.SetPixel(0, 0, new Color(0f, 0f, 0f, 0f)); // R=metallic 0, A=smoothness 0
            t.Apply();
            string abs = Path.Combine(UnityEngine.Application.dataPath, "Project/Settings/EmptyMetallicSmoothness.png");
            File.WriteAllBytes(abs, t.EncodeToPNG());
            Object.DestroyImmediate(t);

            AssetDatabase.ImportAsset(TexRel, ImportAssetOptions.ForceUpdate);
            var imp = AssetImporter.GetAtPath(TexRel) as TextureImporter;
            if (imp != null)
            {
                imp.sRGBTexture = false; // es DATA, no color
                imp.alphaSource = TextureImporterAlphaSource.FromInput;
                imp.alphaIsTransparency = false;
                imp.mipmapEnabled = false;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(TexRel);
        }

        private static string FindMetalProp(Shader sh)
        {
            int c = ShaderUtil.GetPropertyCount(sh);
            for (int i = 0; i < c; i++)
            {
                if (ShaderUtil.GetPropertyType(sh, i) != ShaderUtil.ShaderPropertyType.TexEnv) continue;
                string l = ShaderUtil.GetPropertyName(sh, i).ToLowerInvariant();
                if (l.Contains("metal") || l.Contains("smooth") || l.Contains("mask") || l.Contains("gloss"))
                    return ShaderUtil.GetPropertyName(sh, i);
            }
            return null;
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

        private static void Dlg(string m) => EditorUtility.DisplayDialog("Sprout · Fix Curved Black", m, "OK");
    }
}
#endif
