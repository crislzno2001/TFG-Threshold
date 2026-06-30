#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Sprout.EditorTools
{
    /// <summary>
    /// Ayuda con las luces de la escena.
    ///   - List Lights: imprime TODAS las luces (nombre, tipo, posición, encendida) para localizar esa
    ///     luz suelta que no sabes de dónde sale.
    ///   - Light Up Lamp Posts: pone una luz puntual cálida en la cabeza de cada farola/farol para que la
    ///     noche quede acogedora. (Reversible: borra los hijos "LampLight".)
    /// </summary>
    public static class SproutLights
    {
        [MenuItem("Tools/Sprout/List Lights in Scene")]
        public static void ListLights()
        {
            var all = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var sb = new StringBuilder($"[Sprout] {all.Length} luz(ces) en la escena:\n");
            foreach (var l in all)
            {
                var p = l.transform.position;
                sb.Append($"\n· {l.name}  [{l.type}]  pos=({p.x:0.0},{p.y:0.0},{p.z:0.0})  " +
                          $"int={l.intensity:0.0}  range={l.range:0.0}  {(l.enabled && l.gameObject.activeInHierarchy ? "ON" : "off")}");
            }
            Debug.Log(sb.ToString());
            EditorUtility.DisplayDialog("Sprout · Luces",
                $"{all.Length} luces listadas en la consola (nombre y posición).\n\n" +
                "Busca la que coincida con la luz naranja suelta y la desactivas/borras en la jerarquía.", "OK");
        }

        [MenuItem("Tools/Sprout/Light Up Lamp Posts")]
        public static void LightLamps()
        {
            int n = 0;
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                string nm = t.name.ToLowerInvariant();
                bool isLamp = nm.Contains("lamp") || nm.Contains("lantern") || nm.Contains("farol") || nm.Contains("farola");
                if (!isLamp) continue;

                // Altura del farol (más abajo que la punta): ~28% por debajo del tope = cerca del cristal.
                float headY = t.position.y + 2.5f;
                var rends = t.GetComponentsInChildren<Renderer>();
                if (rends.Length > 0)
                {
                    var b = rends[0].bounds;
                    for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
                    headY = b.max.y - b.size.y * 0.28f;
                }

                // Reutiliza la luz si ya existe (la reposiciona); si no, la crea.
                var existing = t.Find("LampLight");
                GameObject go = existing != null ? existing.gameObject : new GameObject("LampLight");
                if (existing == null) { Undo.RegisterCreatedObjectUndo(go, "Light Up Lamp"); go.transform.SetParent(t, true); }
                go.transform.position = new Vector3(t.position.x, headY, t.position.z);

                var light = go.GetComponent<Light>();
                if (light == null) light = go.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(1f, 0.78f, 0.45f); // cálida
                light.intensity = 2.4f;
                light.range = 9f;
                light.shadows = LightShadows.Soft;
                n++;
            }

            // Controlador día/noche: que las farolas se enciendan SOLO de noche.
            if (Object.FindFirstObjectByType<Sprout.Presentation.LampNightLights>() == null)
            {
                var ctrlGo = new GameObject("LampNightLights");
                Undo.RegisterCreatedObjectUndo(ctrlGo, "Lamp Night Lights");
                ctrlGo.AddComponent<Sprout.Presentation.LampNightLights>();
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("Sprout · Luces",
                n > 0 ? $"Puestas {n} farolas con luz cálida.\n\nSe encienden SOLO de noche (Evening/Night): lo gestiona el objeto 'LampNightLights' que acabo de crear.\n\nReversible: borra los hijos 'LampLight'."
                      : "No encontré farolas por nombre (lamp/lantern/farol). Dime cómo se llaman tus farolas.", "OK");
            Debug.Log($"[Sprout] Farolas: {n}");
        }
    }
}
#endif
