#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Sprout.EditorTools
{
    /// <summary>
    /// Uso único: el pueblo Suntail está hecho a escala humana (~1.8 m) y la florista chibi es mucho más
    /// baja, por eso el mundo parece enorme y las puertas/muebles le quedan gigantes. Esto la escala
    /// hasta una altura objetivo y sube su velocidad en la misma proporción (para que cruzar el pueblo
    /// no se haga eterno). Así el pueblo "encaja" sin tocar casas ni muebles.
    ///
    /// Cambia TargetHeight si la quieres más alta o más baja. Se puede deshacer con Ctrl+Z.
    /// Menú:  Tools/Sprout/Scale Player to World
    /// </summary>
    public static class SproutScalePlayer
    {
        private const float TargetHeight = 1.7f; // metros, altura deseada de la florista

        [MenuItem("Tools/Sprout/Scale Player to World")]
        public static void Scale()
        {
            // 1) Encontrar el objeto-jugador (el que tiene la locomoción) o por nombre
            GameObject player = null;
            foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (mb != null && mb.GetType().Name == "AnimalCrossingLocomotion") { player = mb.gameObject; break; }
            }
            if (player == null)
            {
                foreach (var smr in Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (NameChainContains(smr.transform, "angry", "florista")) { player = smr.transform.root.gameObject; break; }
            }
            if (player == null) { Dlg("No encontré al jugador (ni AnimalCrossingLocomotion ni 'florista'/'angry')."); return; }

            // 2) Medir su altura actual en el mundo (bounds de todos sus renderers)
            bool any = false;
            Bounds b = new Bounds();
            foreach (var r in player.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer) continue;
                if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
            }
            if (!any) { Dlg("El jugador no tiene renderers para medir su altura."); return; }

            float currentH = b.size.y;
            if (currentH < 0.01f) { Dlg("Altura medida demasiado pequeña, algo va raro."); return; }
            float factor = TargetHeight / currentH;
            if (Mathf.Abs(factor - 1f) < 0.02f) { Dlg($"La florista ya mide ~{currentH:0.00} m. No hace falta escalar."); return; }

            // 3) Aplicar escala (sacando cámaras hijas para que NO se escalen con ella)
            var childCams = player.GetComponentsInChildren<Camera>(true);
            var camData = new System.Collections.Generic.List<(Transform t, Transform parent, Vector3 p, Quaternion r, Vector3 s)>();
            foreach (var c in childCams)
            {
                if (c.transform == player.transform) continue;
                camData.Add((c.transform, c.transform.parent, c.transform.position, c.transform.rotation, c.transform.lossyScale));
                Undo.SetTransformParent(c.transform, player.transform.parent, "Scale Player");
            }

            Undo.RecordObject(player.transform, "Scale Player");
            player.transform.localScale *= factor;

            // (las cámaras se quedan fuera del jugador para no heredar la escala; si seguían al jugador
            //  por script, el script las recoloca igual. Si las quieres dentro, dímelo.)

            // 4) Subir la velocidad en la misma proporción
            float oldMove = 0, oldSprint = 0, newMove = 0, newSprint = 0;
            foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (mb == null || mb.GetType().Name != "AnimalCrossingLocomotion") continue;
                var so = new SerializedObject(mb);
                var mv = so.FindProperty("moveSpeed");
                var sp = so.FindProperty("sprintSpeed");
                if (mv != null) { oldMove = mv.floatValue; newMove = oldMove * factor; mv.floatValue = newMove; }
                if (sp != null) { oldSprint = sp.floatValue; newSprint = oldSprint * factor; sp.floatValue = newSprint; }
                so.ApplyModifiedProperties();
                break;
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(player.scene);
            Dlg($"Florista escalada x{factor:0.00}\n" +
                $"Altura: {currentH:0.00} m  ->  ~{TargetHeight:0.00} m\n" +
                $"Velocidad: {oldMove:0.00} -> {newMove:0.00}  /  sprint {oldSprint:0.00} -> {newSprint:0.00}\n\n" +
                "Si te pasaste o te quedaste corta, cambia TargetHeight arriba del script, o deshaz con Ctrl+Z.\n" +
                "Recuerda re-hornear las reflection probes después si las usas.");
            Debug.Log($"[Sprout] Player escalado x{factor:0.00} (a {TargetHeight} m). Vel {oldMove}->{newMove}.");
        }

        private static bool NameChainContains(Transform t, params string[] keys)
        {
            while (t != null)
            {
                string n = t.name.ToLowerInvariant();
                foreach (var k in keys) if (n.Contains(k)) return true;
                t = t.parent;
            }
            return false;
        }

        private static void Dlg(string m) => EditorUtility.DisplayDialog("Sprout", m, "OK");
    }
}
#endif
