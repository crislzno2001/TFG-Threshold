#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Sprout.EditorTools
{
    /// <summary>
    /// Añade Mesh Colliders a los objetos seleccionados (y sus hijos) que tengan malla pero NO collider,
    /// para que el jugador deje de atravesar edificios (floristería, casas, props grandes).
    ///
    /// Selecciona el objeto del edificio en la jerarquía y dale al botón.
    /// Menú:  Tools/Sprout/Add Mesh Colliders (selection)
    /// </summary>
    public static class SproutAddColliders
    {
        [MenuItem("Tools/Sprout/Add Mesh Colliders (selection)")]
        public static void Add()
        {
            if (Selection.gameObjects == null || Selection.gameObjects.Length == 0)
            {
                EditorUtility.DisplayDialog("Sprout", "Selecciona en la jerarquía el edificio (p. ej. la floristería) y vuelve a darle.", "OK");
                return;
            }

            int added = 0;
            foreach (var go in Selection.gameObjects)
            {
                foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (mf.sharedMesh == null) continue;
                    // si ese objeto ya tiene cualquier collider, no tocar
                    if (mf.GetComponent<Collider>() != null) continue;

                    var mc = Undo.AddComponent<MeshCollider>(mf.gameObject);
                    mc.sharedMesh = mf.sharedMesh;
                    added++;
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("Sprout",
                $"Añadidos {added} Mesh Colliders.\n\nAhora el jugador no debería atravesar el edificio.\n" +
                "Si te quedas atascada al entrar por la puerta, en esa parte de la puerta quita su collider " +
                "(o deja un hueco), porque la entrada la gestiona el DoorPortal.", "OK");
            Debug.Log($"[Sprout] Mesh colliders añadidos: {added}");
        }
    }
}
#endif
