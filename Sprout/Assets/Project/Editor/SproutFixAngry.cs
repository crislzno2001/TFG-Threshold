#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Sprout.EditorTools
{
    /// <summary>
    /// Uso único para la florista de Mixamo (Angryç): extrae su textura embebida (para que deje de
    /// salir blanca) y le asigna el Animator Controller "Florista_Anim" + su avatar en la escena.
    ///
    /// Menú:  Tools/Sprout/Fix Angryç (texture + animator)
    /// </summary>
    public static class SproutFixAngry
    {
        private const string Dir = "Assets/Art/Characters/florista_final/";

        [MenuItem("Tools/Sprout/Fix Florista Mixamo (texture + animator)")]
        public static void Fix()
        {
            // Localizar el FBX (con o sin ç)
            string fbx = Dir + "Angryç.fbx";
            if (AssetImporter.GetAtPath(fbx) == null) fbx = Dir + "Angry.fbx";
            var imp = AssetImporter.GetAtPath(fbx) as ModelImporter;
            if (imp == null) { Msg("No encuentro Angryç.fbx / Angry.fbx en\n" + Dir); return; }

            // 1. Extraer la textura embebida y reimportar para que el material la use
            string texFolder = Dir + "Angry_Textures";
            if (!AssetDatabase.IsValidFolder(texFolder))
                AssetDatabase.CreateFolder(Dir.TrimEnd('/'), "Angry_Textures");
            bool extracted = imp.ExtractTextures(texFolder);
            AssetDatabase.Refresh();
            imp.SaveAndReimport();

            // 2. Animator Controller + avatar en la escena
            var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Dir + "Florista_Anim.controller");
            Avatar avatar = null;
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(fbx))
                if (o is Avatar a) avatar = a;

            int n = 0;
            foreach (var smr in Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                string rn = smr.transform.root.name.ToLowerInvariant();
                if (!rn.Contains("angry") && !rn.Contains("florista")) continue;
                var go = smr.transform.root.gameObject;
                var anim = go.GetComponent<Animator>();
                if (anim == null) anim = Undo.AddComponent<Animator>(go);
                if (ctrl != null) anim.runtimeAnimatorController = ctrl;
                if (avatar != null) anim.avatar = avatar;
                EditorUtility.SetDirty(anim);
                n++;
                break;
            }

            Msg((extracted ? "Textura extraída a Angry_Textures (ya no debería salir blanca)."
                           : "No había textura embebida que extraer (mira la pestaña Materials del FBX).") +
                "\n\n" +
                (ctrl == null ? "No encontré Florista_Anim.controller (ejecuta antes 'Setup Florista Animations')."
                 : n > 0 ? "Animator Controller 'Florista_Anim' y avatar asignados a la florista."
                         : "No encontré la florista en la escena: arrastra Florista_Anim a su Animator a mano.") +
                "\n\nPuedes borrar este archivo.");
            Debug.Log("[Sprout] Fix Angry: textura " + extracted + ", animator asignado a " + n);
        }

        private static void Msg(string m) => EditorUtility.DisplayDialog("Sprout", m, "OK");
    }
}
#endif
