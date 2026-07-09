#if UNITY_EDITOR
using UnityEditor;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace OpenAI.Dialogue.Editor
{
    public class NodeSearchProvider : ScriptableObject, ISearchWindowProvider
    {
        private DialogueGraphView _graphView;
        private Vector2 _screenMousePos;

        public void Init(DialogueGraphView graphView, Vector2 screenMousePos)
        {
            _graphView = graphView;
            _screenMousePos = screenMousePos;
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            return new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("Crear nodo"), 0),
                new SearchTreeEntry(new GUIContent("Speech Node")) { level = 1, userData = typeof(SpeechNodeSO) },
                new SearchTreeEntry(new GUIContent("Choice Node")) { level = 1, userData = typeof(ChoiceNodeSO) },
                new SearchTreeEntry(new GUIContent("Conversation Node")) { level = 1, userData = typeof(ConversationNodeSO) },
            };
        }

        public bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
        {
            var type = entry.userData as System.Type;
            if (type == null) return false;

            var so = ScriptableObject.CreateInstance(type) as DialogueNodeSO;
            so.name = type.Name.Replace("SO", "");

            _graphView.CreateNodeView(so, _screenMousePos);
            return true;
        }
    }
}
#endif