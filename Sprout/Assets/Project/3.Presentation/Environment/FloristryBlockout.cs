#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Sprout.World
{
    /// <summary>
    /// Genera el blockout de la escena INTERIOR de la floristería.
    ///
    /// Patrón Animal Crossing: el interior es una escena independiente, no parte
    /// del pueblo. La pared delantera (sur, la más cercana a la cámara) no se
    /// genera porque la cámara isométrica nunca la vería — se omite también
    /// para evitar oclusión.
    ///
    /// Elementos creados:
    /// - Suelo
    /// - Paredes norte, este, oeste (sin pared sur)
    /// - Mobiliario de referencia
    /// - SpawnPoint: dónde aparece el jugador al cargar la escena
    /// - ExitTrigger: collider que devuelve al jugador al pueblo
    ///
    /// USO: añadir a un GameObject vacío "FloristryInterior", pulsar
    /// "Generar Blockout". Reemplazar primitivas por FBX cuando estén listos.
    ///
    /// MonoBehaviour (Editor helper) — no incluir en builds finales.
    /// </summary>
    public sealed class FloristryBlockout : MonoBehaviour
    {
        // ── Dimensiones del local ──────────────────────────────────────────────

        [Header("Dimensiones de la floristería")]
        [Tooltip("Ancho del local en metros (eje X)")]
        [SerializeField] private float width = 10f;

        [Tooltip("Profundidad del local en metros (eje Z)")]
        [SerializeField] private float depth = 8f;

        [Tooltip("Altura de las paredes en metros (eje Y)")]
        [SerializeField] private float wallHeight = 3.5f;

        [Tooltip("Grosor de las paredes")]
        [SerializeField] private float wallThickness = 0.2f;

        // ── Spawn y salida ─────────────────────────────────────────────────────

        [Header("Punto de aparición y salida")]
        [Tooltip("Offset desde el centro del lado sur. Z negativo = más cerca de la salida.")]
        [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 0f, -3f);

        [Tooltip("Tamaño del trigger de salida (X, Y, Z).")]
        [SerializeField] private Vector3 exitTriggerSize = new Vector3(2f, 2.5f, 1.5f);

        [Tooltip("Distancia del trigger de salida desde el centro hacia el sur.")]
        [SerializeField] private float exitTriggerDistance = 3.8f;

        // ── Muebles de referencia ──────────────────────────────────────────────

        [Header("Mobiliario (posiciones de referencia)")]
        [SerializeField] private bool showCounter = true;
        [SerializeField] private bool showShelves = true;
        [SerializeField] private bool showFlowerDisplay = true;

        // ── Materiales ─────────────────────────────────────────────────────────

        [Header("Materiales de blockout (opcionales)")]
        [SerializeField] private Material floorMaterial;
        [SerializeField] private Material wallMaterial;
        [SerializeField] private Material furnitureMaterial;

        // ── Colores por defecto ────────────────────────────────────────────────

        private static readonly Color FloorColor = new Color(0.85f, 0.78f, 0.65f);
        private static readonly Color WallColor = new Color(0.92f, 0.90f, 0.85f);
        private static readonly Color FurnitureColor = new Color(0.55f, 0.75f, 0.55f);

        // ── API pública ────────────────────────────────────────────────────────

        [ContextMenu("Generar Blockout")]
        public void GenerateBlockout()
        {
            ClearChildren();

            BuildFloor();
            BuildWalls();
            BuildFurniture();
            BuildSpawnPoint();
            BuildExitTrigger();

            Debug.Log($"[FloristryBlockout] Interior generado: {width}m x {depth}m (sin pared sur).");
        }

        [ContextMenu("Limpiar Blockout")]
        public void ClearBlockout() => ClearChildren();

        // ── Suelo ──────────────────────────────────────────────────────────────

        private void BuildFloor()
        {
            CreateBox(
                "Floor",
                new Vector3(0f, -wallThickness * 0.5f, 0f),
                new Vector3(width, wallThickness, depth),
                floorMaterial, FloorColor);
        }

        // ── Paredes (sin pared sur) ────────────────────────────────────────────

        private void BuildWalls()
        {
            float halfW = width * 0.5f;
            float halfD = depth * 0.5f;
            float wallY = wallHeight * 0.5f;

            // Pared norte (al fondo, visible)
            CreateBox("Wall_North",
                new Vector3(0f, wallY, halfD),
                new Vector3(width, wallHeight, wallThickness),
                wallMaterial, WallColor);

            // Pared oeste (izquierda, visible)
            CreateBox("Wall_West",
                new Vector3(-halfW, wallY, 0f),
                new Vector3(wallThickness, wallHeight, depth),
                wallMaterial, WallColor);

            // Pared este (derecha, visible)
            CreateBox("Wall_East",
                new Vector3(halfW, wallY, 0f),
                new Vector3(wallThickness, wallHeight, depth),
                wallMaterial, WallColor);

            // NOTA: no se genera pared sur. La cámara isométrica nunca la ve.
            // Si en el futuro se quiere collider para limitar al jugador,
            // crear un cubo invisible (sin MeshRenderer) en su lugar.

            // Collider invisible en el sur para que el jugador no salga andando
            // (la salida se hace solo entrando en el ExitTrigger)
            var invisibleWall = CreateBox("InvisibleWall_South",
                new Vector3(0f, wallY, -halfD),
                new Vector3(width, wallHeight, wallThickness),
                null, Color.clear);
            // Desactivar el renderer pero mantener el collider
            var mr = invisibleWall.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = false;
        }

        // ── Mobiliario ─────────────────────────────────────────────────────────

        private void BuildFurniture()
        {
            if (showCounter)
            {
                // Mostrador central — orientado hacia la cámara (sur)
                CreateBox("Counter",
                    new Vector3(0f, 0.55f, 1f),
                    new Vector3(2.4f, 1.1f, 0.7f),
                    furnitureMaterial, FurnitureColor);
            }

            if (showShelves)
            {
                float shelfY = 1.0f;
                float halfW = width * 0.5f;

                // Estantería pegada a la pared norte (fondo)
                CreateBox("Shelf_Back",
                    new Vector3(0f, shelfY, (depth * 0.5f) - 0.35f),
                    new Vector3(5f, 2.0f, 0.5f),
                    furnitureMaterial, FurnitureColor);

                // Estantería izquierda
                CreateBox("Shelf_Left",
                    new Vector3(-halfW + 0.35f, shelfY, 0.5f),
                    new Vector3(0.5f, 2.0f, 3.0f),
                    furnitureMaterial, FurnitureColor);

                // Estantería derecha
                CreateBox("Shelf_Right",
                    new Vector3(halfW - 0.35f, shelfY, 0.5f),
                    new Vector3(0.5f, 2.0f, 3.0f),
                    furnitureMaterial, FurnitureColor);
            }

            if (showFlowerDisplay)
            {
                // Expositor de flores cerca de la salida
                CreateBox("Flower_Display",
                    new Vector3(-2.5f, 0.5f, -2f),
                    new Vector3(1.5f, 1.0f, 1.0f),
                    furnitureMaterial, new Color(0.9f, 0.65f, 0.75f));
            }
        }

        // ── Spawn point ────────────────────────────────────────────────────────

        private void BuildSpawnPoint()
        {
            var spawn = new GameObject("SpawnPoint");
            spawn.transform.SetParent(transform, false);
            spawn.transform.localPosition = new Vector3(
                spawnOffset.x,
                spawnOffset.y,
                (-depth * 0.5f) + Mathf.Abs(spawnOffset.z));

            // El SceneDestinationRegistry tag/identificador se asigna a mano
            // o con un script SceneDestination que pondremos en el siguiente paso.
        }

        // ── Exit trigger ───────────────────────────────────────────────────────

        private void BuildExitTrigger()
        {
            var exit = new GameObject("ExitTrigger");
            exit.transform.SetParent(transform, false);
            exit.transform.localPosition = new Vector3(
                0f,
                exitTriggerSize.y * 0.5f,
                -exitTriggerDistance);

            var collider = exit.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = exitTriggerSize;

            // El script DoorTrigger se añadirá en el siguiente paso.
            // Por ahora solo creamos el GameObject con el collider listo.
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private GameObject CreateBox(
            string boxName,
            Vector3 localPosition,
            Vector3 scale,
            Material mat,
            Color fallbackColor)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = boxName;
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = scale;

            var renderer = go.GetComponent<MeshRenderer>();

            if (mat != null)
            {
                renderer.sharedMaterial = mat;
            }
            else
            {
                // Material temporal con color para distinguir piezas en el editor
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");

                var tempMat = new Material(shader);
                tempMat.color = fallbackColor;
                renderer.sharedMaterial = tempMat;
            }

            return go;
        }

        private void ClearChildren()
        {
            var children = new System.Collections.Generic.List<GameObject>();
            foreach (Transform child in transform)
                children.Add(child.gameObject);

            foreach (var child in children)
            {
#if UNITY_EDITOR
                DestroyImmediate(child);
#else
                Destroy(child);
#endif
            }
        }

        // ── Gizmos: visualización en editor ────────────────────────────────────

        private void OnDrawGizmos()
        {
            // Marco del local
            Gizmos.color = new Color(0.4f, 0.8f, 0.4f, 0.4f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(
                new Vector3(0f, wallHeight * 0.5f, 0f),
                new Vector3(width, wallHeight, depth));

            // Spawn point
            Gizmos.color = Color.green;
            Vector3 spawnPos = new Vector3(
                spawnOffset.x,
                spawnOffset.y + 0.3f,
                (-depth * 0.5f) + Mathf.Abs(spawnOffset.z));
            Gizmos.DrawSphere(spawnPos, 0.25f);

            // Exit trigger
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawCube(
                new Vector3(0f, exitTriggerSize.y * 0.5f, -exitTriggerDistance),
                exitTriggerSize);

            Gizmos.matrix = Matrix4x4.identity;
        }
    }

    // ── Custom Inspector ──────────────────────────────────────────────────────

#if UNITY_EDITOR
    [CustomEditor(typeof(FloristryBlockout))]
    public sealed class FloristryBlockoutEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8);

            var blockout = (FloristryBlockout)target;

            if (GUILayout.Button("✿  Generar Blockout", GUILayout.Height(36)))
                blockout.GenerateBlockout();

            EditorGUILayout.Space(4);

            if (GUILayout.Button("Limpiar", GUILayout.Height(24)))
                blockout.ClearBlockout();

            EditorGUILayout.Space(8);
            EditorGUILayout.HelpBox(
                "Patrón Animal Crossing: esta es una escena INTERIOR independiente.\n" +
                "No tiene pared sur (la cámara nunca la ve).\n\n" +
                "El SpawnPoint marca dónde aparece el jugador al entrar.\n" +
                "El ExitTrigger devuelve al jugador al pueblo.\n\n" +
                "Cuando los FBX estén listos, eliminar este script y\n" +
                "reemplazar las primitivas por los modelos finales.",
                MessageType.Info);
        }
    }
#endif
}