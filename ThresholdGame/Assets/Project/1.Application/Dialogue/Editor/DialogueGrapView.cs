#if UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace OpenAI.Dialogue.Editor
{
    public class DialogueGraphView : GraphView
    {
        private readonly DialogueGraphEditorWindow _window;

        public IEnumerable<DialogueNodeView> NodeViews =>
            nodes.ToList().OfType<DialogueNodeView>();

        public DialogueGraphView(DialogueGraphEditorWindow window)
        {
            _window = window;

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            // Grid de fondo
            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            // Menú contextual clic derecho
            nodeCreationRequest = ctx =>
            {
                var provider = ScriptableObject.CreateInstance<NodeSearchProvider>();
                provider.Init(this, ctx.screenMousePosition);
                SearchWindow.Open(new SearchWindowContext(ctx.screenMousePosition), provider);
            };
        }

        public DialogueNodeView CreateNodeView(DialogueNodeSO so, Vector2 position)
        {
            var view = new DialogueNodeView(so);
            view.SetPosition(new Rect(position, new Vector2(240, 150)));
            AddElement(view);
            return view;
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports.ToList()
                .Where(p => p != startPort
                         && p.node != startPort.node
                         && p.direction != startPort.direction)
                .ToList();
        }
    }
}
#endif
