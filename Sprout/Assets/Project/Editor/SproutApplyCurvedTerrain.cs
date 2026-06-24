#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Sprout.EditorTools
{
    /// <summary>
    /// Uso único: pone el shader CurvedWorld_Terrain en el/los Terrain de la escena (el suelo de Unity),
    /// para que el terreno se curve igual que casas, props y vegetación. Crea un material con ese shader,
    /// lo asigna al campo Material del Terrain y lo mete en la lista del CurvedWorldOriginSetter.
    ///
    /// Las texturas del suelo (terrain layers/splatmaps) las sigue gestionando el propio Terrain, no hay
    /// que asignarlas a mano.
    ///
    /// Menú:  Tools/Sprout/Apply Curved World to Terrain
    /// </summary>
    public static class SproutApplyCurvedTerrain
    {
        private const string Dir = "Assets/Project/Settings";
        private const string MatPath = Dir + "/CurvedWorld_Terrain_Mat.mat";

        [MenuItem("Tools/Sprout/Apply Curved World to Terrain")]
        public static void Apply()
        {
            Shader curved = FindTerrainShader();
            if (curved == null) { Dlg("No encuentro el shader 'CurvedWorld_Terrain' (.shadergraph)."); return; }

            var terrains = Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (terrains.Length == 0) { Dlg("No hay ningún Terrain en la escena."); return; }

            if (!AssetDatabase.IsValidFolder("Assets/Project")) AssetDatabase.CreateFolder("Assets", "Project");
            if (!AssetDatabase.IsValidFolder(Dir)) AssetDatabase.CreateFolder("Assets/Project", "Settings");

            var mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
            if (mat == null)
            {
                mat = new Material(curved) { name = "CurvedWorld_Terrain_Mat" };
                AssetDatabase.CreateAsset(mat, MatPath);
            }
            else mat.shader = curved;
            EditorUtility.SetDirty(mat);

            int n = 0;
            foreach (var t in terrains)
            {
                Undo.RecordObject(t, "Apply Curved Terrain");
                t.materialTemplate = mat;
                EditorUtility.SetDirty(t);
                n++;
            }

            int added = AddToSetter(new List<Material> { mat });

            AssetDatabase.SaveAssets();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Dlg($"Material de terreno curvado aplicado a {n} Terrain(s).\n" +
                $"Añadido al CurvedWorldOriginSetter: {added}\n\n" +
                (added == 0 ? "⚠ No encontré CurvedWorldOriginSetter en la escena (sin él el suelo no se curva)."
                            : "El suelo debería curvarse ya con el resto del mundo."));
            Debug.Log($"[Sprout] Curved terrain aplicado a {n} terrenos, setter {added}.");
        }

        private static Shader FindTerrainShader()
        {
            foreach (var g in AssetDatabase.FindAssets("CurvedWorld_Terrain t:Shader"))
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                if (!p.EndsWith(".shadergraph")) continue;
                var s = AssetDatabase.LoadAssetAtPath<Shader>(p);
                if (s != null && p.EndsWith("/CurvedWorld_Terrain.shadergraph")) return s;
                if (s != null) return s;
            }
            return null;
        }

        private static int AddToSetter(List<Material> mats)
        {
            foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (mb == null || mb.GetType().Name != "CurvedWorldOriginSetter") continue;
                var so = new SerializedObject(mb);
                var arr = so.FindProperty("curvedWorldMaterials");
                if (arr == null || !arr.isArray) return 0;
                var have = new HashSet<Object>();
                for (int i = 0; i < arr.arraySize; i++) have.Add(arr.GetArrayElementAtIndex(i).objectReferenceValue);
                int added = 0;
                foreach (var m in mats)
                {
                    if (have.Contains(m)) continue;
                    arr.arraySize++;
                    arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = m;
                    have.Add(m); added++;
                }
                so.ApplyModifiedProperties();
                return added;
            }
            return 0;
        }

        private static void Dlg(string m) => EditorUtility.DisplayDialog("Sprout · Curved Terrain", m, "OK");
    }
}
#endif
