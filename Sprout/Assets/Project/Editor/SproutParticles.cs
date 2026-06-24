#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Sprout.EditorTools
{
    /// <summary>
    /// Uso único: ayuda con las partículas del pack que aparecen "en la cara" (god rays, polvo, niebla…).
    ///   - List Particle Systems: imprime en consola TODAS las partículas de la escena (nombre + posición)
    ///     para que identifiques cuál te molesta.
    ///   - Disable Ambient Particles: desactiva las ambientales por nombre (godray/ray/dust/mote/fog/pollen/
    ///     ash/snow/leaf/firefly/shaft). NO toca fuego/humo de chimenea. Reversible (las deja desactivadas
    ///     en la jerarquía; las reactivas con el check del GameObject).
    /// </summary>
    public static class SproutParticles
    {
        private static readonly string[] Ambient =
            { "godray", "god ray", "ray", "dust", "mote", "fog", "pollen", "ash", "snow",
              "leaf", "leaves", "firefly", "fireflies", "shaft", "atmos", "haze", "mist" };

        [MenuItem("Tools/Sprout/List Particle Systems")]
        public static void List()
        {
            var all = Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var sb = new StringBuilder($"[Sprout] {all.Length} ParticleSystem(s) en la escena:\n");
            foreach (var ps in all)
            {
                var p = ps.transform.position;
                sb.Append($"\n· {ps.name}  pos=({p.x:0.0},{p.y:0.0},{p.z:0.0})  " +
                          $"space={ps.main.simulationSpace}  max={ps.main.maxParticles}  " +
                          $"{(ps.gameObject.activeInHierarchy ? "ON" : "off")}");
            }
            Debug.Log(sb.ToString());
            EditorUtility.DisplayDialog("Sprout",
                $"{all.Length} sistemas de partículas listados en la consola (con nombre y posición).\n\n" +
                "Mira cuál coincide con lo que ves 'en la cara' y lo desactivas en la jerarquía, " +
                "o usa 'Disable Ambient Particles' para apagar las atmosféricas de golpe.", "OK");
        }

        [MenuItem("Tools/Sprout/Disable Fire and Smoke Particles")]
        public static void DisableFireSmoke()
        {
            int off = DisableWhere(n =>
                n.Contains("fire") || n.Contains("smoke") || n.Contains("candle") ||
                n.Contains("ember") || n.Contains("spark") || n.Contains("chimney") ||
                n.Contains("humo") || n.Contains("fuego"));
            Done(off, "fuego/humo");
        }

        [MenuItem("Tools/Sprout/Disable ALL Particles")]
        public static void DisableAll()
        {
            if (!EditorUtility.DisplayDialog("Sprout",
                "¿Desactivar TODAS las partículas de la escena? (reversible con el check del GameObject)", "Sí", "No"))
                return;
            int off = DisableWhere(_ => true);
            Done(off, "todas");
        }

        [MenuItem("Tools/Sprout/Disable Ambient Particles")]
        public static void DisableAmbient()
        {
            var all = Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int off = 0;
            var names = new StringBuilder();
            foreach (var ps in all)
            {
                string n = ps.name.ToLowerInvariant();
                // No tocar fuego/humo/vela/chispa de chimenea
                if (n.Contains("fire") || n.Contains("smoke") || n.Contains("candle") || n.Contains("ember") || n.Contains("spark"))
                    continue;
                bool ambient = false;
                foreach (var k in Ambient) if (n.Contains(k)) { ambient = true; break; }
                if (!ambient) continue;

                if (ps.gameObject.activeSelf)
                {
                    Undo.RecordObject(ps.gameObject, "Disable Ambient Particles");
                    ps.gameObject.SetActive(false);
                    EditorUtility.SetDirty(ps.gameObject);
                    off++;
                    if (off <= 30) names.Append("\n· ").Append(ps.name);
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("Sprout",
                off > 0
                    ? $"Desactivadas {off} partículas ambientales:{names}\n\nReactiva cualquiera con el check del GameObject."
                    : "No encontré partículas ambientales por nombre. Usa 'List Particle Systems' para verlas todas y dime cuál es.",
                "OK");
            Debug.Log($"[Sprout] Ambient particles desactivadas: {off}");
        }

        // Desactiva los GameObjects de las partículas cuyo nombre cumple el predicado.
        private static int DisableWhere(System.Func<string, bool> match)
        {
            var all = Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int off = 0;
            foreach (var ps in all)
            {
                if (!match(ps.name.ToLowerInvariant())) continue;
                if (!ps.gameObject.activeSelf) continue;
                Undo.RecordObject(ps.gameObject, "Disable Particles");
                ps.gameObject.SetActive(false);
                EditorUtility.SetDirty(ps.gameObject);
                off++;
            }
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            return off;
        }

        private static void Done(int off, string what)
        {
            EditorUtility.DisplayDialog("Sprout",
                off > 0
                    ? $"Desactivadas {off} partículas ({what}).\nReactiva cualquiera con el check del GameObject."
                    : $"No encontré partículas de tipo '{what}' activas.",
                "OK");
            Debug.Log($"[Sprout] Partículas '{what}' desactivadas: {off}");
        }
    }
}
#endif
