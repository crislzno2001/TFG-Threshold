#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace OpenAI.Dialogue.Editor
{
    public class DialogueNodeView : Node
    {
        public DialogueNodeSO NodeSO { get; private set; }
        public Port InputPort { get; private set; }
        public Port OutputPort { get; private set; }

        public DialogueNodeView(DialogueNodeSO so)
        {
            NodeSO = so;
            title = so.name;
            viewDataKey = so.name + "_" + so.GetHashCode();

            BuildPorts();
            BuildBody();
            RefreshExpandedState();
            RefreshPorts();
        }

        private void BuildPorts()
        {
            InputPort = Port.Create<Edge>(
                Orientation.Horizontal, Direction.Input,
                Port.Capacity.Multi, typeof(bool));
            InputPort.portName = "Entrada";
            inputContainer.Add(InputPort);

            if (NodeSO is ChoiceNodeSO choiceSO)
            {
                if (choiceSO.choices != null)
                    foreach (var choice in choiceSO.choices)
                        AddChoicePort(choice.condition);
            }
            else
            {
                OutputPort = Port.Create<Edge>(
                    Orientation.Horizontal, Direction.Output,
                    Port.Capacity.Single, typeof(bool));
                OutputPort.portName = "Siguiente";
                outputContainer.Add(OutputPort);
            }
        }

        public Port AddChoicePort(string label)
        {
            var port = Port.Create<Edge>(
                Orientation.Horizontal, Direction.Output,
                Port.Capacity.Single, typeof(bool));
            port.portName = string.IsNullOrEmpty(label) ? "opción" : label;
            outputContainer.Add(port);
            RefreshPorts();
            return port;
        }

        private void BuildBody()
        {
            var contextField = new TextField("Contexto IA")
            {
                value = NodeSO.contextForAI,
                multiline = true
            };
            contextField.style.minWidth = 200;
            contextField.style.whiteSpace = WhiteSpace.Normal;
            contextField.RegisterValueChangedCallback(e =>
            {
                NodeSO.contextForAI = e.newValue;
                EditorUtility.SetDirty(NodeSO);
            });
            extensionContainer.Add(contextField);

            if (NodeSO is SpeechNodeSO speechSO)
            {
                titleContainer.style.backgroundColor = new Color(0.15f, 0.35f, 0.55f);

                var openingField = new TextField("Frase inicial") { value = speechSO.openingLine };
                openingField.RegisterValueChangedCallback(e =>
                {
                    speechSO.openingLine = e.newValue;
                    EditorUtility.SetDirty(NodeSO);
                });
                extensionContainer.Add(openingField);

                if (speechSO.transitions != null && speechSO.transitions.Count > 0)
                {
                    var header = new Label("── Transiciones ──");
                    header.style.unityFontStyleAndWeight = FontStyle.Bold;
                    header.style.marginTop = 6;
                    extensionContainer.Add(header);

                    for (int i = 0; i < speechSO.transitions.Count; i++)
                    {
                        int idx = i;
                        var condField = new TextField($"Condición {i + 1}") { value = speechSO.transitions[i].condition };
                        condField.style.minWidth = 180;
                        condField.RegisterValueChangedCallback(e =>
                        {
                            speechSO.transitions[idx].condition = e.newValue;
                            EditorUtility.SetDirty(NodeSO);
                        });
                        extensionContainer.Add(condField);
                    }
                }
            }
            else if (NodeSO is ChoiceNodeSO choiceSO)
            {
                titleContainer.style.backgroundColor = new Color(0.45f, 0.25f, 0.05f);

                if (choiceSO.choices == null)
                    choiceSO.choices = new System.Collections.Generic.List<ChoiceData>();

                var header = new Label("── Opciones ──");
                header.style.unityFontStyleAndWeight = FontStyle.Bold;
                header.style.marginTop = 6;
                extensionContainer.Add(header);

                var choicesContainer = new VisualElement();
                extensionContainer.Add(choicesContainer);

                void RefreshChoices()
                {
                    choicesContainer.Clear();
                    outputContainer.Clear();

                    for (int i = 0; i < choiceSO.choices.Count; i++)
                    {
                        int idx = i;
                        var row = new VisualElement();
                        row.style.flexDirection = FlexDirection.Row;

                        var condField = new TextField { value = choiceSO.choices[idx].condition };
                        condField.style.flexGrow = 1;
                        condField.RegisterValueChangedCallback(e =>
                        {
                            choiceSO.choices[idx].condition = e.newValue;
                            if (outputContainer.childCount > idx)
                                ((Port)outputContainer[idx]).portName = e.newValue;
                            EditorUtility.SetDirty(NodeSO);
                        });

                        var removeBtn = new Button(() =>
                        {
                            choiceSO.choices.RemoveAt(idx);
                            EditorUtility.SetDirty(NodeSO);
                            RefreshChoices();
                        }) { text = "✕" };
                        removeBtn.style.width = 24;

                        row.Add(condField);
                        row.Add(removeBtn);
                        choicesContainer.Add(row);

                        AddChoicePort(choiceSO.choices[i].condition);
                    }

                    RefreshPorts();
                }

                RefreshChoices();

                var addBtn = new Button(() =>
                {
                    choiceSO.choices.Add(new ChoiceData { condition = "nueva opción" });
                    EditorUtility.SetDirty(NodeSO);
                    RefreshChoices();
                }) { text = "+ Añadir opción" };
                addBtn.style.marginTop = 4;
                extensionContainer.Add(addBtn);
            }

            expanded = true;
        }
    }
}
#endif