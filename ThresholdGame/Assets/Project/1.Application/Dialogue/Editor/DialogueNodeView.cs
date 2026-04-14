#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace OpenAI.Dialogue.Editor
{
    public class DialogueNodeView : Node
    {
        public DialogueNodeSO NodeSO    { get; private set; }
        public Port           InputPort  { get; private set; }
        public Port           OutputPort { get; private set; }

        public string SceneName => _sceneNameField?.value ?? NodeSO.name;

        private TextField _sceneNameField;
        private VisualElement _body;
        private bool _collapsed = false;

        private bool _resizing;
        private Vector2 _resizeStart;
        private Vector2 _sizeStart;

        public DialogueNodeView(DialogueNodeSO so, Vector2 size = default)
        {
            NodeSO = so;
            viewDataKey = string.IsNullOrEmpty(so.nodeGuid)
                ? so.name + "_" + so.GetHashCode()
                : so.nodeGuid;

            titleContainer.Clear();

            BuildTitleBar();
            BuildPorts();
            BuildBody();
            BuildResizeHandle();

            RefreshExpandedState();
            RefreshPorts();

            if (size.x > 10)
                style.width = size.x;
        }

        private void BuildTitleBar()
        {
            titleContainer.style.flexDirection = FlexDirection.Row;
            titleContainer.style.alignItems = Align.Center;
            titleContainer.style.paddingLeft = 6;
            titleContainer.style.paddingRight = 4;

            _sceneNameField = new TextField
            {
                value = NodeSO.name
            };
            _sceneNameField.style.flexGrow = 1;
            _sceneNameField.style.unityFontStyleAndWeight = FontStyle.Bold;
            _sceneNameField.style.backgroundColor = new Color(0, 0, 0, 0);
            _sceneNameField.RegisterValueChangedCallback(e =>
            {
                NodeSO.name = e.newValue;
                EditorUtility.SetDirty(NodeSO);
            });
            titleContainer.Add(_sceneNameField);

            var collapseBtn = new Button(ToggleCollapse) { text = "▼" };
            collapseBtn.name = "collapse-btn";
            collapseBtn.style.width = 22;
            collapseBtn.style.height = 22;
            collapseBtn.style.fontSize = 10;
            collapseBtn.style.marginLeft = 4;
            titleContainer.Add(collapseBtn);

            if (NodeSO is SpeechNodeSO)
                titleContainer.style.backgroundColor = new Color(0.15f, 0.35f, 0.55f);
            else if (NodeSO is ChoiceNodeSO)
                titleContainer.style.backgroundColor = new Color(0.45f, 0.25f, 0.05f);
        }

        protected override void ToggleCollapse()
        {
            _collapsed = !_collapsed;
            _body.style.display = _collapsed ? DisplayStyle.None : DisplayStyle.Flex;

            var btn = titleContainer.Q<Button>("collapse-btn");
            if (btn != null) btn.text = _collapsed ? "▶" : "▼";
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
            _body = new VisualElement();
            _body.style.paddingLeft = 6;
            _body.style.paddingRight = 6;
            _body.style.paddingBottom = 6;

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
            _body.Add(contextField);

            BuildGateEditors(_body);

            if (NodeSO is SpeechNodeSO speechSO)
            {
                var openingField = new TextField("Frase inicial") { value = speechSO.openingLine };
                openingField.RegisterValueChangedCallback(e =>
                {
                    speechSO.openingLine = e.newValue;
                    EditorUtility.SetDirty(NodeSO);
                });
                _body.Add(openingField);

                if (speechSO.transitions != null && speechSO.transitions.Count > 0)
                {
                    var header = new Label("── Transiciones ──");
                    header.style.unityFontStyleAndWeight = FontStyle.Bold;
                    header.style.marginTop = 6;
                    _body.Add(header);

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
                        _body.Add(condField);
                    }
                }
            }
            else if (NodeSO is ChoiceNodeSO choiceSO)
            {
                if (choiceSO.choices == null)
                    choiceSO.choices = new System.Collections.Generic.List<ChoiceData>();

                var header = new Label("── Opciones ──");
                header.style.unityFontStyleAndWeight = FontStyle.Bold;
                header.style.marginTop = 6;
                _body.Add(header);

                var choicesContainer = new VisualElement();
                _body.Add(choicesContainer);

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
                _body.Add(addBtn);
            }

            extensionContainer.Add(_body);
            expanded = true;
        }

        private void BuildResizeHandle()
        {
            var handle = new VisualElement();
            handle.style.width = 14;
            handle.style.height = 14;
            handle.style.position = Position.Absolute;
            handle.style.right = 0;
            handle.style.bottom = 0;
            handle.style.cursor = new StyleCursor(new UnityEngine.UIElements.Cursor
            {
                texture = null,
                hotspot = Vector2.zero
            });
            handle.style.backgroundColor = new Color(1, 1, 1, 0.15f);

            var triangle = new Label("◢");
            triangle.style.fontSize = 10;
            triangle.style.color = new Color(1, 1, 1, 0.6f);
            triangle.style.marginLeft = 2;
            triangle.style.marginTop = 2;
            handle.Add(triangle);

            handle.RegisterCallback<MouseDownEvent>(e =>
            {
                if (e.button != 0) return;
                _resizing = true;
                _resizeStart = e.mousePosition;
                _sizeStart = new Vector2(resolvedStyle.width, resolvedStyle.height);
                handle.CaptureMouse();
                e.StopPropagation();
            });

            handle.RegisterCallback<MouseMoveEvent>(e =>
            {
                if (!_resizing) return;
                var delta = e.mousePosition - _resizeStart;
                float newW = Mathf.Max(200, _sizeStart.x + delta.x);
                float newH = Mathf.Max(80, _sizeStart.y + delta.y);
                style.width = newW;
                style.height = newH;
                e.StopPropagation();
            });

            handle.RegisterCallback<MouseUpEvent>(e =>
            {
                if (!_resizing) return;
                _resizing = false;
                handle.ReleaseMouse();
                e.StopPropagation();
            });

            Add(handle);
        }

        private void BuildGateEditors(VisualElement parent)
{
    if (NodeSO.prerequisiteFlags == null)
        NodeSO.prerequisiteFlags = new System.Collections.Generic.List<DialogueFlagRequirement>();

    if (NodeSO.flagsOnEnter == null)
        NodeSO.flagsOnEnter = new System.Collections.Generic.List<DialogueFlagChange>();

    var gateHeader = new Label("── Prerequisite Gates ──");
    gateHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
    gateHeader.style.marginTop = 6;
    parent.Add(gateHeader);

    var lockedReplyField = new TextField("Locked Reply")
    {
        value = NodeSO.lockedReply,
        multiline = true
    };
    lockedReplyField.RegisterValueChangedCallback(e =>
    {
        NodeSO.lockedReply = e.newValue;
        EditorUtility.SetDirty(NodeSO);
    });
    parent.Add(lockedReplyField);

    var requirementsContainer = new VisualElement();
    parent.Add(requirementsContainer);

    void RefreshRequirements()
    {
        requirementsContainer.Clear();

        for (int i = 0; i < NodeSO.prerequisiteFlags.Count; i++)
        {
            int idx = i;

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            var flagField = new TextField
            {
                value = NodeSO.prerequisiteFlags[idx].flag
            };
            flagField.style.flexGrow = 1;
            flagField.RegisterValueChangedCallback(e =>
            {
                NodeSO.prerequisiteFlags[idx].flag = e.newValue;
                EditorUtility.SetDirty(NodeSO);
            });

            var expectedToggle = new Toggle("true")
            {
                value = NodeSO.prerequisiteFlags[idx].expectedValue
            };
            expectedToggle.style.width = 70;
            expectedToggle.RegisterValueChangedCallback(e =>
            {
                NodeSO.prerequisiteFlags[idx].expectedValue = e.newValue;
                EditorUtility.SetDirty(NodeSO);
            });

            var removeBtn = new Button(() =>
            {
                NodeSO.prerequisiteFlags.RemoveAt(idx);
                EditorUtility.SetDirty(NodeSO);
                RefreshRequirements();
            })
            { text = "✕" };
            removeBtn.style.width = 24;

            row.Add(flagField);
            row.Add(expectedToggle);
            row.Add(removeBtn);

            requirementsContainer.Add(row);
        }
    }

    RefreshRequirements();

    var addRequirementBtn = new Button(() =>
    {
        NodeSO.prerequisiteFlags.Add(new DialogueFlagRequirement
        {
            flag = "new_flag",
            expectedValue = true
        });
        EditorUtility.SetDirty(NodeSO);
        RefreshRequirements();
    })
    { text = "+ Añadir requisito" };
    addRequirementBtn.style.marginTop = 4;
    parent.Add(addRequirementBtn);

    var enterHeader = new Label("── Flags al entrar ──");
    enterHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
    enterHeader.style.marginTop = 8;
    parent.Add(enterHeader);

    var flagsOnEnterContainer = new VisualElement();
    parent.Add(flagsOnEnterContainer);

    void RefreshFlagsOnEnter()
    {
        flagsOnEnterContainer.Clear();

        for (int i = 0; i < NodeSO.flagsOnEnter.Count; i++)
        {
            int idx = i;

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            var flagField = new TextField
            {
                value = NodeSO.flagsOnEnter[idx].flag
            };
            flagField.style.flexGrow = 1;
            flagField.RegisterValueChangedCallback(e =>
            {
                NodeSO.flagsOnEnter[idx].flag = e.newValue;
                EditorUtility.SetDirty(NodeSO);
            });

            var valueToggle = new Toggle("true")
            {
                value = NodeSO.flagsOnEnter[idx].value
            };
            valueToggle.style.width = 70;
            valueToggle.RegisterValueChangedCallback(e =>
            {
                NodeSO.flagsOnEnter[idx].value = e.newValue;
                EditorUtility.SetDirty(NodeSO);
            });

            var removeBtn = new Button(() =>
            {
                NodeSO.flagsOnEnter.RemoveAt(idx);
                EditorUtility.SetDirty(NodeSO);
                RefreshFlagsOnEnter();
            })
            { text = "✕" };
            removeBtn.style.width = 24;

            row.Add(flagField);
            row.Add(valueToggle);
            row.Add(removeBtn);

            flagsOnEnterContainer.Add(row);
        }
    }

    RefreshFlagsOnEnter();

    var addFlagBtn = new Button(() =>
    {
        NodeSO.flagsOnEnter.Add(new DialogueFlagChange
        {
            flag = "new_flag",
            value = true
        });
        EditorUtility.SetDirty(NodeSO);
        RefreshFlagsOnEnter();
    })
    { text = "+ Añadir flag al entrar" };
    addFlagBtn.style.marginTop = 4;
    parent.Add(addFlagBtn);
}
    }


}
#endif
