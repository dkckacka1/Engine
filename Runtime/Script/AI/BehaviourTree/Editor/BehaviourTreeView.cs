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
        BehaviourTree tree;

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
            PopulateView(tree);
            AssetDatabase.SaveAssets();
        }

        NodeView FindNodeView(Node node)
        {
            return GetNodeByGuid(node.guid) as NodeView;
        }

        internal void PopulateView(BehaviourTree tree)
        {
            this.tree = tree;

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

                    Edge edge = parentView.output.ConnectTo(childView.input);
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
                    NodeView nodeView = elem as NodeView;
                    if (nodeView != null)
                    {
                        tree.Deletenode(nodeView.node);
                    }

                    Edge edge = elem as Edge;
                    if (edge != null)
                    {
                        NodeView parentView = edge.output.node as NodeView;
                        NodeView childView = edge.input.node as NodeView;
                        tree.RemoveChild(parentView.node, childView.node);

                        var compositeNode = parentView.node as CompositeNode;
                        if (compositeNode)
                        {
                            for (int i = 0; i < compositeNode.children.Count; i++)
                            {
                                Node child = compositeNode.children[i];
                                SetNodeName(child, $"[{i + 1}] {child.GetType().Name}");
                            }
                        }

                        SetNodeName(childView.node, $"{childView.node.GetType().Name}");
                    }
                });
            }

            if (graphViewChange.edgesToCreate != null)
            {
                graphViewChange.edgesToCreate.ForEach(edge =>
                {
                    NodeView parentView = edge.output.node as NodeView;
                    NodeView childView = edge.input.node as NodeView;

                    tree.AddChild(parentView.node, childView.node);

                    var compositeNode = parentView.node as CompositeNode;
                    if (compositeNode)
                    {
                        for (int i = 0; i < compositeNode.children.Count; i++)
                        {
                            Node child = compositeNode.children[i];
                            SetNodeName(child, $"[{i + 1}] {child.GetType().Name}");
                        }
                    }
                });
            }

            if (graphViewChange.movedElements != null)
            {
                nodes.ForEach((n) =>
                {
                    NodeView view = n as NodeView;
                    view.SortChildren();
                });
            }

            SetNodesName();

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

        void CreateNode(Type type)
        {
            Node node = tree.CreateNode(type);
            CreateNodeView(node);
        }

        void CreateNode(Type type, Vector2 position)
        {
            Node node = tree.CreateNode(type);
            var nodeView = CreateNodeView(node);

            var targetRect = nodeView.GetPosition();
            targetRect.x = position.x;
            targetRect.y = position.y;

            nodeView.SetPosition(targetRect);
            nodeView.node.position.x = position.x;
            nodeView.node.position.y = position.y;
        }

        NodeView CreateNodeView(Node node)
        {
            NodeView nodeView = new NodeView(node);
            nodeView.OnNodeSelected = OnNodeSelected;
            AddElement(nodeView);

            return nodeView;
        }

        public void UpdateNodeState()
        {
            if (tree is null) return;

            if (tree.rootNode.state == Node.State.Failure)
            {
                nodes.ForEach(n =>
                {
                    NodeView view = n as NodeView;
                    view.ClearNode();
                });

                return;
            }
            else
            {
                nodes.ForEach(n =>
                {
                    NodeView view = n as NodeView;
                    view.Update();
                });
            }
        }

        private void SetNodesName()
        {
            nodes.ForEach(node =>
            {
                NodeView nodeView = node as NodeView;

                if (nodeView.node is CompositeNode)
                {
                    var compositeNode = nodeView.node as CompositeNode;

                    for (int i = 0; i < compositeNode.children.Count; i++)
                    {
                        Node child = compositeNode.children[i];

                        SetNodeName(child, $"[{i + 1}] {child.GetType().Name}");
                    }
                }
            });
        }

        private void SetNodeName(Node node, string name)
        {
            var nodeView = GetNodeByGuid(node.guid);
            nodeView.title = name;
            node.name = name;
        }
    }
}