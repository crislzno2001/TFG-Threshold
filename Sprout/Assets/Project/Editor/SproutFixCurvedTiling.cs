#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Sprout.EditorTools
{
    /// <summary>
    /// Uso único: arregla los materiales CurvedWorld que se ven PLANOS/de un solo color (farola marrón
    /// lisa, hojas blancas, corteza plana) porque su propiedad Tiling quedó en (0,0). Con Tiling 0 la UV
    /// se colapsa y la textura se muestrea en un solo píxel. Esto lo pone a (1,1).
    ///
    /// Carpeta seleccionada en Project, o "Assets/Suntail Village" por defecto.
    /// Menú:  Tools/Sprout/Fix Curved Tiling (0 -> 1)
    /// </summary>
    public static class SproutFixCurvedTiling
    {
        [MenuItem("Tools/Sprout/Fix Curved Tiling (0 -> 1)")]
        public static void Fix()
        {
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

                    string prop = FindTilingProp(mat.shader);
                    if (prop == null) continue;

                    Vector4 t = mat.GetVector(prop);
                    if (Mathf.Abs(t.x) < 0.0001f && Mathf.Abs(t.y) < 0.0001f)
                    {
                        mat.SetVector(prop, new Vector4(1f, 1f, t.z, t.w));
                        EditorUtility.SetDirty(mat);
                        fixedN++;
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
            }

            Dlg($"Materiales curved revisados: {scanned}\nTiling 0 -> 1 arreglados: {fixedN}\n\n" +
                "Las texturas planas (farola, corteza, hojas) deberían recuperar su detalle.\n\n" +
                "TIP: para que no vuelva a pasar con materiales nuevos, abre el shader y pon el Default de " +
                "la propiedad 'Tiling' en 1,1 (o 1).");
            Debug.Log($"[Sprout] Fix curved tiling: {fixedN}/{scanned}");
        }

        private static string FindTilingProp(Shader sh)
        {
            int c = ShaderUtil.GetPropertyCount(sh);
            for (int i = 0; i < c; i++)
            {
                var type = ShaderUtil.GetPropertyType(sh, i);
                if (type != ShaderUtil.ShaderPropertyType.Vector && type != ShaderUtil.ShaderPropertyType.Float) continue;
                if (ShaderUtil.GetPropertyName(sh, i).ToLowerInvariant().Contains("tiling"))
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

        private static void Dlg(string m) => EditorUtility.DisplayDialog("Sprout · Fix Curved Tiling", m, "OK");
    }
}
#endif
