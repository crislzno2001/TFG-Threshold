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

            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            nodeCreationRequest = ctx =>
            {
                var provider = ScriptableObject.CreateInstance<NodeSearchProvider>();
                provider.Init(this, ctx.screenMousePosition);
                SearchWindow.Open(new SearchWindowContext(ctx.screenMousePosition), provider);
            };
        }

        /// <summary>Creates a node view at the given position with an optional saved size.</summary>
        public DialogueNodeView CreateNodeView(DialogueNodeSO so, Vector2 position, Vector2 size = default)
        {
            if (size.x < 10) size = new Vector2(260, 200);

            var view = new DialogueNodeView(so, size);
            view.SetPosition(new Rect(position, size));
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