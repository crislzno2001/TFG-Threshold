#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using OpenAI.Dialogue;

namespace Sprout.EditorTools
{
    /// <summary>
    /// MODO DEMO: a cada NPC de la escena le asigna un grafo de diálogo SIMPLE de una sola conversación
    /// abierta con la IA (sin ramas que se rompan). El jugador habla libremente y sale diciendo "adiós".
    /// La personalidad de cada NPC sigue viniendo de su CharacterProfile, así que cada uno habla distinto.
    ///
    /// No borra tus grafos anteriores: crea grafos nuevos en Data/Dialogue/DemoChat y cambia la referencia.
    /// Para volver a los tuyos, vuelve a poner el grafo antiguo en el NPCBrain.
    ///
    /// Menú:  Tools/Sprout/Demo - Simple Chat Graphs for NPCs
    /// </summary>
    public static class SproutDemoChatGraphs
    {
        private const string Dir = "Assets/Project/Data/Dialogue/DemoChat";

        [MenuItem("Tools/Sprout/Demo - Simple Chat Graphs for NPCs")]
        public static void Build()
        {
            EnsureFolders();

            int n = 0;
            var sb = new System.Text.StringBuilder();
            foreach (var brain in Object.FindObjectsByType<NPCBrain>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                string name = string.IsNullOrWhiteSpace(brain.npcName) || brain.npcName == "NPC"
                    ? brain.gameObject.name : brain.npcName;
                string safe = Sanitize(name);
                string path = $"{Dir}/Chat_{safe}.asset";

                // Grafo
                var graph = ScriptableObject.CreateInstance<DialogueGraphSO>();
                AssetDatabase.CreateAsset(graph, AssetDatabase.GenerateUniqueAssetPath(path));

                // Nodo de conversación abierta (auto-referencia para que NO se cierre tras 1 mensaje)
                var node = ScriptableObject.CreateInstance<ConversationNodeSO>();
                node.name = "Chat";
                node.nodeGuid = System.Guid.NewGuid().ToString();
                node.contextForAI = "Conversación libre y abierta con el jugador. Responde siempre en personaje, " +
                                    "con frases cortas y naturales. Si el jugador propone ideas, muéstrate curioso.";
                node.openingLine = "¡Anda, hola! Cuéntame lo que quieras.";
                node.conversationTopics = new System.Collections.Generic.List<string>
                    { "tu día", "el pueblo", "una idea cualquiera", "lo que se te ocurra" };
                node.exitCondition = "El jugador se despide, dice adiós o que se tiene que ir.";
                node.nextNodes = new System.Collections.Generic.List<DialogueNodeSO> { node }; // self-loop: no es terminal

                AssetDatabase.AddObjectToAsset(node, graph);
                graph.nodes = new System.Collections.Generic.List<DialogueNodeSO> { node };
                graph.entryNode = node;

                EditorUtility.SetDirty(node);
                EditorUtility.SetDirty(graph);

                // Asignar al NPC
                brain.dialogueGraph = graph;
                EditorUtility.SetDirty(brain);

                n++;
                sb.Append("\n· ").Append(name);
            }

            AssetDatabase.SaveAssets();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sprout · Demo",
                n > 0
                    ? $"Grafo de chat simple asignado a {n} NPC(s):{sb}\n\n" +
                      "Cada uno habla libre con la IA en su personalidad y se sale diciendo \"adiós\".\n" +
                      "Tus grafos antiguos siguen guardados; esto solo cambió la referencia."
                    : "No encontré NPCs (NPCBrain) en la escena. ¿Están metidos en la escena de juego?",
                "OK");
            Debug.Log($"[Sprout] Demo chat graphs -> {n} NPCs.");
        }

        private static void EnsureFolders()
        {
            string[] parts = { "Assets/Project", "Assets/Project/Data", "Assets/Project/Data/Dialogue", Dir };
            string acc = "Assets";
            foreach (var p in parts)
            {
                if (!AssetDatabase.IsValidFolder(p))
                {
                    string parent = Path.GetDirectoryName(p).Replace('\\', '/');
                    string leaf = Path.GetFileName(p);
                    AssetDatabase.CreateFolder(parent, leaf);
                }
            }
        }

        private static string Sanitize(string s)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
            return s.Replace(' ', '_');
        }
    }
}
#endif
