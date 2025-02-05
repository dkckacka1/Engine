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
        public Node node;
        public Port input;
        public Port output;

        public NodeView(Node node) : base(UnityEditor.AssetDatabase.GetAssetPath(Resources.Load<BehaviourTreeData>("BehaviourTreeData").nodeView))
        {
            this.node = node;
            title = node.name;
            viewDataKey = node.guid;

            style.left = node.position.x;
            style.top = node.position.y;

            CreateInputPorts();
            CreateOutPorts();
            SetupClasses();

            Label descriptionLabel = this.Q<Label>("description");
            descriptionLabel.bindingPath = "description";
            descriptionLabel.Bind(new SerializedObject(node));
        }

        private void SetupClasses()
        {
            if (node is ActionNode)
            {
                AddToClassList("action");
            }
            else if (node is CompositeNode)
            {
                AddToClassList("composite");
            }
            else if (node is DecoratorNode)
            {
                AddToClassList("decorator");
            }
            else if (node is RootNode)
            {
                AddToClassList("root");
            }
        }

        private void CreateInputPorts()
        {
            if (node is ActionNode)
            {
                input = InstantiatePort(Orientation.Vertical, Direction.Input, Port.Capacity.Single, typeof(bool));
            }
            else if (node is CompositeNode)
            {
                input = InstantiatePort(Orientation.Vertical, Direction.Input, Port.Capacity.Single, typeof(bool));
            }
            else if (node is DecoratorNode)
            {
                input = InstantiatePort(Orientation.Vertical, Direction.Input, Port.Capacity.Single, typeof(bool));
            }
            else if (node is RootNode)
            {
            }

            if (input != null)
            {
                input.portName = "";
                input.style.flexDirection = FlexDirection.Column;
                inputContainer.Add(input);
            }
        }


        private void CreateOutPorts()
        {
            if (node is ActionNode)
            {
            }
            else if (node is CompositeNode)
            {
                output = InstantiatePort(Orientation.Vertical, Direction.Output, Port.Capacity.Multi, typeof(bool));
            }
            else if (node is DecoratorNode)
            {
                output = InstantiatePort(Orientation.Vertical, Direction.Output, Port.Capacity.Single, typeof(bool));
            }
            else if (node is RootNode)
            {
                output = InstantiatePort(Orientation.Vertical, Direction.Output, Port.Capacity.Single, typeof(bool));
            }

            if (output != null)
            {
                output.portName = "";
                output.style.flexDirection = FlexDirection.ColumnReverse;
                outputContainer.Add(output);
            }
        }

        public override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);
            Undo.RecordObject(node, "Behaviour Tree (Set Position)");
            node.position.x = newPos.x;
            node.position.y = newPos.y;
            EditorUtility.SetDirty(node);
        }

        public override void OnSelected()
        {
            base.OnSelected();

            if (OnNodeSelected != null)
            {
                OnNodeSelected.Invoke(this);
            }
        }

        public void SortChildren()
        {
            CompositeNode composite = node as CompositeNode;

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
                switch (node.state)
                {
                    case Node.State.Running:
                        if (node.started)
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
                }
            }
        }

        public void ClearNode()
        {
            RemoveFromClassList("running");
            RemoveFromClassList("failure");
            RemoveFromClassList("success");
        }

        public static bool TryGetInputNodeView(NodeView nodeView, out NodeView inputNodeView)
        {
            inputNodeView = null;

            if (nodeView.input is not null && nodeView.input.connected)
            {
                Edge edge = nodeView.input.connections.SingleOrDefault();
                inputNodeView = edge.output.node as NodeView;
            }

            return inputNodeView is not null ? true : false;
        }

        public static bool TryGetOutputNodeViews(NodeView nodeView, out IEnumerable<NodeView> outputNodeViews)
        {
            outputNodeViews = null;

            if (nodeView.output is not null && nodeView.output.connected)
            {
                outputNodeViews = nodeView.output.connections.Select(edge => edge.input.node as NodeView);
            }

            return outputNodeViews is not null ? true : false;
        }

        public override string ToString()
        {
            string result = $"currentNode : {node.name}\n";

            if (input is null)
            {
                result += $"input : null\n";
            }
            else if (input.connected is false)
            {
                result += $"input node : null\n";
            }
            else if (TryGetInputNodeView(this, out var inputNodeView))
            {
                result += $"inputNode : {inputNodeView.node.name}\n";
            }

            if (output is null)
            {
                result += $"output : null\n";
            }
            else if (output.connected is false)
            {
                result += $"output node : null\n";
            }
            else if (TryGetOutputNodeViews(this, out var outputNodeViews))
            {
                result += $"outputNodes\n";
                foreach (var nodeView in outputNodeViews)
                {
                    result += $"{nodeView.node.name}\n";
                }
            }

            return result;
        }

    }
}