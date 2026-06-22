#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Sprout.EditorTools
{
    /// <summary>
    /// Uso único: mide las casas y comercios del pack Suntail (las instancia un instante, mide el tamaño
    /// real de su geometría y las borra) para que sepas CUÁLES son las más pequeñas y usar esas como
    /// casas de los NPCs. No deja nada en la escena.
    ///
    /// Resultado: lista ordenada por ALTURA (de más baja a más alta) con ancho x alto x fondo en metros.
    /// Menú:  Tools/Sprout/Measure Houses
    /// </summary>
    public static class SproutMeasureHouses
    {
        [MenuItem("Tools/Sprout/Measure Houses")]
        public static void Measure()
        {
            string baseB = "Assets/Suntail Village/Prefabs/Buildings/";
            string baseE = "Assets/Suntail Village/Prefabs/Environment/";
            var paths = new List<string>();
            for (int i = 1; i <= 8; i++) paths.Add(baseB + "House_" + i + ".prefab");
            for (int i = 1; i <= 3; i++) paths.Add(baseE + "Shop_" + i + ".prefab");

            var rows = new List<(string name, Vector3 size)>();
            foreach (var p in paths)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (asset == null) continue;
                var go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
                if (go == null) continue;
                go.transform.position = Vector3.zero;

                bool any = false; Bounds b = new Bounds();
                foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                {
                    if (r is ParticleSystemRenderer) continue;
                    if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
                }
                if (any) rows.Add((go.name, b.size));
                Object.DestroyImmediate(go);
            }

            if (rows.Count == 0) { EditorUtility.DisplayDialog("Sprout", "No encontré las casas en\n" + baseB, "OK"); return; }

            rows = rows.OrderBy(r => r.size.y).ToList();
            var sb = new System.Text.StringBuilder("Casas ordenadas de MÁS BAJA a más alta (ancho x alto x fondo, en metros):\n");
            foreach (var r in rows)
                sb.Append($"\n{r.name,-10}  {r.size.x:0.0} x {r.size.y:0.0} x {r.size.z:0.0}");

            sb.Append("\n\nLas de arriba (más bajas) son las más 'casita'. Úsalas para los NPCs; las altas, de fondo.");
            Debug.Log("[Sprout] " + sb.ToString().Replace("\n", "  "));
            EditorUtility.DisplayDialog("Sprout · Tamaño de las casas", sb.ToString(), "OK");
        }
    }
}
#endif
