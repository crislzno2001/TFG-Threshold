#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Sprout.EditorTools
{
    /// <summary>
    /// Uso único: deja MATE el material de la florista. El modelo de Mixamo lee el brillo del canal
    /// alfa de la textura (Smoothness Source = Albedo Alpha), por eso sigue brillando aunque el slider
    /// esté a 0. Esto lo corta: workflow Metallic, Metallic 0, Smoothness 0 y sin leer el alfa.
    ///
    /// Menú:  Tools/Sprout/Make Florista Matte
    /// </summary>
    public static class SproutMatteFlorista
    {
        [MenuItem("Tools/Sprout/Make Florista Matte")]
        public static void Matte()
        {
            int mats = 0;
            foreach (var smr in Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                string rn = smr.transform.root.name.ToLowerInvariant();
                if (!rn.Contains("angry") && !rn.Contains("florista")) continue;

                foreach (var mat in smr.sharedMaterials)
                {
                    if (mat == null) continue;
                    if (mat.HasProperty("_WorkflowMode")) mat.SetFloat("_WorkflowMode", 1f);   // 1 = Metallic
                    if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
                    if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0f);
                    if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0f);
                    if (mat.HasProperty("_SmoothnessTextureChannel")) mat.SetFloat("_SmoothnessTextureChannel", 1f);
                    if (mat.HasProperty("_SpecColor")) mat.SetColor("_SpecColor", Color.black);
                    // dejar de leer el brillo del alfa de la textura
                    mat.DisableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
                    mat.DisableKeyword("_METALLICSPECGLOSSMAP");
                    mat.DisableKeyword("_SPECGLOSSMAP");
                    EditorUtility.SetDirty(mat);
                    mats++;
                }
            }
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Sprout",
                mats > 0
                    ? $"Material(es) de la florista en mate ({mats}). Ya no debería brillar."
                    : "No encontré la florista (Angry/Florista) en la escena.",
                "OK");
            Debug.Log("[Sprout] Florista material -> mate. Materiales tocados: " + mats);
        }
    }
}
#endif
