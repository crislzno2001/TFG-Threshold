#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Sprout.EditorTools
{
    /// <summary>
    /// Uso único: pone al material de la florista el MISMO shader curvado (Curved World) que usa el
    /// resto del mundo (el del personaje viejo "Chris"), para que se curve igual que el suelo y deje
    /// de "flotar". Re-asigna su textura (texture_0) al base map del shader nuevo.
    ///
    /// Menú:  Tools/Sprout/Florista uses Curved World shader
    /// </summary>
    public static class SproutCurvedFlorista
    {
        [MenuItem("Tools/Sprout/Florista uses Curved World shader")]
        public static void Apply()
        {
            // 1. Shader curvado: el del PLAYER VIEJO (Player.prefab), que ya se curvaba bien.
            Shader curved = null;
            var oldPlayer = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Project/3.Presentation/Player/Prefabs/Player.prefab");
            if (oldPlayer != null)
            {
                foreach (var r in oldPlayer.GetComponentsInChildren<Renderer>(true))
                {
                    if (r.sharedMaterial == null || r.sharedMaterial.shader == null) continue;
                    curved = r.sharedMaterial.shader;
                    if (curved.name.ToLowerInvariant().Contains("curved")) break; // preferimos uno curvado
                }
            }
            // 2. Fallback: cualquier material curvado del entorno (p. ej. el suelo).
            if (curved == null || !curved.name.ToLowerInvariant().Contains("curved"))
            {
                foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/Art/Environment" }))
                {
                    var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                    if (m != null && m.shader != null && m.shader.name.ToLowerInvariant().Contains("curved"))
                    { curved = m.shader; break; }
                }
            }
            if (curved == null) { Dlg("No encuentro un shader curvado (ni en el Player viejo ni en el entorno)."); return; }

            // 2. Textura de la florista
            Texture2D tex = null;
            foreach (var guid in AssetDatabase.FindAssets("texture_0 t:Texture2D"))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid).ToLowerInvariant();
                if (p.Contains("florista") || p.Contains("angry"))
                { tex = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guid)); break; }
            }

            // 3. Propiedad de textura base del shader curvado
            string baseProp = FirstBaseTextureProp(curved);

            // 4. Aplicar a los materiales de la florista (busca por la cadena de nombres)
            int n = 0;
            foreach (var smr in Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!NameChainContains(smr.transform, "angry", "florista")) continue;
                foreach (var mat in smr.sharedMaterials)
                {
                    if (mat == null) continue;
                    mat.shader = curved;
                    if (tex != null && baseProp != null) mat.SetTexture(baseProp, tex);
                    if (tex != null && mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
                    if (tex != null && mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
                    EditorUtility.SetDirty(mat);
                    n++;
                }
            }

            AssetDatabase.SaveAssets();
            Dlg(n > 0
                ? $"Material de la florista cambiado al shader curvado: {curved.name}\n" +
                  $"Textura puesta en: {(baseProp ?? "_BaseMap")}.\n\nAhora debería curvarse igual que el suelo (no flotar)."
                : "No encontré la florista en la escena (busqué objetos cuyo nombre o el de un padre contenga 'angry'/'florista').");
            Debug.Log($"[Sprout] Florista -> shader curvado {(curved != null ? curved.name : "?")}, materiales: {n}");
        }

        private static string FirstBaseTextureProp(Shader sh)
        {
            int count = ShaderUtil.GetPropertyCount(sh);
            // primero una que suene a base/albedo/color
            for (int i = 0; i < count; i++)
            {
                if (ShaderUtil.GetPropertyType(sh, i) != ShaderUtil.ShaderPropertyType.TexEnv) continue;
                string n = ShaderUtil.GetPropertyName(sh, i).ToLowerInvariant();
                if (n.Contains("base") || n.Contains("albedo") || n.Contains("main") || n.Contains("color") || n.Contains("diffuse"))
                    return ShaderUtil.GetPropertyName(sh, i);
            }
            // si no, la primera textura que haya
            for (int i = 0; i < count; i++)
                if (ShaderUtil.GetPropertyType(sh, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                    return ShaderUtil.GetPropertyName(sh, i);
            return null;
        }

        private static bool NameChainContains(Transform t, params string[] keys)
        {
            while (t != null)
            {
                string n = t.name.ToLowerInvariant();
                foreach (var k in keys) if (n.Contains(k)) return true;
                t = t.parent;
            }
            return false;
        }

        private static void Dlg(string m) => EditorUtility.DisplayDialog("Sprout", m, "OK");
    }
}
#endif
