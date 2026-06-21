#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Sprout.EditorTools
{
    /// <summary>
    /// Uso único: genera una floristería low-poly "de relleno" (cubos/cilindros/esferas con
    /// materiales cozy) bajo un objeto raíz "Floristeria_Brote". No es arte de artista, pero es
    /// gratis, tuya y totalmente editable: muévela, repíntala o sustituye piezas por modelos de
    /// Kenney/Quaternius cuando quieras.
    ///
    /// Menú:  Tools/Sprout/Build Florist Shop
    /// Luego puedes borrar este archivo.
    /// </summary>
    public static class SproutShopBuilder
    {
        // Paleta cozy
        static readonly Color Wood   = new Color(0.69f, 0.53f, 0.37f);
        static readonly Color WoodHi = new Color(0.80f, 0.64f, 0.46f);
        static readonly Color Cream  = new Color(0.95f, 0.91f, 0.82f);
        static readonly Color Roof   = new Color(0.78f, 0.45f, 0.40f);
        static readonly Color Leaf   = new Color(0.42f, 0.62f, 0.40f);
        static readonly Color Pot    = new Color(0.80f, 0.46f, 0.34f);
        static readonly Color Soil   = new Color(0.34f, 0.26f, 0.20f);
        static readonly Color Glass  = new Color(0.70f, 0.85f, 0.90f);
        static readonly Color Rug    = new Color(0.84f, 0.55f, 0.62f);

        // Colores de las 7 flores
        static readonly Color[] Blooms = {
            new Color(0.60f,0.78f,0.92f), // Acuariana (azul suave)
            new Color(0.86f,0.27f,0.27f), // Brasa (rojo)
            new Color(0.55f,0.42f,0.62f), // Velada (morado apagado)
            new Color(0.98f,0.84f,0.30f), // Sol (amarillo)
            new Color(0.95f,0.58f,0.28f), // Inquieta (naranja)
            new Color(0.96f,0.96f,0.93f), // Crisálida (blanco)
            new Color(0.80f,0.76f,0.92f), // Ánima (lavanda pálido)
        };

        [MenuItem("Tools/Sprout/Build Florist Shop")]
        public static void Build()
        {
            var root = new GameObject("Floristeria_Brote");
            Undo.RegisterCreatedObjectUndo(root, "Build Florist Shop");

            // Suelo
            Box(root, "Suelo", new Vector3(0, -0.05f, 0), new Vector3(7, 0.1f, 7), Wood);
            Box(root, "Alfombra", new Vector3(0, 0.02f, 1.2f), new Vector3(2.2f, 0.04f, 1.6f), Rug);

            // Paredes (cream), dejando hueco de puerta delante
            Box(root, "ParedFondo",  new Vector3(0, 1.4f, -3.4f), new Vector3(7, 2.8f, 0.2f), Cream);
            Box(root, "ParedIzq",    new Vector3(-3.4f, 1.4f, 0), new Vector3(0.2f, 2.8f, 7), Cream);
            Box(root, "ParedDer",    new Vector3(3.4f, 1.4f, 0),  new Vector3(0.2f, 2.8f, 7), Cream);
            // Frente: dos tramos + dintel, hueco de puerta en el centro
            Box(root, "FrenteIzq",   new Vector3(-2.3f, 1.4f, 3.4f), new Vector3(2.2f, 2.8f, 0.2f), Cream);
            Box(root, "FrenteDer",   new Vector3(2.3f, 1.4f, 3.4f),  new Vector3(2.2f, 2.8f, 0.2f), Cream);
            Box(root, "Dintel",      new Vector3(0, 2.5f, 3.4f),     new Vector3(2.6f, 0.6f, 0.2f), Cream);

            // Tejado a dos aguas (dos cubos inclinados)
            var rA = Box(root, "TejadoA", new Vector3(-1.05f, 3.25f, 0), new Vector3(2.6f, 0.18f, 7.6f), Roof);
            rA.transform.localRotation = Quaternion.Euler(0, 0, 28);
            var rB = Box(root, "TejadoB", new Vector3(1.05f, 3.25f, 0), new Vector3(2.6f, 0.18f, 7.6f), Roof);
            rB.transform.localRotation = Quaternion.Euler(0, 0, -28);

            // Ventana en la pared del fondo (marco + cristal)
            Box(root, "Ventana", new Vector3(0, 1.7f, -3.3f), new Vector3(2.2f, 1.3f, 0.06f), Glass);
            Box(root, "MarcoV1", new Vector3(0, 1.05f, -3.28f), new Vector3(2.3f, 0.12f, 0.1f), Wood);
            Box(root, "MarcoV2", new Vector3(0, 2.35f, -3.28f), new Vector3(2.3f, 0.12f, 0.1f), Wood);
            Box(root, "MarcoV3", new Vector3(-1.1f, 1.7f, -3.28f), new Vector3(0.12f, 1.4f, 0.1f), Wood);
            Box(root, "MarcoV4", new Vector3(1.1f, 1.7f, -3.28f), new Vector3(0.12f, 1.4f, 0.1f), Wood);

            // Mostrador
            Box(root, "Mostrador",    new Vector3(-1.6f, 0.5f, -2.0f), new Vector3(2.4f, 1.0f, 0.9f), Wood);
            Box(root, "MostradorTop", new Vector3(-1.6f, 1.02f, -2.0f), new Vector3(2.6f, 0.12f, 1.1f), WoodHi);

            // Estanterías en la pared derecha
            Box(root, "Estante1", new Vector3(2.9f, 1.0f, -1.0f), new Vector3(0.6f, 0.1f, 3.0f), WoodHi);
            Box(root, "Estante2", new Vector3(2.9f, 1.8f, -1.0f), new Vector3(0.6f, 0.1f, 3.0f), WoodHi);

            // Jardinera de la ventana (fuera, delante)
            Box(root, "Jardinera", new Vector3(0, 0.9f, 3.6f), new Vector3(2.4f, 0.4f, 0.4f), Wood);

            // Flores: en estantes, mostrador y jardinera
            int c = 0;
            for (int i = 0; i < 3; i++) Flower(root, new Vector3(2.9f, 1.15f, -2.0f + i * 1.0f), Blooms[c++ % 7]);
            for (int i = 0; i < 3; i++) Flower(root, new Vector3(2.9f, 1.95f, -2.0f + i * 1.0f), Blooms[c++ % 7]);
            for (int i = 0; i < 4; i++) Flower(root, new Vector3(-0.9f + i * 0.6f, 1.0f, 3.6f), Blooms[c++ % 7]);
            Flower(root, new Vector3(-2.2f, 1.1f, -2.0f), Blooms[3]); // una en el mostrador
            Flower(root, new Vector3(-1.0f, 1.1f, -2.0f), Blooms[6]);

            // Cartel "Brote" sobre la puerta
            Box(root, "Cartel", new Vector3(0, 2.95f, 3.5f), new Vector3(1.6f, 0.5f, 0.1f), WoodHi);
            var t = new GameObject("Texto_Brote");
            t.transform.SetParent(root.transform);
            t.transform.localPosition = new Vector3(0, 2.95f, 3.58f);
            var tm = t.AddComponent<TextMesh>();
            tm.text = "Brote";
            tm.characterSize = 0.12f;
            tm.fontSize = 60;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = new Color(0.40f, 0.26f, 0.22f);

            // Taburete
            Cyl(root, "Taburete", new Vector3(-0.2f, 0.35f, -1.4f), new Vector3(0.45f, 0.35f, 0.45f), Wood);

            Selection.activeGameObject = root;
            EditorUtility.DisplayDialog("Sprout",
                "Floristería 'Brote' creada en la escena (objeto Floristeria_Brote).\n\n" +
                "Es low-poly de relleno: muévela, repíntala o cambia piezas por modelos de Kenney/" +
                "Quaternius cuando quieras. Puedes borrar este archivo.",
                "OK");
        }

        // ── helpers ──
        static Material Mat(Color col)
        {
            var sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var m = new Material(sh);
            m.color = col;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
            return m;
        }

        static GameObject Box(GameObject parent, string name, Vector3 pos, Vector3 scale, Color col)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = name;
            g.transform.SetParent(parent.transform);
            g.transform.localPosition = pos;
            g.transform.localScale = scale;
            g.GetComponent<Renderer>().sharedMaterial = Mat(col);
            return g;
        }

        static GameObject Cyl(GameObject parent, string name, Vector3 pos, Vector3 scale, Color col)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            g.name = name;
            g.transform.SetParent(parent.transform);
            g.transform.localPosition = pos;
            g.transform.localScale = scale;
            g.GetComponent<Renderer>().sharedMaterial = Mat(col);
            return g;
        }

        static GameObject Sphere(GameObject parent, string name, Vector3 pos, Vector3 scale, Color col)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            g.name = name;
            g.transform.SetParent(parent.transform);
            g.transform.localPosition = pos;
            g.transform.localScale = scale;
            g.GetComponent<Renderer>().sharedMaterial = Mat(col);
            return g;
        }

        static void Flower(GameObject parent, Vector3 basePos, Color bloom)
        {
            var holder = new GameObject("Flor");
            holder.transform.SetParent(parent.transform);
            holder.transform.localPosition = basePos;
            Cyl(holder, "Maceta", new Vector3(0, 0, 0), new Vector3(0.22f, 0.16f, 0.22f), Pot);
            Cyl(holder, "Tierra", new Vector3(0, 0.16f, 0), new Vector3(0.2f, 0.02f, 0.2f), Soil);
            Cyl(holder, "Tallo",  new Vector3(0, 0.35f, 0), new Vector3(0.04f, 0.2f, 0.04f), Leaf);
            Sphere(holder, "Flor", new Vector3(0, 0.55f, 0), new Vector3(0.22f, 0.18f, 0.22f), bloom);
        }
    }
}
#endif
