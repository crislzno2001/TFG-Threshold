#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace OpenAI.Dialogue.Editor
{
    public static class DialogueGraphSerializer
    {
        public static void Save(DialogueGraphView view, DialogueGraphSO graphSO)
        {
            string assetPath = AssetDatabase.GetAssetPath(graphSO);

            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
                if (asset != graphSO && asset is DialogueNodeSO)
                    AssetDatabase.RemoveObjectFromAsset(asset);

            graphSO.nodes.Clear();
            graphSO.nodePositions.Clear();

            var nodeViews = view.NodeViews.ToList();

            foreach (var nodeView in nodeViews)
            {
                var so = nodeView.NodeSO;
                so.nextNodes.Clear();
                so.name = nodeView.title;
                AssetDatabase.AddObjectToAsset(so, graphSO);
                graphSO.nodes.Add(so);
                graphSO.nodePositions.Add(new NodePositionData
                {
                    nodeId = so.GetHashCode().ToString(),
                    position = nodeView.GetPosition().position
                });
            }

            foreach (var edge in view.edges.ToList())
            {
                var fromView = edge.output.node as DialogueNodeView;
                var toView = edge.input.node as DialogueNodeView;
                if (fromView == null || toView == null) continue;

                if (fromView.NodeSO is ChoiceNodeSO choiceSO)
                {
                    int portIndex = fromView.outputContainer.IndexOf(edge.output);
                    if (portIndex >= 0 && portIndex < choiceSO.choices.Count)
                        choiceSO.choices[portIndex].nextNode = toView.NodeSO;
                }
                else
                {
                    if (!fromView.NodeSO.nextNodes.Contains(toView.NodeSO))
                        fromView.NodeSO.nextNodes.Add(toView.NodeSO);
                }
            }

            var nodesWithInput = view.edges.ToList()
                .Select(e => (e.input.node as DialogueNodeView)?.NodeSO)
                .Where(s => s != null).ToHashSet();

            graphSO.entryNode = graphSO.nodes.FirstOrDefault(n => !nodesWithInput.Contains(n));

            EditorUtility.SetDirty(graphSO);
            AssetDatabase.SaveAssets();
        }

        public static void Load(DialogueGraphView view, DialogueGraphSO graphSO)
        {
            view.DeleteElements(view.graphElements.ToList());

            var map = new Dictionary<DialogueNodeSO, DialogueNodeView>();

            foreach (var nodeSO in graphSO.nodes)
            {
                var posData = graphSO.nodePositions
                    .Find(p => p != null && p.nodeId == nodeSO.GetHashCode().ToString());
                var pos = posData != null ? posData.position : Vector2.zero;
                map[nodeSO] = view.CreateNodeView(nodeSO, pos);
            }

            foreach (var nodeSO in graphSO.nodes)
            {
                var fromView = map[nodeSO];

                if (nodeSO is ChoiceNodeSO choiceSO)
                {
                    for (int i = 0; i < choiceSO.choices.Count; i++)
                    {
                        var next = choiceSO.choices[i].nextNode;
                        if (next == null || !map.ContainsKey(next)) continue;
                        if (fromView.outputContainer.childCount <= i) continue;

                        var outPort = fromView.outputContainer[i] as Port;
                        if (outPort == null) continue;

                        view.AddElement(outPort.ConnectTo(map[next].InputPort));
                    }
                }
                else
                {
                    foreach (var next in nodeSO.nextNodes)
                    {
                        if (!map.ContainsKey(next)) continue;
                        view.AddElement(fromView.OutputPort.ConnectTo(map[next].InputPort));
                    }
                }
            }
        }
    }
}
#endif