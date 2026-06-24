#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Sprout.EditorTools
{
    /// <summary>
    /// Uso único: arregla los objetos que "aparecen/desaparecen al acercarte" porque borraste mallas de
    /// sus LOD (Level Of Detail). Un LODGroup cambia de malla según la distancia y, si una de esas mallas
    /// ya no existe, a esa distancia no dibuja nada.
    ///
    /// - Fix Broken LOD Groups (scene): recorre la escena y quita SOLO los LODGroup rotos (con mallas
    ///   borradas/huecos). La malla que quede se verá siempre. Los LODGroup sanos no se tocan.
    /// - Remove LOD Groups (selection): quita el LODGroup de lo que tengas seleccionado (y sus hijos),
    ///   para forzar que se vean siempre.
    ///
    /// Menú:  Tools/Sprout/...
    /// </summary>
    public static class SproutFixLOD
    {
        [MenuItem("Tools/Sprout/Fix Broken LOD Groups")]
        public static void FixBroken()
        {
            int removed = 0, checkedN = 0;
            foreach (var lg in Object.FindObjectsByType<LODGroup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                checkedN++;
                if (IsBroken(lg))
                {
                    Undo.DestroyObjectImmediate(lg); // quita el componente; las mallas que queden se ven siempre
                    removed++;
                }
            }
            MarkDirty();
            EditorUtility.DisplayDialog("Sprout",
                $"LODGroups revisados: {checkedN}\nRotos quitados (se ven siempre): {removed}\n\n" +
                (removed == 0 ? "No había LODGroups rotos. Si aún ves el efecto, usa 'Remove LOD Groups (selection)' sobre el objeto que parpadea."
                              : "Si todavía parpadea algo, selecciónalo y usa 'Remove LOD Groups (selection)'."), "OK");
            Debug.Log($"[Sprout] Fix LOD: revisados {checkedN}, rotos quitados {removed}");
        }

        [MenuItem("Tools/Sprout/Remove LOD Groups (selection)")]
        public static void RemoveInSelection()
        {
            if (Selection.gameObjects == null || Selection.gameObjects.Length == 0)
            { EditorUtility.DisplayDialog("Sprout", "Selecciona en la jerarquía el/los objetos que parpadean.", "OK"); return; }

            int removed = 0;
            foreach (var go in Selection.gameObjects)
                foreach (var lg in go.GetComponentsInChildren<LODGroup>(true))
                { Undo.DestroyObjectImmediate(lg); removed++; }

            MarkDirty();
            EditorUtility.DisplayDialog("Sprout", $"LODGroups quitados de la selección: {removed}\nAhora se ven siempre.", "OK");
            Debug.Log($"[Sprout] LODGroups quitados de selección: {removed}");
        }

        // Un LODGroup está "roto" si algún nivel tiene renderers nulos (mallas borradas) o un nivel sin renderers.
        private static bool IsBroken(LODGroup lg)
        {
            var lods = lg.GetLODs();
            if (lods == null || lods.Length == 0) return true;
            foreach (var lod in lods)
            {
                if (lod.renderers == null || lod.renderers.Length == 0) return true;
                foreach (var r in lod.renderers)
                    if (r == null) return true; // referencia a malla borrada
            }
            return false;
        }

        private static void MarkDirty()
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }
    }
}
#endif
