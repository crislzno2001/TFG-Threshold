#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Sprout.EditorTools
{
    /// <summary>
    /// Uso único: apaga el "glow de bombilla" de la florista, sea cual sea la causa:
    /// 1) Apaga la Emisión del material (keyword, color negro, sin emission map).
    /// 2) Si el Base Map está vacío (renderiza blanco puro), le mete texture_0.
    /// 3) Desactiva cualquier componente Light pegado al personaje o a sus hijos.
    ///
    /// Menú:  Tools/Sprout/Kill Florista Glow
    /// </summary>
    public static class SproutKillGlow
    {
        [MenuItem("Tools/Sprout/Kill Florista Glow")]
        public static void Kill()
        {
            // Buscar texture_0 dentro de la carpeta de la florista
            Texture2D tex = null;
            foreach (var guid in AssetDatabase.FindAssets("texture_0 t:Texture2D"))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid).ToLowerInvariant();
                if (p.Contains("florista") || p.Contains("angry"))
                {
                    tex = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guid));
                    break;
                }
            }

            int matsTouched = 0, lightsOff = 0;
            foreach (var smr in Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                string rn = smr.transform.root.name.ToLowerInvariant();
                if (!rn.Contains("angry") && !rn.Contains("florista")) continue;

                foreach (var mat in smr.sharedMaterials)
                {
                    if (mat == null) continue;

                    // 1. Apagar emisión
                    mat.DisableKeyword("_EMISSION");
                    if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", Color.black);
                    if (mat.HasProperty("_EmissionMap")) mat.SetTexture("_EmissionMap", null);
                    mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;

                    // 2. Base map si está vacío
                    if (tex != null && mat.HasProperty("_BaseMap") && mat.GetTexture("_BaseMap") == null)
                        mat.SetTexture("_BaseMap", tex);
                    if (tex != null && mat.HasProperty("_MainTex") && mat.GetTexture("_MainTex") == null)
                        mat.SetTexture("_MainTex", tex);

                    // 3. Asegurar mate también
                    if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
                    if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0f);

                    EditorUtility.SetDirty(mat);
                    matsTouched++;
                }

                // 4. Desactivar luces pegadas al personaje
                foreach (var l in smr.transform.root.GetComponentsInChildren<Light>(true))
                {
                    l.enabled = false;
                    EditorUtility.SetDirty(l);
                    lightsOff++;
                }
            }

            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Sprout",
                matsTouched == 0
                    ? "No encontré la florista (Angry/Florista) en la escena. ¿Está el SkinnedMeshRenderer dentro de un objeto cuyo nombre contenga 'angry' o 'florista'?"
                    : $"Glow apagado:\n• Emisión OFF en {matsTouched} material(es)\n• {lightsOff} componente(s) Light desactivado(s)\n• Base Map asignado si estaba vacío\n\nSi AÚN brilla: apaga la Directional Light un momento — si sigue iluminada a oscuras, dime y miramos el shader.",
                "OK");
            Debug.Log($"[Sprout] Kill glow: mats {matsTouched}, lights {lightsOff}, tex {(tex != null)}");
        }
    }
}
#endif
