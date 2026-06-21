#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using OpenAI.Dialogue;
using ThresholdGame.Presentation.Interaction;

namespace Sprout.EditorTools
{
    /// <summary>
    /// Uso único: deja a los NPC listos para interactuar. Para cada GameObject con NPCBrain en la
    /// escena, añade (si faltan): un Collider (para que el detector del jugador lo note), un Rigidbody
    /// kinemático (necesario para que se disparen los eventos de trigger), el NPCInteractionTrigger
    /// (IInteractable) y un DialogueRunner. Además le asigna su grafo generado por el nombre
    /// (NPC_Mochi -> Mochi_Sprout, etc.).
    ///
    /// Menú:  Tools/Sprout/Setup NPCs (interaction + graphs)
    /// Ejecútalo, guarda la escena y puedes borrar este archivo.
    /// </summary>
    public static class SproutNpcSetup
    {
        private const string GraphDir = "Assets/Project/ScriptableObjects/DialogueGraphs/Generated/";

        [MenuItem("Tools/Sprout/Setup NPCs (interaction + graphs)")]
        public static void Setup()
        {
            var brains = Object.FindObjectsByType<NPCBrain>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (brains.Length == 0)
            {
                EditorUtility.DisplayDialog("Sprout", "No encuentro ningún NPCBrain en la escena abierta.", "OK");
                return;
            }

            int done = 0;
            var report = new System.Text.StringBuilder();

            foreach (var brain in brains)
            {
                var go = brain.gameObject;

                // 1. Collider (para que la esfera de detección del jugador lo registre)
                if (go.GetComponentInChildren<Collider>() == null)
                {
                    var cap = Undo.AddComponent<CapsuleCollider>(go);
                    cap.height = 2f;
                    cap.radius = 0.5f;
                    cap.center = new Vector3(0f, 1f, 0f);
                }

                // 2. Rigidbody kinemático (sin él los OnTriggerEnter no se disparan)
                if (go.GetComponent<Rigidbody>() == null)
                {
                    var rb = Undo.AddComponent<Rigidbody>(go);
                    rb.isKinematic = true;
                    rb.useGravity = false;
                }

                // 3. NPCInteractionTrigger (IInteractable) — auto-encuentra brain y dialogueUI en Awake
                if (go.GetComponent<NPCInteractionTrigger>() == null)
                    Undo.AddComponent<NPCInteractionTrigger>(go);

                // 4. DialogueRunner (lo usa DialogueUI.Open)
                if (go.GetComponent<DialogueRunner>() == null)
                    Undo.AddComponent<DialogueRunner>(go);

                // 5. Asignar el grafo generado por el nombre del objeto
                var graph = GraphFor(go.name);
                if (graph != null)
                {
                    brain.dialogueGraph = graph;
                    EditorUtility.SetDirty(brain);
                }

                report.AppendLine($"• {go.name}  ->  {(graph != null ? graph.name : "(sin grafo asignado)")}");
                done++;
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(brains[0].gameObject.scene);
            EditorUtility.DisplayDialog("Sprout",
                $"NPCs preparados ({done}):\n\n{report}\nGuarda la escena (Ctrl+S) y dale a Play. " +
                "Acércate y pulsa E.\n\nPuedes borrar este archivo.",
                "OK");
            Debug.Log($"[Sprout] NPC setup completado para {done} NPC.");
        }

        private static DialogueGraphSO GraphFor(string objName)
        {
            string n = objName.ToLowerInvariant();
            string file = null;
            if (n.Contains("mochi")) file = "Mochi_Sprout";
            else if (n.Contains("aster")) file = "Aster_Sprout";
            else if (n.Contains("moth")) file = "Moth_Sprout";
            else if (n.Contains("rix")) file = "Rix_Sprout";
            if (file == null) return null;
            return AssetDatabase.LoadAssetAtPath<DialogueGraphSO>(GraphDir + file + ".asset");
        }
    }
}
#endif
