using System;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using System.Collections.Generic;

namespace Engine.AI.BehaviourTree
{
    public class NodeView : UnityEditor.Experimental.GraphView.Node
    {
        public Action<NodeView> OnNodeSelected;
        public readonly Node Node;
        public Port Input;
        public Port Output;

        private readonly Label _subtitleLable;
        private readonly Label _descriptionLabel;

        public NodeView(Node node) : base(UnityEditor.AssetDatabase.GetAssetPath(Resources.Load<BehaviourTreeData>("BehaviourTreeData").nodeView))
        {
            Node = node;
            title = node.name;
            viewDataKey = node.guid;

            style.left = node.position.x;
            style.top = node.position.y;

            CreateInputPorts();
            CreateOutPorts();
            SetupClasses();

            _descriptionLabel = this.Q<Label>("description");
            _descriptionLabel.bindingPath = "description";
            _descriptionLabel.Bind(new SerializedObject(node));

            _subtitleLable = this.Q<Label>("subtitle");
            SetSubTitleName();
            SetDescription();
        }

        public sealed override string title
        {
            get => base.title;
            set => base.title = value;
        }

        private void SetupClasses()
        {
            switch (Node)
            {
                case ActionNode:
                    AddToClassList("action");
                    break;
                case CompositeNode:
                    AddToClassList("composite");
                    break;
                case DecoratorNode:
                    AddToClassList("decorator");
                    break;
                case RootNode:
                    AddToClassList("root");
                    break;
            }
        }

        private void CreateInputPorts()
        {
            switch (Node)
            {
                case ActionNode:
                case CompositeNode:
                case DecoratorNode:
                    Input = InstantiatePort(Orientation.Vertical, Direction.Input, Port.Capacity.Single, typeof(bool));
                    break;
                case RootNode:
                    break;
            }

            if (Input != null)
            {
                Input.portName = "";
                Input.style.flexDirection = FlexDirection.Column;
                inputContainer.Add(Input);
            }
        }


        private void CreateOutPorts()
        {
            switch (Node)
            {
                case ActionNode:
                    break;
                case CompositeNode:
                    Output = InstantiatePort(Orientation.Vertical, Direction.Output, Port.Capacity.Multi, typeof(bool));
                    break;
                case DecoratorNode:
                case RootNode:
                    Output = InstantiatePort(Orientation.Vertical, Direction.Output, Port.Capacity.Single, typeof(bool));
                    break;
            }

            if (Output != null)
            {
                Output.portName = "";
                Output.style.flexDirection = FlexDirection.ColumnReverse;
                outputContainer.Add(Output);
            }
        }

        public override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);
            Undo.RecordObject(Node, "Behaviour Tree (Set Position)");
            Node.position.x = newPos.x;
            Node.position.y = newPos.y;
            EditorUtility.SetDirty(Node);
        }

        public override void OnSelected()
        {
            base.OnSelected();

            OnNodeSelected?.Invoke(this);
        }

        public void SortChildren()
        {
            var composite = Node as CompositeNode;

            if (composite)
            {
                composite.children.Sort(SortByHorizontalPosition);
            }
        }

        private int SortByHorizontalPosition(Node left, Node right)
        {
            return left.position.x < right.position.x ? -1 : 1;
        }

        public void Update()
        {
            RemoveFromClassList("running");
            RemoveFromClassList("failure");
            RemoveFromClassList("success");

            if (Application.isPlaying)
            {
                switch (Node.state)
                {
                    case Node.State.Running:
                        if (Node.started)
                        {
                            AddToClassList("running");
                        }
                        break;
                    case Node.State.Failure:
                        AddToClassList("failure");
                        break;
                    case Node.State.Success:
                        AddToClassList("success");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public void ClearNode()
        {
            RemoveFromClassList("running");
            RemoveFromClassList("failure");
            RemoveFromClassList("success");
        }

        private static bool TryGetInputNodeView(NodeView nodeView, out NodeView inputNodeView)
        {
            inputNodeView = null;

            if (nodeView.Input is not null && nodeView.Input.connected)
            {
                Edge edge = nodeView.Input.connections.SingleOrDefault();
                inputNodeView = edge.output.node as NodeView;
            }

            return inputNodeView is not null ? true : false;
        }

        private static bool TryGetOutputNodeViews(NodeView nodeView, out IEnumerable<NodeView> outputNodeViews)
        {
            outputNodeViews = null;

            if (nodeView.Output is not null && nodeView.Output.connected)
            {
                outputNodeViews = nodeView.Output.connections.Select(edge => edge.input.node as NodeView);
            }

            return outputNodeViews is not null ? true : false;
        }

        public override string ToString()
        {
            var result = $"currentNode : {Node.name}\n";

            if (Input is null)
            {
                result += $"input : null\n";
            }
            else if (Input.connected is false)
            {
                result += $"input node : null\n";
            }
            else if (TryGetInputNodeView(this, out var inputNodeView))
            {
                result += $"inputNode : {inputNodeView.Node.name}\n";
            }

            if (Output is null)
            {
                result += $"output : null\n";
            }
            else if (Output.connected is false)
            {
                result += $"output node : null\n";
            }
            else if (TryGetOutputNodeViews(this, out var outputNodeViews))
            {
                result += $"outputNodes\n";
                result = outputNodeViews.Aggregate(result, (current, nodeView) => current + $"{nodeView.Node.name}\n");
            }

            return result;
        }

        public void SetSubTitleName()
        {
            var subTitleName = Node.GetSubTitleName;

            if (string.IsNullOrEmpty(subTitleName))
            {
                _subtitleLable.parent.style.display = DisplayStyle.None; 
            }
            else
            {
                _subtitleLable.parent.style.display = DisplayStyle.Flex;
                _subtitleLable.text = subTitleName;
            }
        }

        public void SetDescription()
        {
            _descriptionLabel.text = Node.GetDescription;
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);

            evt.menu.AppendAction("check", _ =>
            {
                Debug.Log(_subtitleLable.parent.name);
            });

            evt.menu.AppendAction("enable", _ =>
            {
                _subtitleLable.parent.style.display = DisplayStyle.Flex;
            });

            evt.menu.AppendAction("Disable", _ =>
            {
                _subtitleLable.parent.style.display = DisplayStyle.None;
            });
        }
    }
}