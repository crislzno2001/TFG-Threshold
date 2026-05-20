#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace OpenAI.Dialogue.Editor
{
    public class DialogueGraphEditorWindow : EditorWindow
    {
        private DialogueGraphView _graphView;
        private DialogueGraphSO _currentGraph;

        [MenuItem("Tools/Dialogue Graph Editor")]
        public static void Open()
        {
            var window = GetWindow<DialogueGraphEditorWindow>();
            window.titleContent = new GUIContent("Dialogue Graph");
            window.minSize = new Vector2(800, 500);
        }

        public static void OpenWithGraph(DialogueGraphSO graph)
        {
            var window = GetWindow<DialogueGraphEditorWindow>();
            window.titleContent = new GUIContent($"Dialogue — {graph.name}");
            window.LoadGraph(graph);
        }

        private void OnEnable()
        {
            BuildGraphView();
            BuildToolbar();
        }

        private void OnDisable()
        {
            if (_graphView != null)
                rootVisualElement.Remove(_graphView);
        }

        private void BuildGraphView()
        {
            _graphView = new DialogueGraphView(this);
            _graphView.style.flexGrow = 1;
            rootVisualElement.Add(_graphView);
        }

        private void BuildToolbar()
        {
            var toolbar = new Toolbar();

            var saveBtn = new ToolbarButton(SaveGraph) { text = "Guardar" };
            var loadBtn = new ToolbarButton(PickAndLoadGraph) { text = "Cargar grafo" };
            var graphLabel = new Label("Sin grafo") { name = "graph-label" };
            graphLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
            graphLabel.style.marginLeft = 8;
            graphLabel.style.alignSelf = Align.Center;

            toolbar.Add(saveBtn);
            toolbar.Add(loadBtn);
            toolbar.Add(graphLabel);

            rootVisualElement.Insert(0, toolbar);
        }

        public void LoadGraph(DialogueGraphSO graph)
        {
            _currentGraph = graph;
            var label = rootVisualElement.Q<Label>("graph-label");
            if (label != null) label.text = graph.name;
            DialogueGraphSerializer.Load(_graphView, graph);
        }

        private void SaveGraph()
        {
            if (_currentGraph == null)
            {
                string path = EditorUtility.SaveFilePanelInProject(
                    "Guardar grafo", "NewDialogueGraph", "asset", "Elige dónde guardar");
                if (string.IsNullOrEmpty(path)) return;

                _currentGraph = ScriptableObject.CreateInstance<DialogueGraphSO>();
                AssetDatabase.CreateAsset(_currentGraph, path);
            }

            DialogueGraphSerializer.Save(_graphView, _currentGraph);

            var label = rootVisualElement.Q<Label>("graph-label");
            if (label != null) label.text = _currentGraph.name;

            Debug.Log($"[Dialogue] Grafo guardado: {_currentGraph.name}");
        }

        private void PickAndLoadGraph()
        {
            string path = EditorUtility.OpenFilePanelWithFilters(
                "Abrir grafo", "Assets", new[] { "Asset", "asset" });
            if (string.IsNullOrEmpty(path)) return;

            path = "Assets" + path.Substring(Application.dataPath.Length);
            var graph = AssetDatabase.LoadAssetAtPath<DialogueGraphSO>(path);
            if (graph != null) LoadGraph(graph);
        }
    }
}
#endif
