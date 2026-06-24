#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Sprout.EditorTools
{
    /// <summary>
    /// Uso único: selecciona en la escena un objeto que se vea mal (farola azul, árbol negro…) y dale a
    /// este botón. Imprime, por cada material, el SHADER y QUÉ TEXTURA hay en CADA hueco de textura, más
    /// el color base. Así vemos si una normal se metió en el color, si falta el albedo, etc.
    ///
    /// Menú:  Tools/Sprout/Diagnose Selected Material
    /// </summary>
    public static class SproutDiagnoseMaterial
    {
        [MenuItem("Tools/Sprout/Diagnose Selected Material")]
        public static void Diagnose()
        {
            if (Selection.gameObjects == null || Selection.gameObjects.Length == 0)
            { EditorUtility.DisplayDialog("Sprout", "Selecciona en la escena el objeto que se ve mal.", "OK"); return; }

            var sb = new StringBuilder();
            foreach (var go in Selection.gameObjects)
            {
                foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                {
                    foreach (var mat in r.sharedMaterials)
                    {
                        if (mat == null) { sb.Append($"\n[{r.name}] material NULL\n"); continue; }
                        sb.Append($"\n■ {mat.name}  (shader: {mat.shader.name})\n");

                        var shader = mat.shader;
                        int count = ShaderUtil.GetPropertyCount(shader);
                        for (int i = 0; i < count; i++)
                        {
                            if (ShaderUtil.GetPropertyType(shader, i) != ShaderUtil.ShaderPropertyType.TexEnv) continue;
                            string prop = ShaderUtil.GetPropertyName(shader, i);
                            var tex = mat.GetTexture(prop);
                            string texName = tex != null ? tex.name : "—(vacío)";
                            string warn = "";
                            string pl = prop.ToLowerInvariant();
                            string tl = texName.ToLowerInvariant();
                            bool propIsColor = pl.Contains("base") || pl.Contains("albedo") || pl.Contains("main") || pl.Contains("color") || pl.Contains("diffuse") || pl.Contains("texture2d");
                            bool texIsNormal = tl.Contains("normal") || tl.Contains("_nor") || tl.Contains("bump") || tl.Contains("_n");
                            bool texIsMetal = tl.Contains("metal") || tl.Contains("_met") || tl.Contains("smooth") || tl.Contains("mask");
                            if (propIsColor && tex != null && (texIsNormal || texIsMetal)) warn = "  ⚠ ¡textura equivocada en el color!";
                            if (propIsColor && tex == null) warn = "  ⚠ falta el albedo (saldrá negro/blanco)";

                            sb.Append($"   · {prop} = {texName}{warn}\n");
                        }

                        if (mat.HasProperty("_BaseColor")) sb.Append($"   · _BaseColor = {mat.GetColor("_BaseColor")}\n");
                        else if (mat.HasProperty("_Color")) sb.Append($"   · _Color = {mat.GetColor("_Color")}\n");
                    }
                    break; // primer renderer por objeto, suficiente
                }
            }

            Debug.Log("[Sprout] DIAGNÓSTICO MATERIAL\n" + sb);
            // El diálogo corta si es muy largo; lo importante está en la consola.
            string shortMsg = sb.Length > 1200 ? sb.ToString(0, 1200) + "\n...(resto en la consola)" : sb.ToString();
            EditorUtility.DisplayDialog("Sprout · Diagnóstico material", shortMsg, "OK");
        }
    }
}
#endif
