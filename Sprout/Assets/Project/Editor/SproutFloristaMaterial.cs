#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Sprout.EditorTools
{
    /// <summary>
    /// Uso único: crea un material URP a partir de las texturas de Meshy de la florista
    /// (base + normal), mate (sin el brillo metálico del material por defecto), y lo asigna a
    /// los objetos de la escena cuyo nombre contenga "florista".
    ///
    /// Menú:  Tools/Sprout/Fix Florista Material
    /// </summary>
    public static class SproutFloristaMaterial
    {
        private const string Folder = "Assets/Art/Characters/florista_final/Florista/";
        private const string Base   = Folder + "Meshy_AI_Mushroom_Cap_Adventur_0612154838_texture.png";
        private const string Normal = Folder + "Meshy_AI_Mushroom_Cap_Adventur_0612154838_texture_normal.png";

        [MenuItem("Tools/Sprout/Fix Florista Material")]
        public static void Fix()
        {
            var baseTex = AssetDatabase.LoadAssetAtPath<Texture2D>(Base);
            if (baseTex == null)
            {
                EditorUtility.DisplayDialog("Sprout", "No encuentro la textura base en:\n" + Base, "OK");
                return;
            }

            // Asegurar que la normal se importa como mapa de normales.
            var nImp = AssetImporter.GetAtPath(Normal) as TextureImporter;
            if (nImp != null && nImp.textureType != TextureImporterType.NormalMap)
            {
                nImp.textureType = TextureImporterType.NormalMap;
                nImp.SaveAndReimport();
            }
            var normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(Normal);

            // Material URP/Lit mate.
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", baseTex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", baseTex);
            if (normalTex != null && mat.HasProperty("_BumpMap"))
            {
                mat.SetTexture("_BumpMap", normalTex);
                mat.EnableKeyword("_NORMALMAP");
            }
            if (mat.HasProperty("_Metallic"))   mat.SetFloat("_Metallic", 0f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.12f);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.12f);

            string matPath = Folder + "Florista_Mat.mat";
            AssetDatabase.CreateAsset(mat, AssetDatabase.GenerateUniqueAssetPath(matPath));
            AssetDatabase.SaveAssets();

            // Asignar a los renderers de la florista en la escena.
            int assigned = 0;
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!r.transform.root.name.ToLowerInvariant().Contains("florista") &&
                    !r.gameObject.name.ToLowerInvariant().Contains("florista"))
                    continue;
                var mats = new Material[r.sharedMaterials.Length == 0 ? 1 : r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                r.sharedMaterials = mats;
                EditorUtility.SetDirty(r);
                assigned++;
            }

            EditorUtility.DisplayDialog("Sprout",
                "Material 'Florista_Mat' creado (mate, con base + normal).\n\n" +
                (assigned > 0
                    ? $"Asignado a {assigned} renderer(s) de la florista en la escena."
                    : "No encontré la florista en la escena: arrastra Florista_Mat al modelo a mano.") +
                "\n\nSi sigue brillante, baja Smoothness en el material. Puedes borrar este archivo.",
                "OK");
            Debug.Log("[Sprout] Florista material creado y asignado a " + assigned + " renderers.");
        }
    }
}
#endif
