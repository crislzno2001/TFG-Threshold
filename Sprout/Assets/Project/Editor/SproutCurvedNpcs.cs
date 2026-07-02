#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using OpenAI.Dialogue;

namespace Sprout.EditorTools
{
    /// <summary>
    /// Uso único: pone a TODOS los NPC de la escena (objetos con NPCBrain) el mismo shader Curved World
    /// que usa el suelo y la florista, para que se curven igual y dejen de "flotar" al alejarse del origen.
    /// Conserva sus texturas (albedo/normal/metallic) y los añade a la lista 'curvedWorldMaterials' del
    /// CurvedWorldOriginSetter de la escena (sin eso NO se curvan).
    ///
    /// Menú:  Tools/Sprout/NPCs use Curved World shader
    /// Ejecútalo, guarda la escena. Puedes borrar este archivo luego.
    /// </summary>
    public static class SproutCurvedNpcs
    {
        [MenuItem("Tools/Sprout/NPCs use Curved World shader")]
        public static void Apply()
        {
            Shader curved = FindCurvedShader();
            if (curved == null) { Dlg("No encuentro el shader CurvedWorld (.shadergraph) en el proyecto."); return; }

            // Propiedades del shader (base/normal/metal) por nombre.
            string baseProp = null, normalProp = null, metalProp = null;
            int count = ShaderUtil.GetPropertyCount(curved);
            for (int i = 0; i < count; i++)
            {
                if (ShaderUtil.GetPropertyType(curved, i) != ShaderUtil.ShaderPropertyType.TexEnv) continue;
                string n = ShaderUtil.GetPropertyName(curved, i);
                string l = n.ToLowerInvariant();
                if (normalProp == null && (l.Contains("normal") || l.Contains("bump"))) normalProp = n;
                else if (metalProp == null && (l.Contains("metal") || l.Contains("smooth") || l.Contains("mask") || l.Contains("gloss"))) metalProp = n;
                else if (baseProp == null && (l.Contains("tex") || l.Contains("base") || l.Contains("albedo") || l.Contains("main") || l.Contains("color") || l.Contains("diffuse"))) baseProp = n;
            }

            var brains = Object.FindObjectsByType<NPCBrain>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (brains.Length == 0) { Dlg("No encuentro ningún NPC (NPCBrain) en la escena abierta."); return; }

            var converted = new List<Material>();
            var report = new System.Text.StringBuilder();

            foreach (var brain in brains)
            {
                int matsHere = 0;
                foreach (var r in brain.GetComponentsInChildren<Renderer>(true))
                {
                    foreach (var mat in r.sharedMaterials)
                    {
                        if (mat == null) continue;

                        // Guardar sus texturas actuales antes de cambiar el shader.
                        var tex = new Dictionary<string, Texture>();
                        ReadSavedTextures(mat, tex);
                        Texture albedo = First(tex, "_BaseMap", "_Albedo", "_MainTex", "_BaseColorMap");
                        Texture normal = First(tex, "_BumpMap", "_Normal", "_NormalMap");
                        Texture metal  = First(tex, "_MetallicGlossMap", "_MetallicSmoothness", "_MaskMap");

                        mat.shader = curved;
                        if (baseProp   != null && albedo != null) mat.SetTexture(baseProp, albedo);
                        if (normalProp != null && normal != null) mat.SetTexture(normalProp, normal);
                        if (metalProp  != null && metal  != null) mat.SetTexture(metalProp, metal);
                        // por si el shader tiene además nombres estándar
                        if (albedo != null && mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", albedo);

                        EditorUtility.SetDirty(mat);
                        if (!converted.Contains(mat)) converted.Add(mat);
                        matsHere++;
                    }
                }
                report.AppendLine($"• {brain.name}: {matsHere} material(es)");
            }

            AssetDatabase.SaveAssets();
            int addedToSetter = AddToSetter(converted);

            Dlg($"NPCs pasados a Curved World: {brains.Length}\n" +
                $"Materiales cambiados: {converted.Count}\n" +
                $"Añadidos al CurvedWorldOriginSetter: {addedToSetter}\n\n{report}\n" +
                (addedToSetter == 0
                    ? "⚠ No encontré CurvedWorldOriginSetter en la escena: añádelo y vuelve a ejecutar, o no se curvarán."
                    : "Listo: deberían curvarse como la florista (no flotar). Guarda la escena."));
            Debug.Log($"[Sprout] NPCs -> CurvedWorld: {converted.Count} materiales, setter {addedToSetter}.");
        }

        private static Shader FindCurvedShader()
        {
            Shader best = null;
            foreach (var g in AssetDatabase.FindAssets("CurvedWorld t:Shader"))
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                if (!p.EndsWith(".shadergraph")) continue;
                var s = AssetDatabase.LoadAssetAtPath<Shader>(p);
                if (s == null) continue;
                if (p.EndsWith("/CurvedWorld.shadergraph")) return s;
                best = s;
            }
            return best;
        }

        private static int AddToSetter(List<Material> mats)
        {
            if (mats.Count == 0) return 0;
            foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (mb == null || mb.GetType().Name != "CurvedWorldOriginSetter") continue;
                var so = new SerializedObject(mb);
                var arr = so.FindProperty("curvedWorldMaterials");
                if (arr == null || !arr.isArray) return 0;

                var existing = new HashSet<Object>();
                for (int i = 0; i < arr.arraySize; i++) existing.Add(arr.GetArrayElementAtIndex(i).objectReferenceValue);

                int added = 0;
                foreach (var m in mats)
                {
                    if (existing.Contains(m)) continue;
                    arr.arraySize++;
                    arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = m;
                    existing.Add(m);
                    added++;
                }
                so.ApplyModifiedProperties();
                return added;
            }
            return 0;
        }

        private static void ReadSavedTextures(Material mat, Dictionary<string, Texture> tex)
        {
            var so = new SerializedObject(mat);
            var te = so.FindProperty("m_SavedProperties.m_TexEnvs");
            if (te == null) return;
            for (int i = 0; i < te.arraySize; i++)
            {
                var e = te.GetArrayElementAtIndex(i);
                string name = e.FindPropertyRelative("first").stringValue;
                var t = e.FindPropertyRelative("second.m_Texture").objectReferenceValue as Texture;
                if (!string.IsNullOrEmpty(name) && t != null && !tex.ContainsKey(name)) tex[name] = t;
            }
        }

        private static Texture First(Dictionary<string, Texture> d, params string[] keys)
        { foreach (var k in keys) if (d.TryGetValue(k, out var v) && v != null) return v; return null; }

        private static void Dlg(string m) => EditorUtility.DisplayDialog("Sprout · Curved World NPCs", m, "OK");
    }
}
#endif
