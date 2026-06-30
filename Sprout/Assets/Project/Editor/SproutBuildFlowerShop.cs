#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Sprout.SceneFlow;

namespace Sprout.EditorTools
{
    /// <summary>
    /// Monta el interior de la floristería EN LA ESCENA ABIERTA actualmente (abre tu FlowerShop antes).
    /// Crea un objeto "FlowerShop_Interior" con suelo + paredes (hueco de puerta), luz cálida, un
    /// SpawnPoint "Entrada" y una puerta de salida (DoorPortal) que vuelve al pueblo.
    ///
    /// Todo cuelga de "FlowerShop_Interior", así que si choca con algo que ya tengas, lo borras de un golpe.
    /// Menú:  Tools/Sprout/Build FlowerShop Interior (current scene)
    /// </summary>
    public static class SproutBuildFlowerShop
    {
        private const float W = 7f, D = 5f, H = 3f, T = 0.2f, DoorGap = 1.6f;

        [MenuItem("Tools/Sprout/Build FlowerShop Interior (current scene)")]
        public static void Build()
        {
            if (!EditorUtility.DisplayDialog("Sprout",
                "Voy a montar el interior (suelo, paredes, luz, SpawnPoint 'Entrada' y puerta de salida) en la " +
                "ESCENA ABIERTA ahora mismo.\n\nAsegúrate de tener abierta tu escena FlowerShop. ¿Continuar?",
                "Sí, montar", "Cancelar"))
                return;

            var room = new GameObject("FlowerShop_Interior");
            Undo.RegisterCreatedObjectUndo(room, "Build FlowerShop");

            Box("Suelo", new Vector3(0, -T / 2f, 0), new Vector3(W, T, D), room.transform, new Color(0.55f, 0.42f, 0.30f));
            Box("Pared_Norte", new Vector3(0, H / 2f, D / 2f), new Vector3(W, H, T), room.transform, WallCol());
            Box("Pared_Este", new Vector3(W / 2f, H / 2f, 0), new Vector3(T, H, D), room.transform, WallCol());
            Box("Pared_Oeste", new Vector3(-W / 2f, H / 2f, 0), new Vector3(T, H, D), room.transform, WallCol());
            float seg = (W - DoorGap) / 2f;
            Box("Pared_Sur_Izq", new Vector3(-(DoorGap / 2f + seg / 2f), H / 2f, -D / 2f), new Vector3(seg, H, T), room.transform, WallCol());
            Box("Pared_Sur_Der", new Vector3((DoorGap / 2f + seg / 2f), H / 2f, -D / 2f), new Vector3(seg, H, T), room.transform, WallCol());

            var lightGo = new GameObject("Luz_Interior");
            lightGo.transform.SetParent(room.transform);
            lightGo.transform.position = new Vector3(0, H - 0.4f, 0);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.93f, 0.8f);
            light.intensity = 1.4f;
            light.range = 16f;
            light.shadows = LightShadows.Soft;

            var spawn = new GameObject("SpawnPoint_Entrada");
            spawn.transform.SetParent(room.transform);
            spawn.transform.SetPositionAndRotation(new Vector3(0, 0, -D / 2f + 0.9f), Quaternion.identity);
            spawn.AddComponent<SpawnPoint>().id = "Entrada";

            var door = new GameObject("Puerta_Salida");
            door.transform.SetParent(room.transform);
            door.transform.position = new Vector3(0, 1f, -D / 2f);
            var col = door.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(DoorGap, 2f, 1f);
            var portal = door.AddComponent<DoorPortal>();
            portal.targetScene = "GameScene";          // cámbialo al nombre real de tu escena del pueblo
            portal.targetSpawnId = "Floristeria_Salida";

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("Sprout",
                "Interior montado en la escena actual (objeto 'FlowerShop_Interior'). Guarda con Ctrl+S.\n\n" +
                "Pendiente:\n" +
                "1) La puerta de salida va a 'GameScene' -> si tu pueblo se llama distinto, cámbialo en el DoorPortal.\n" +
                "2) En el pueblo, pon un SpawnPoint con id 'Floristeria_Salida' delante de la floristería (para volver ahí).\n" +
                "3) Amueblala con los props de Suntail.", "OK");
            Debug.Log("[Sprout] FlowerShop interior montado.");
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
                var sh = Shader.Find("Universal Render Pipeline/Lit");
                var m = new Material(sh != null ? sh : Shader.Find("Standard"));
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
                m.color = color;
                if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.1f);
                r.sharedMaterial = m;
            }
            return go;
        }

        private static Color WallCol() => new Color(0.88f, 0.84f, 0.78f);
    }
}
#endif
