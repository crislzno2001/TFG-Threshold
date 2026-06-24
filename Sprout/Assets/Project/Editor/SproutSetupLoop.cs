#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using OpenAI.Dialogue;
using Sprout.Application;
using Sprout.Presentation;

namespace Sprout.EditorTools
{
    /// <summary>
    /// Uso único: deja el bucle diario montado en la escena actual.
    ///   - Añade DialogueEntryRouter a cada NPC (NPCBrain) y precarga "Día 1 = nodo de entrada del grafo"
    ///     (los días 2 y 3 los asignas tú con los nodos de reacción/desenlace).
    ///   - Crea/usa un objeto de dormir con BedSleepPoint + Collider trigger, con sus referencias puestas.
    ///   - Pone el tag "Player" al jugador (lo necesitan las puertas y la cama).
    ///
    /// Menú:  Tools/Sprout/Setup Daily Loop
    /// </summary>
    public static class SproutSetupLoop
    {
        [MenuItem("Tools/Sprout/Setup Daily Loop")]
        public static void Setup()
        {
            int routers = 0, prefilled = 0;

            // 1) Routers en los NPCs
            foreach (var brain in Object.FindObjectsByType<NPCBrain>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var router = brain.GetComponent<DialogueEntryRouter>();
                if (router == null) { router = Undo.AddComponent<DialogueEntryRouter>(brain.gameObject); routers++; }

                if (router.entriesByDay.Count == 0 && brain.dialogueGraph != null && brain.dialogueGraph.entryNode != null)
                {
                    router.entriesByDay.Add(new DialogueEntryRouter.DayEntry { day = 1, node = brain.dialogueGraph.entryNode });
                    prefilled++;
                }
                EditorUtility.SetDirty(router);
            }

            // 2) Objeto para dormir
            var bsp = Object.FindFirstObjectByType<BedSleepPoint>();
            GameObject bedGo;
            if (bsp != null) bedGo = bsp.gameObject;
            else
            {
                GameObject found = null;
                foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    string n = t.name.ToLowerInvariant();
                    if (n.Contains("bed") || n.Contains("cama")) { found = t.gameObject; break; }
                }
                bedGo = found != null ? found : new GameObject("SleepPoint (cama)");
            }

            // Collider trigger PRIMERO (BedSleepPoint requiere Collider; añadirlo antes evita el fallo).
            var col = bedGo.GetComponent<Collider>();
            if (col == null) { var bc = Undo.AddComponent<BoxCollider>(bedGo); bc.isTrigger = true; bc.size = new Vector3(2f, 1f, 2f); }
            else col.isTrigger = true;

            if (bsp == null) bsp = bedGo.GetComponent<BedSleepPoint>();
            if (bsp == null) bsp = Undo.AddComponent<BedSleepPoint>(bedGo);

            // Referencias del BedSleepPoint
            if (bsp != null)
            {
                var so = new SerializedObject(bsp);
                var pDay = so.FindProperty("dayCycle");
                var pGos = so.FindProperty("gossip");
                if (pDay != null) pDay.objectReferenceValue = Object.FindFirstObjectByType<DayCycleService>();
                if (pGos != null) pGos.objectReferenceValue = Object.FindFirstObjectByType<NightGossipService>();
                so.ApplyModifiedProperties();
            }

            // 3) Tag Player al jugador
            string playerInfo = "no encontrado";
            foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (mb == null || mb.GetType().Name != "AnimalCrossingLocomotion") continue;
                if (mb.tag != "Player") { mb.tag = "Player"; EditorUtility.SetDirty(mb.gameObject); }
                playerInfo = mb.gameObject.name + " (tag Player)";
                break;
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sprout",
                $"Bucle diario montado:\n\n" +
                $"· DialogueEntryRouter añadido a {routers} NPC(s); Día 1 precargado en {prefilled}.\n" +
                $"· Dormir: '{bedGo.name}' con BedSleepPoint + trigger.\n" +
                $"· Jugador: {playerInfo}\n\n" +
                "Te falta a mano:\n" +
                "1) En cada router, asignar los nodos de Día 2 y Día 3 (los de reacción/desenlace).\n" +
                "2) Mover el objeto de dormir encima de la cama si se creó en el origen.\n" +
                "3) (Opcional) Poner ShowIngredientCardOnFlag con la flag de la receta de Mochi.", "OK");
            Debug.Log($"[Sprout] Daily loop: routers={routers}, prefilled={prefilled}, bed='{bedGo.name}'.");
        }
    }
}
#endif
