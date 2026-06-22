#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Sprout.EditorTools
{
    /// <summary>
    /// Uso único: pone TINTE VERDE al césped/hojas/arbustos que se quedaron blancos.
    /// En este pack las texturas de vegetación son grises a propósito y el verde lo daba el color del
    /// material (_MainColor). Los materiales que se rompieron al pasarlos a URP/Lit perdieron ese verde,
    /// así que aquí se lo volvemos a poner en _BaseColor (multiplica la textura -> se ve verde).
    ///
    /// Solo afecta a nombres de césped/hojas. NO toca ivy (su textura ya es verde), madera, troncos,
    /// ni cerezos en flor (blossom/cherry/flower), que deben quedarse claros.
    ///
    /// Cambia GREEN abajo si quieres otro tono.
    /// Trabaja sobre la carpeta seleccionada en Project; si no, "Assets/Suntail Village".
    /// Menú:  Tools/Sprout/Tint Grass and Leaves Green
    /// </summary>
    public static class SproutTintFoliage
    {
        // Verde por defecto del pack (Raygeas/Suntail Foliage _MainColor). Cámbialo a tu gusto.
        private static readonly Color GREEN = new Color(0.20f, 0.50f, 0.42f, 1f);

        private static readonly string[] Include =
            { "grass", "leaf", "leaves", "bush", "fern", "hedge", "shrub", "foliage", "plant", "weed", "reed", "cattail" };
        private static readonly string[] Exclude =
            { "ivy", "blossom", "cherry", "sakura", "flower", "bark", "wood", "plank", "trunk", "branch", "stone", "rock", "dirt", "ground" };

        [MenuItem("Tools/Sprout/Tint Grass and Leaves Green")]
        public static void Tint()
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
                EditorUtility.DisplayDialog("Sprout", "Selecciona la carpeta del pack en Project.", "OK");
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { folder });
            int n = 0;
            var done = new System.Text.StringBuilder();

            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (var g in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(g);
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (mat == null) continue;

                    string nm = mat.name.ToLowerInvariant();
                    if (!Has(nm, Include) || Has(nm, Exclude)) continue;

                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", GREEN);
                    if (mat.HasProperty("_Color")) mat.SetColor("_Color", GREEN);
                    EditorUtility.SetDirty(mat);
                    n++;
                    if (n <= 25) done.Append("\n· ").Append(mat.name);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
            }

            EditorUtility.DisplayDialog("Sprout",
                $"Verde aplicado a {n} materiales de vegetación:{done}\n\n" +
                "Si alguno NO debía ir verde, dime el nombre y lo saco de la lista. " +
                "Si quieres otro tono, cambia GREEN en SproutTintFoliage.cs.", "OK");
            Debug.Log($"[Sprout] Tinte verde aplicado a {n} materiales.");
        }

        private static bool Has(string s, string[] keys)
        { foreach (var k in keys) if (s.Contains(k)) return true; return false; }
    }
}
#endif
