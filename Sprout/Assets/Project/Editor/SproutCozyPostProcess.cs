#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Sprout.EditorTools
{
    /// <summary>
    /// Uso único: crea un look "cozy" unificado para que el escenario (pueblo Suntail) y los personajes
    /// chibi (florista, Mochi) parezcan del mismo juego. Hace 3 cosas:
    ///   1) Crea un Volume Profile con: Tonemapping (Neutral), Color Adjustments (cálido + algo de
    ///      saturación/contraste), White Balance templado, Bloom suave y Vignette.
    ///   2) Crea un "Global Volume - Cozy" en la escena que usa ese perfil.
    ///   3) Activa Post Processing en las cámaras de la escena.
    ///
    /// Ajusta los valores en el Volume del inspector hasta que te guste; esto es solo el punto de partida.
    /// Menú:  Tools/Sprout/Setup Cozy Post-Processing
    /// </summary>
    public static class SproutCozyPostProcess
    {
        private const string Dir = "Assets/Project/Settings";
        private const string ProfilePath = Dir + "/CozyPostProcess.asset";

        [MenuItem("Tools/Sprout/Setup Cozy Post-Processing")]
        public static void Setup()
        {
            // Carpeta
            if (!AssetDatabase.IsValidFolder("Assets/Project")) AssetDatabase.CreateFolder("Assets", "Project");
            if (!AssetDatabase.IsValidFolder(Dir)) AssetDatabase.CreateFolder("Assets/Project", "Settings");

            // Perfil (reescribe si ya existe)
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }
            else
            {
                for (int i = profile.components.Count - 1; i >= 0; i--)
                {
                    Object.DestroyImmediate(profile.components[i], true);
                }
                profile.components.Clear();
            }

            // Tonemapping neutro (evita colores lavados o quemados)
            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.Neutral);

            // Color cálido + vida
            var ca = profile.Add<ColorAdjustments>(true);
            ca.postExposure.Override(0.05f);
            ca.contrast.Override(8f);                                   // -100..100
            ca.saturation.Override(12f);                                // -100..100
            ca.colorFilter.Override(new Color(1.00f, 0.97f, 0.92f));    // tinte cálido suave

            // Balance de blancos templado (más acogedor)
            var wb = profile.Add<WhiteBalance>(true);
            wb.temperature.Override(12f);   // + = más cálido
            wb.tint.Override(2f);

            // Bloom suave (brillos amables, look cozy)
            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(0.7f);
            bloom.threshold.Override(0.9f);
            bloom.scatter.Override(0.6f);
            bloom.tint.Override(new Color(1f, 0.96f, 0.9f));

            // Viñeta sutil (enfoca el centro)
            var vig = profile.Add<Vignette>(true);
            vig.intensity.Override(0.28f);
            vig.smoothness.Override(0.45f);

            EditorUtility.SetDirty(profile);

            // Volume global en la escena (borra uno anterior con el mismo nombre)
            var existing = GameObject.Find("Global Volume - Cozy");
            if (existing != null) Object.DestroyImmediate(existing);
            var go = new GameObject("Global Volume - Cozy");
            var vol = go.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 1f;
            vol.sharedProfile = profile;

            // Activar post en todas las cámaras
            int cams = 0;
            foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var data = cam.GetUniversalAdditionalCameraData();
                if (data != null) { data.renderPostProcessing = true; EditorUtility.SetDirty(cam); cams++; }
            }

            AssetDatabase.SaveAssets();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sprout",
                "Look cozy creado.\n\n" +
                $"· Perfil: {ProfilePath}\n" +
                "· Volume: 'Global Volume - Cozy' en la escena\n" +
                $"· Post Processing activado en {cams} cámara(s)\n\n" +
                "Selecciona el 'Global Volume - Cozy' y juega con Bloom / Color Adjustments / White Balance " +
                "hasta que te guste. Si no ves cambios, revisa que el Renderer de URP tenga Post-processing activado.",
                "OK");
            Debug.Log("[Sprout] Cozy post-processing listo. Cámaras con post: " + cams);
        }
    }
}
#endif
