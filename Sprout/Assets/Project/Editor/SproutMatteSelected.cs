#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Sprout.EditorTools
{
    /// <summary>
    /// Deja MATE el/los material(es) del objeto SELECCIONADO (mismo arreglo que la florista): corta el
    /// brillo que muchos materiales leen del alfa de la textura, quita reflejos del entorno y specular.
    /// Selecciona la carta (o lo que sea) en la jerarquía o el prefab y dale al menú.
    ///
    /// Menú:  Tools/Sprout/Make Selected Matte
    /// </summary>
    public static class SproutMatteSelected
    {
        [MenuItem("Tools/Sprout/Make Selected Matte")]
        public static void Matte()
        {
            var sel = Selection.gameObjects;
            if (sel == null || sel.Length == 0)
            {
                EditorUtility.DisplayDialog("Sprout", "Selecciona primero el objeto (la carta) en la jerarquía o el prefab.", "OK");
                return;
            }

            int mats = 0;
            foreach (var go in sel)
                foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                    foreach (var mat in r.sharedMaterials)
                    {
                        if (mat == null) continue;

                        if (mat.HasProperty("_WorkflowMode")) mat.SetFloat("_WorkflowMode", 1f); // Metallic
                        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
                        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0f);
                        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0f);
                        if (mat.HasProperty("_SmoothnessTextureChannel")) mat.SetFloat("_SmoothnessTextureChannel", 1f); // Metallic Alpha, no Albedo
                        if (mat.HasProperty("_SpecColor")) mat.SetColor("_SpecColor", Color.black);

                        // No leer el brillo del alfa de la textura (la causa del brillo terco).
                        mat.DisableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
                        mat.DisableKeyword("_METALLICSPECGLOSSMAP");
                        mat.DisableKeyword("_SPECGLOSSMAP");

                        // Quitar reflejos del entorno y specular (lustre de porcelana).
                        if (mat.HasProperty("_EnvironmentReflections")) mat.SetFloat("_EnvironmentReflections", 0f);
                        if (mat.HasProperty("_SpecularHighlights")) mat.SetFloat("_SpecularHighlights", 0f);
                        mat.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
                        mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");

                        EditorUtility.SetDirty(mat);
                        mats++;
                    }

            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Sprout",
                mats > 0 ? $"{mats} material(es) en mate. Ya no debería brillar." : "El objeto no tiene materiales.", "OK");
            Debug.Log($"[Sprout] Materiales en mate: {mats}");
        }
    }
}
#endif
