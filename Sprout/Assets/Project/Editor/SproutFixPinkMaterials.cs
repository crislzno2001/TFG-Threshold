#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Sprout.EditorTools
{
    /// <summary>
    /// Uso único: arregla los materiales ROSA (o BLANCOS, si ya los convertiste mal) de un pack
    /// Built-in importado en un proyecto URP. Los pasa a "Universal Render Pipeline/Lit" mapeando
    /// correctamente las texturas custom del pack (p. ej. Suntail/Raygeas usa _Albedo, _Normal,
    /// _MetallicSmoothness en vez de _BaseMap/_BumpMap/_MetallicGlossMap).
    ///
    /// - Lee las texturas de los DATOS GUARDADOS del material (m_SavedProperties), así que funciona
    ///   aunque el shader ya se haya cambiado a URP/Lit y se vea blanco.
    /// - La vegetación (Foliage / grass / leaf / tree…) la pone en modo RECORTE ALFA + DOBLE CARA.
    /// - El AGUA (GrabPass) NO se puede portar a URP/Lit: esos materiales se SALTAN (avisa).
    ///
    /// Trabaja sobre la carpeta seleccionada en Project; si no, sobre "Assets/Suntail Village".
    /// Menú:  Tools/Sprout/Fix Pink Materials (URP)
    /// </summary>
    public static class SproutFixPinkMaterials
    {
        [MenuItem("Tools/Sprout/Fix Pink Materials (URP)")]
        public static void Fix()
        {
            string folder = null;
            if (Selection.activeObject != null)
            {
                string p = AssetDatabase.GetAssetPath(Selection.activeObject);
                if (!string.IsNullOrEmpty(p) && AssetDatabase.IsValidFolder(p)) folder = p;
            }
            if (folder == null && AssetDatabase.IsValidFolder("Assets/Suntail Village")) folder = "Assets/Suntail Village";
            if (folder == null)
            {
                EditorUtility.DisplayDialog("Sprout",
                    "Selecciona en Project la carpeta del pack (p. ej. Suntail Village) y vuelve a darle.", "OK");
                return;
            }

            var urp = Shader.Find("Universal Render Pipeline/Lit");
            if (urp == null) { EditorUtility.DisplayDialog("Sprout", "No encuentro URP/Lit (¿está URP instalado?)", "OK"); return; }

            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { folder });
            int changed = 0, foliage = 0, skippedWater = 0, alreadyOk = 0, guessed = 0;

            // Índice de TODAS las texturas del pack, para adivinar la albedo por nombre cuando el
            // material perdió la referencia (p. ej. lo cambiaste tú a mano a URP/Lit y se borró _Albedo).
            var texIndex = new List<KeyValuePair<string, Texture>>();
            foreach (var tg in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
            {
                string tp = AssetDatabase.GUIDToAssetPath(tg);
                var t2 = AssetDatabase.LoadAssetAtPath<Texture>(tp);
                if (t2 != null) texIndex.Add(new KeyValuePair<string, Texture>(
                    System.IO.Path.GetFileNameWithoutExtension(tp).ToLowerInvariant(), t2));
            }

            try
            {
                AssetDatabase.StartAssetEditing(); // batch: evita reimportar uno a uno (mucho más rápido)
                for (int gi = 0; gi < guids.Length; gi++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[gi]);
                    if (EditorUtility.DisplayCancelableProgressBar("Sprout · Fix Pink",
                        System.IO.Path.GetFileName(path), (float)gi / Mathf.Max(1, guids.Length)))
                        break;

                    var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (mat == null) continue;

                    string oldShader = mat.shader != null ? mat.shader.name.ToLowerInvariant() : "";
                    string matName = mat.name.ToLowerInvariant();

                    // AGUA: GrabPass no existe en URP -> no se puede portar a Lit. Saltar.
                    if (oldShader.Contains("water") || matName.Contains("water"))
                    { skippedWater++; continue; }

                    // Leer texturas/colores/floats GUARDADOS (persisten aunque el shader ya no los use)
                    var tex = new Dictionary<string, Texture>();
                    var col = new Dictionary<string, Color>();
                    var flt = new Dictionary<string, float>();
                    ReadSaved(mat, tex, col, flt);

                    bool wasFoliage = oldShader.Contains("foliage") || flt.ContainsKey("_AlphaCutoff")
                        || HasAny(matName, "grass", "leaf", "leaves", "tree", "bush", "foliage", "plant", "ivy", "fern", "flower", "branch");

                    // Si ya está en URP/Lit Y tiene base map, lo damos por bueno (salvo que sea vegetación sin recorte)
                    if (oldShader.StartsWith("universal render pipeline") && mat.GetTexture("_BaseMap") != null && !wasFoliage)
                    { alreadyOk++; continue; }

                    Texture baseMap = First(tex, "_Albedo", "_MainTex", "_BaseMap", "_BaseColorMap");
                    Texture normal  = First(tex, "_Normal", "_BumpMap", "_NormalMap");
                    Texture metal   = First(tex, "_MetallicSmoothness", "_MetallicGlossMap", "_MaskMap");

                    // Si perdió la textura guardada, adivinarla por nombre entre las texturas del pack.
                    if (baseMap == null) { baseMap = GuessTexture(matName, texIndex, false); if (baseMap != null) guessed++; }
                    if (normal == null) normal = GuessTexture(matName, texIndex, true);
                    Color baseCol   = FirstCol(col, "_Color", "_MainColor", "_BaseColor", "_TintColor");
                    baseCol.a = 1f; // evitar que un alpha 0 del pack lo vuelva invisible
                    float smooth    = FirstFlt(flt, 0.15f, "_SurfaceSmoothness", "_Smoothness", "_Glossiness");

                    mat.shader = urp;
                    if (baseMap != null) mat.SetTexture("_BaseMap", baseMap);
                    mat.SetColor("_BaseColor", baseCol);
                    if (normal != null) { mat.SetTexture("_BumpMap", normal); mat.EnableKeyword("_NORMALMAP"); }
                    if (metal != null) { mat.SetTexture("_MetallicGlossMap", metal); mat.EnableKeyword("_METALLICSPECGLOSSMAP"); }
                    mat.SetFloat("_Metallic", 0f);
                    mat.SetFloat("_Smoothness", smooth);

                    if (wasFoliage)
                    {
                        // Recorte alfa (hojas) + doble cara (se ven por los dos lados)
                        mat.SetFloat("_AlphaClip", 1f);
                        mat.EnableKeyword("_ALPHATEST_ON");
                        mat.SetFloat("_Cutoff", FirstFlt(flt, 0.4f, "_AlphaCutoff", "_Cutoff"));
                        mat.SetFloat("_Cull", 0f); // Off
                        mat.renderQueue = 2450; // AlphaTest
                        foliage++;
                    }

                    EditorUtility.SetDirty(mat);
                    changed++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
            }

            EditorUtility.DisplayDialog("Sprout",
                $"Convertidos a URP/Lit: {changed}  (de ellos vegetación: {foliage})\n" +
                $"Textura recuperada por nombre: {guessed}\n" +
                $"Ya estaban bien: {alreadyOk}\n" +
                $"Agua saltada (GrabPass, no portable): {skippedWater}\n" +
                $"Carpeta: {folder}\n\n" +
                (skippedWater > 0
                    ? "El AGUA seguirá rara: necesita un shader de agua de URP aparte (no es portable automáticamente)."
                    : "Listo."), "OK");
            Debug.Log($"[Sprout] Fix pink: {changed} -> URP/Lit ({foliage} vegetación), agua saltada {skippedWater}, ya ok {alreadyOk}");
        }

        // ── helpers ──────────────────────────────────────────────────────────

        private static void ReadSaved(Material mat, Dictionary<string, Texture> tex,
                                      Dictionary<string, Color> col, Dictionary<string, float> flt)
        {
            var so = new SerializedObject(mat);
            var texEnvs = so.FindProperty("m_SavedProperties.m_TexEnvs");
            if (texEnvs != null)
                for (int i = 0; i < texEnvs.arraySize; i++)
                {
                    var e = texEnvs.GetArrayElementAtIndex(i);
                    string name = e.FindPropertyRelative("first").stringValue;
                    var t = e.FindPropertyRelative("second.m_Texture").objectReferenceValue as Texture;
                    if (!string.IsNullOrEmpty(name) && t != null && !tex.ContainsKey(name)) tex[name] = t;
                }
            var colors = so.FindProperty("m_SavedProperties.m_Colors");
            if (colors != null)
                for (int i = 0; i < colors.arraySize; i++)
                {
                    var e = colors.GetArrayElementAtIndex(i);
                    string name = e.FindPropertyRelative("first").stringValue;
                    if (!string.IsNullOrEmpty(name) && !col.ContainsKey(name))
                        col[name] = e.FindPropertyRelative("second").colorValue;
                }
            var floats = so.FindProperty("m_SavedProperties.m_Floats");
            if (floats != null)
                for (int i = 0; i < floats.arraySize; i++)
                {
                    var e = floats.GetArrayElementAtIndex(i);
                    string name = e.FindPropertyRelative("first").stringValue;
                    if (!string.IsNullOrEmpty(name) && !flt.ContainsKey(name))
                        flt[name] = e.FindPropertyRelative("second").floatValue;
                }
        }

        private static Texture First(Dictionary<string, Texture> d, params string[] keys)
        { foreach (var k in keys) if (d.TryGetValue(k, out var v) && v != null) return v; return null; }

        private static Color FirstCol(Dictionary<string, Color> d, params string[] keys)
        { foreach (var k in keys) if (d.TryGetValue(k, out var v)) return v; return Color.white; }

        private static float FirstFlt(Dictionary<string, float> d, float fallback, params string[] keys)
        { foreach (var k in keys) if (d.TryGetValue(k, out var v)) return v; return fallback; }

        private static bool HasAny(string s, params string[] keys)
        { foreach (var k in keys) if (s.Contains(k)) return true; return false; }

        // Adivina la textura de un material por parecido de nombre con las texturas del pack.
        private static Texture GuessTexture(string matName, List<KeyValuePair<string, Texture>> index, bool wantNormal)
        {
            var mt = Tokenize(matName);
            if (mt.Count == 0) return null;
            string[] notAlbedo = { "normal", "bump", "nrm", "metallic", "smooth", "mask", "rough",
                                   "specular", "emission", "emis", "occlusion", "_ao", "height",
                                   "displacement", "_n_", "_m_", "_s_" };
            string[] albedoHint = { "albedo", "basecolor", "base_color", "diffuse", "_alb", "_col", "color", "_d_", "_d." };

            Texture best = null; int bestScore = -1;
            foreach (var kv in index)
            {
                string tn = kv.Key;
                bool looksNormal = tn.Contains("normal") || tn.Contains("bump") || tn.Contains("nrm") || tn.EndsWith("_n");
                if (wantNormal && !looksNormal) continue;
                if (!wantNormal && (looksNormal || HasAny(tn, notAlbedo))) continue;

                var tt = Tokenize(tn);
                int shared = 0;
                foreach (var a in mt) if (tt.Contains(a)) shared++;
                if (shared == 0) continue;

                int score = shared * 10;
                if (!wantNormal && HasAny(tn, albedoHint)) score += 5;
                if (score > bestScore) { bestScore = score; best = kv.Value; }
            }
            return best;
        }

        private static List<string> Tokenize(string s)
        {
            var outp = new List<string>();
            foreach (var raw in System.Text.RegularExpressions.Regex.Split(s, "[^a-z0-9]+"))
            {
                if (string.IsNullOrEmpty(raw)) continue;
                string t = raw;
                if (int.TryParse(t, out int n)) t = n.ToString(); // 01 -> 1
                if (t == "mat" || t == "material" || t == "m" || t == "t" || t == "tex" || t == "texture") continue;
                outp.Add(t);
            }
            return outp;
        }
    }
}
#endif
