using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Engine.AI.BehaviourTree
{
    [UxmlElement("BehaviourTreeView")]
    public partial class BehaviourTreeView : GraphView
    {
        public Action<NodeView> OnNodeSelected;
        private BehaviourTree _tree;

        public BehaviourTreeView()
        {
            Insert(0, new GridBackground());

            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var styleSheet = Resources.Load<BehaviourTreeData>("BehaviourTreeData").stypeSheet;
            styleSheets.Add(styleSheet);

            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnUndoRedo()
        {
            PopulateView(_tree);
            AssetDatabase.SaveAssets();
        }

        private NodeView FindNodeView(Node node)
        {
            return GetNodeByGuid(node.guid) as NodeView;
        }

        internal void PopulateView(BehaviourTree tree)
        {
            this._tree = tree;

            graphViewChanged -= OnGraphViewChanged;
            DeleteElements(graphElements);
            graphViewChanged += OnGraphViewChanged;

            if (tree.rootNode == null)
            {
                tree.rootNode = tree.CreateNode(typeof(RootNode)) as RootNode;
                EditorUtility.SetDirty(tree);
                AssetDatabase.SaveAssets();
            }

            tree.nodes.ForEach(n => CreateNodeView(n));

            tree.nodes.ForEach(n =>
            {
                var children = tree.GetChildren(n);
                children.ForEach(c =>
                {
                    NodeView parentView = FindNodeView(n);
                    NodeView childView = FindNodeView(c);

                    Edge edge = parentView.Output.ConnectTo(childView.Input);
                    AddElement(edge);
                });
            });
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports.ToList().Where(endPort => endPort.direction != startPort.direction && endPort.node != startPort.node).ToList();
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
        {
            if (graphViewChange.elementsToRemove != null)
            {
                graphViewChange.elementsToRemove.ForEach(elem =>
                {
                    if (elem is NodeView nodeView)
                    {
                        _tree.DeleteNode(nodeView.Node);
                    }

                    if (elem is Edge edge)
                    {
                        NodeView parentView = edge.output.node as NodeView;
                        NodeView childView = edge.input.node as NodeView;
                        _tree.RemoveChild(parentView.Node, childView.Node);

                        var compositeNode = parentView.Node as CompositeNode;
                        if (compositeNode)
                        {
                            for (int i = 0; i < compositeNode.children.Count; i++)
                            {
                                Node child = compositeNode.children[i];
                                SetNodeName(child, $"[{i + 1}] {child.GetTitleName}");
                            }
                        }

                        SetNodeName(childView.Node, $"{childView.Node.GetTitleName}");
                    }
                });
            }

            if (graphViewChange.edgesToCreate != null)
            {
                graphViewChange.edgesToCreate.ForEach(edge =>
                {
                    NodeView parentView = edge.output.node as NodeView;
                    NodeView childView = edge.input.node as NodeView;

                    _tree.AddChild(parentView.Node, childView.Node);

                    var compositeNode = parentView.Node as CompositeNode;
                    if (compositeNode)
                    {
                        for (int i = 0; i < compositeNode.children.Count; i++)
                        {
                            var child = compositeNode.children[i];
                            SetNodeName(child, $"[{i + 1}] {child.GetTitleName}");
                        }
                    }
                });
            }

            if (graphViewChange.movedElements != null)
            {
                nodes.ForEach((n) =>
                {
                    var view = n as NodeView;
                    view?.SortChildren();
                });
            }

            UpdateNodeName();

            return graphViewChange;
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            var createPostition = contentViewContainer.WorldToLocal(evt.mousePosition);

            string contextPath = "";
            {
                var types = TypeCache.GetTypesDerivedFrom<ActionNode>();
                contextPath = nameof(ActionNode);

                foreach (var type in types)
                {
                    if (type.IsAbstract) continue;

                    evt.menu.AppendAction($"{contextPath}/{type.Name}", (a) => CreateNode(type, createPostition));
                }
            }

            {
                var types = TypeCache.GetTypesDerivedFrom<CompositeNode>();
                contextPath = nameof(CompositeNode);

                foreach (var type in types)
                {
                    if (type.IsAbstract) continue;

                    evt.menu.AppendAction($"{contextPath}/{type.Name}", (a) => CreateNode(type, createPostition));
                }
            }

            {
                var types = TypeCache.GetTypesDerivedFrom<DecoratorNode>();
                contextPath = nameof(DecoratorNode);

                foreach (var type in types)
                {
                    if (type.IsAbstract) continue;

                    evt.menu.AppendAction($"{contextPath}/{type.Name}", (a) => CreateNode(type, createPostition));
                }
            }
        }

        private void CreateNode(Type type)
        {
            var node = _tree.CreateNode(type);
            CreateNodeView(node);
        }

        private void CreateNode(Type type, Vector2 position)
        {
            var node = _tree.CreateNode(type);
            var nodeView = CreateNodeView(node);

            var targetRect = nodeView.GetPosition();
            targetRect.x = position.x;
            targetRect.y = position.y;

            nodeView.SetPosition(targetRect);
            nodeView.Node.position.x = position.x;
            nodeView.Node.position.y = position.y;
        }

        private NodeView CreateNodeView(Node node)
        {
            var nodeView = new NodeView(node);
            nodeView.OnNodeSelected = OnNodeSelected;
            AddElement(nodeView);

            return nodeView;
        }

        public void UpdateNodeState()
        {
            if (_tree is null) return;

            if (_tree.rootNode.state == Node.State.Failure)
            {
                nodes.ForEach(n =>
                {
                    NodeView view = n as NodeView;
                    view?.ClearNode();
                });

                return;
            }
            else
            {
                nodes.ForEach(n =>
                {
                    var view = n as NodeView;
                    view?.Update();
                });
            }
        }

        public void UpdateNodeName()
        {
            nodes.ForEach(node =>
            {
                var nodeView = node as NodeView;

                if (nodeView != null && nodeView.Node is CompositeNode)
                {
                    var compositeNode = nodeView.Node as CompositeNode;

                    if (compositeNode)
                        for (int i = 0; i < compositeNode.children.Count; i++)
                        {
                            var child = compositeNode.children[i];

                            SetNodeName(child, $"[{i + 1}] {child.GetTitleName}");
                        }
                }
            });
        }

        private void SetNodeName(Node node, string nodeName)
        {
            var nodeView = GetNodeByGuid(node.guid);
            nodeView.title = nodeName;
            node.name = nodeName;
        }
    }
}