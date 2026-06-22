#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Sprout.SceneFlow;

namespace Sprout.EditorTools
{
    /// <summary>
    /// Uso único: crea una ESCENA INTERIOR de ejemplo, ya montada y a buena escala, para el sistema de
    /// puertas estilo Animal Crossing. Genera una habitación cerrada (suelo + 4 paredes con un hueco de
    /// puerta), una luz cálida, un SpawnPoint "Entrada" y una puerta de salida (DoorPortal) que vuelve al
    /// pueblo. La guarda en Assets/Project/Scenes/Interiors/ y la añade a Build Settings.
    ///
    /// Duplica el .unity resultante por cada NPC (Interior_Mochi, Interior_Aster…) y redecóralo con los
    /// muebles de Suntail. Las paredes son primitivas (fiables y a escala); si quieres, luego cambias el
    /// aspecto por los módulos Indoor de Suntail.
    ///
    /// Menú:  Tools/Sprout/Build Interior Scene Template
    /// </summary>
    public static class SproutBuildInteriorTemplate
    {
        private const string Dir = "Assets/Project/Scenes/Interiors";
        private const string ScenePath = Dir + "/Interior_Plantilla.unity";

        // Medidas de la habitación (metros)
        private const float W = 6f;   // ancho (X)
        private const float D = 4f;   // fondo (Z)
        private const float H = 3f;   // alto
        private const float T = 0.2f; // grosor pared
        private const float DoorGap = 1.4f; // hueco de la puerta en la pared sur

        [MenuItem("Tools/Sprout/Build Interior Scene Template")]
        public static void Build()
        {
            EnsureFolders();

            var prev = EditorSceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            EditorSceneManager.SetActiveScene(scene);

            // Raíz para ordenar
            var room = new GameObject("Habitacion");

            // Suelo
            var floor = Box("Suelo", new Vector3(0, -T / 2f, 0), new Vector3(W, T, D), room.transform, new Color(0.55f, 0.42f, 0.30f));
            // Paredes
            Box("Pared_Norte", new Vector3(0, H / 2f, D / 2f), new Vector3(W, H, T), room.transform, WallCol());
            Box("Pared_Este", new Vector3(W / 2f, H / 2f, 0), new Vector3(T, H, D), room.transform, WallCol());
            Box("Pared_Oeste", new Vector3(-W / 2f, H / 2f, 0), new Vector3(T, H, D), room.transform, WallCol());
            // Pared sur con hueco de puerta (dos segmentos)
            float seg = (W - DoorGap) / 2f;
            Box("Pared_Sur_Izq", new Vector3(-(DoorGap / 2f + seg / 2f), H / 2f, -D / 2f), new Vector3(seg, H, T), room.transform, WallCol());
            Box("Pared_Sur_Der", new Vector3((DoorGap / 2f + seg / 2f), H / 2f, -D / 2f), new Vector3(seg, H, T), room.transform, WallCol());

            // Luz cálida
            var lightGo = new GameObject("Luz_Interior");
            lightGo.transform.position = new Vector3(0, H - 0.4f, 0);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.93f, 0.8f);
            light.intensity = 1.3f;
            light.range = 14f;
            light.shadows = LightShadows.Soft;

            // SpawnPoint "Entrada": justo dentro de la puerta, mirando al interior (+Z)
            var spawn = new GameObject("SpawnPoint_Entrada");
            spawn.transform.SetPositionAndRotation(new Vector3(0, 0, -D / 2f + 0.8f), Quaternion.identity);
            var sp = spawn.AddComponent<SpawnPoint>();
            sp.id = "Entrada";

            // Puerta de salida (DoorPortal): en el hueco de la pared sur, vuelve al pueblo
            var door = new GameObject("Puerta_Salida");
            door.transform.position = new Vector3(0, 1f, -D / 2f);
            var col = door.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(DoorGap, 2f, 1f);
            var portal = door.AddComponent<DoorPortal>();
            portal.targetScene = "Pueblo";       // cámbialo al nombre real de tu escena del pueblo
            portal.targetSpawnId = "Puerta_Casa"; // pon un SpawnPoint con este id delante de la casa
            portal.autoEnter = false;

            // Guardar
            bool ok = EditorSceneManager.SaveScene(scene, ScenePath);
            if (ok) AddToBuildSettings(ScenePath);

            // Volver a dejar activa la escena anterior (no cerramos nada, para que veas el resultado)
            if (prev.IsValid()) EditorSceneManager.SetActiveScene(prev);

            EditorUtility.DisplayDialog("Sprout",
                (ok ? "Escena interior creada:\n" + ScenePath : "No se pudo guardar la escena.") + "\n\n" +
                "· La he abierto (additive) para que la veas. Tu pueblo sigue cargado.\n" +
                "· Duplícala (Ctrl+D en Project) por cada NPC: Interior_Mochi, Interior_Aster…\n" +
                "· En el DoorPortal de salida, ajusta 'targetScene' al nombre real de tu escena del pueblo " +
                "y pon en el pueblo un SpawnPoint con id 'Puerta_Casa' delante de la puerta.\n" +
                "· Acuérdate de añadir el pueblo a File > Build Settings también.", "OK");
            Debug.Log("[Sprout] Interior template -> " + ScenePath);
        }

        private static GameObject Box(string name, Vector3 pos, Vector3 size, Transform parent, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.transform.localScale = size;
            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                m.color = color;
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
                m.SetFloat("_Smoothness", 0.1f);
                r.sharedMaterial = m;
            }
            return go;
        }

        private static Color WallCol() => new Color(0.88f, 0.84f, 0.78f);

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Project")) AssetDatabase.CreateFolder("Assets", "Project");
            if (!AssetDatabase.IsValidFolder("Assets/Project/Scenes")) AssetDatabase.CreateFolder("Assets/Project", "Scenes");
            if (!AssetDatabase.IsValidFolder(Dir)) AssetDatabase.CreateFolder("Assets/Project/Scenes", "Interiors");
        }

        private static void AddToBuildSettings(string path)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (var s in scenes) if (s.path == path) return; // ya está
            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
