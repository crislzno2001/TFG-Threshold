using UnityEditor;
using UnityEngine;
using ThresholdGame.Presentation.UI.MainMenu;

namespace Sprout.EditorTools
{
    /// <summary>
    /// Monta de un clic la escena del menú tipo "Savoring the Breeze": cámara + 3 capas de parallax
    /// curvado (cielo / casas / suelo) + un marcador donde arrastrar tu florista 3D andando.
    /// Menú: Tools/Sprout/Montar menú parallax (cozy).
    /// </summary>
    public static class MainMenuParallaxBuilder
    {
        [MenuItem("Tools/Sprout/Montar menú parallax (cozy)")]
        public static void Build()
        {
            var root = new GameObject("MainMenuParallax");
            Undo.RegisterCreatedObjectUndo(root, "Montar menú parallax");

            // ── Cámara ortográfica (menú 2D: sin perspectiva, el parallax lo da la velocidad) ──
            var camGo = new GameObject("MenuCamera");
            camGo.transform.SetParent(root.transform);
            camGo.transform.localPosition = new Vector3(0f, 0f, -10f);
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.85f, 0.93f, 0.83f); // verde menta suave de fondo

            // ── Capas (de lejos a cerca). Distinta velocidad = parallax. ──
            MakeLayer(root.transform, "Cielo",  y:  1.0f, z: 15f, width: 32f, height: 13f, curvature: 0f, speed: 0.010f);
            MakeLayer(root.transform, "Casas",  y:  0.3f, z: 10f, width: 28f, height: 7f,  curvature: 3f, speed: 0.030f);
            MakeLayer(root.transform, "Suelo",  y: -2.6f, z:  5f, width: 28f, height: 5f,  curvature: 3f, speed: 0.060f);

            // ── Marcador del personaje: MÁS CERCA que las capas (z menor) para que salga por delante ──
            var charMarker = new GameObject("PERSONAJE (arrastra aquí tu florista 3D)");
            charMarker.transform.SetParent(root.transform);
            charMarker.transform.localPosition = new Vector3(0f, -1.7f, 3f);

            Selection.activeGameObject = root;

            EditorUtility.DisplayDialog(
                "Menú parallax montado",
                "Creado 'MainMenuParallax'. Ahora:\n\n" +
                "1) En cada capa (Cielo/Casas/Suelo) arrastra su textura al campo 'Texture'.\n" +
                "2) Selecciona cada textura y pon Wrap Mode = Repeat.\n" +
                "3) Mete tu florista 3D dentro del marcador PERSONAJE y déjale la animación de andar en loop.\n" +
                "4) Dale a Play: las capas se mueven y la florista anda en el sitio.\n\n" +
                "Ajusta 'Curvature' y 'Scroll Speed' de cada capa a tu gusto.",
                "¡Vale!");
        }

        private static void MakeLayer(Transform parent, string name, float y, float z,
                                      float width, float height, float curvature, float speed)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent);
            go.transform.localPosition = new Vector3(0f, y, z);

            var layer = go.AddComponent<CurvedParallaxLayer>();
            var so = new SerializedObject(layer);
            so.FindProperty("width").floatValue = width;
            so.FindProperty("height").floatValue = height;
            so.FindProperty("curvature").floatValue = curvature;
            so.FindProperty("scrollSpeed").floatValue = speed;
            so.ApplyModifiedProperties();
        }
    }
}
