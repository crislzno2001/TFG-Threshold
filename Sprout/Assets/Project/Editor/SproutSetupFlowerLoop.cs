#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using Sprout.Application;
using Sprout.Data;

namespace Sprout.EditorTools
{
    /// <summary>
    /// Conecta el sistema de flores en la escena de un clic. Carga TODOS los FlowerDefinitionSO y
    /// BouquetDefinitionSO del proyecto y los mete en las listas del FlowerService (si no, no encuentra
    /// el modelo 3D del ramo ni la reacción del vecino). También enlaza el FlowerService al
    /// FlowerFlagListener y avisa de lo que falte.
    /// </summary>
    public static class SproutSetupFlowerLoop
    {
        [MenuItem("Tools/Sprout/Setup Flower Loop")]
        public static void Setup()
        {
            var fs = Object.FindFirstObjectByType<FlowerService>();
            if (fs == null)
            {
                Dlg("No hay 'FlowerService' en la escena.\nAñádelo a un objeto (p. ej. el SproutGame) y vuelve a darle.");
                return;
            }

            var flowers = LoadAll<FlowerDefinitionSO>();
            var bouquets = LoadAll<BouquetDefinitionSO>();

            var so = new SerializedObject(fs);
            int f = AssignList(so, "flowerDefs", flowers);
            int b = AssignList(so, "bouquetDefs", bouquets);
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(fs);

            // Enlazar FlowerFlagListener -> FlowerService si le falta.
            var listener = Object.FindFirstObjectByType<FlowerFlagListener>();
            if (listener != null)
            {
                var lso = new SerializedObject(listener);
                var p = lso.FindProperty("flowerService");
                if (p != null && p.objectReferenceValue == null)
                {
                    p.objectReferenceValue = fs;
                    lso.ApplyModifiedProperties();
                    EditorUtility.SetDirty(listener);
                }
            }

            // Ramos sin modelo 3D (volará un ramo de repuesto, pero conviene saberlo).
            int noModel = 0; var names = new StringBuilder();
            foreach (var bq in bouquets)
                if (bq != null && bq.model == null) { noModel++; names.Append("\n· " + bq.displayName); }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(fs.gameObject.scene);

            Dlg($"FlowerService conectado:\n" +
                $"· Flores asignadas: {f}/7\n" +
                $"· Ramos asignados: {b}/8\n" +
                (listener == null
                    ? "\n⚠ No hay 'FlowerFlagListener' en la escena: las flores no brotarán solas con las flags (puedes usar la tecla 3 del debug para probar)."
                    : "\n✓ FlowerFlagListener enlazado.") +
                (noModel > 0
                    ? $"\n\n⚠ {noModel} ramos SIN modelo 3D (volará un ramo de repuesto):{names}\nAsigna el campo 'model' en esos Bouquet_*.asset."
                    : "\n\n✓ Todos los ramos tienen modelo 3D."));
        }

        private static List<T> LoadAll<T>() where T : Object
        {
            var list = new List<T>();
            foreach (var g in AssetDatabase.FindAssets("t:" + typeof(T).Name))
            {
                var a = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g));
                if (a != null) list.Add(a);
            }
            return list;
        }

        private static int AssignList<T>(SerializedObject so, string prop, List<T> items) where T : Object
        {
            var arr = so.FindProperty(prop);
            if (arr == null || !arr.isArray) return 0;
            arr.ClearArray();
            for (int i = 0; i < items.Count; i++)
            {
                arr.InsertArrayElementAtIndex(i);
                arr.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            }
            return items.Count;
        }

        private static void Dlg(string m) => EditorUtility.DisplayDialog("Sprout · Setup Flower Loop", m, "OK");
    }
}
#endif
