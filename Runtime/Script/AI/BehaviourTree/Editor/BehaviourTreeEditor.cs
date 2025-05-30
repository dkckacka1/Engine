using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Callbacks;
using System;

namespace Engine.AI.BehaviourTree
{
    public class BehaviourTreeEditor : EditorWindow
    {
        private BehaviourTreeView _treeView;
        private InspectorView _treeInspectorView;
        private InspectorView _nodeInspectorView;

        private BehaviourTree _currentTree;

        private static BehaviourTreeEditor Instance { get; set; }

        [MenuItem("BehaviourTreeEditor/Editor")]
        public static void OpenWindow()
        {
            BehaviourTreeEditor wnd = GetWindow<BehaviourTreeEditor>();
            wnd.titleContent = new GUIContent("BehaviourTreeEditor");
        }

        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            if (Selection.activeObject is BehaviourTree)
            {
                OpenWindow();
                return true;
            }

            return false;
        }

        public void CreateGUI()
        {
            // Each editor window contains a root VisualElement object
            var root = rootVisualElement;

            var behaviourTreeData = Resources.Load<BehaviourTreeData>("BehaviourTreeData");

            // Instantiate UXML
            var visualTree = behaviourTreeData.treeAsset;
            visualTree.CloneTree(root);

            var styleSheet = behaviourTreeData.stypeSheet;
            root.styleSheets.Add(styleSheet);

            _treeView = root.Q<BehaviourTreeView>();
            _treeInspectorView = root.Q<InspectorView>("tree_inspector_container");

            _nodeInspectorView = root.Q<InspectorView>("node_inspector_container");

            _treeView.OnNodeSelected = OnNodeSelectionChanged;

            OnSelectionChange();
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            Instance = this;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

            Instance = null;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange obj)
        {
            switch (obj)
            {
                case PlayModeStateChange.EnteredEditMode:
                    OnSelectionChange();
                    break;
                case PlayModeStateChange.ExitingEditMode:
                    break;
                case PlayModeStateChange.EnteredPlayMode:
                    OnSelectionChange();
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(obj), obj, null);
            }
        }

        private void OnSelectionChange()
        {
            var tree = Selection.activeObject as BehaviourTree;

            if (!tree)
            {
                if (Selection.activeGameObject)
                {
                    BehaviourTreeRunner runner = Selection.activeGameObject.GetComponent<BehaviourTreeRunner>();

                    if (runner)
                    {
                        tree = runner.tree;
                        _currentTree = tree;
                    }
                }

                _treeInspectorView?.UpdateTreeObject(tree);
            }

            if (Application.isPlaying)
            {
                if (tree && _treeView is not null)
                {
                    _treeView?.PopulateView(tree);

                    if (_currentTree != tree)
                    {
                        _treeInspectorView?.UpdateTreeObject(null);
                    }

                    _nodeInspectorView?.UpdateSelection(null);
                }
            }
            else
            {
                if (tree && AssetDatabase.CanOpenAssetInEditor(tree.GetInstanceID()))
                {
                    _treeView?.PopulateView(tree);
                }

                _treeInspectorView?.UpdateTreeObject(null);
                _nodeInspectorView?.UpdateSelection(null);
            }
        }

        private void OnNodeSelectionChanged(NodeView nodeView)
        {
            _nodeInspectorView.UpdateSelection(nodeView);
        }

        private void OnInspectorUpdate()
        {
            _treeView?.UpdateNodeState();
        }

        public static void UpdateTreeView()
        {
            var editor = GetWindow<BehaviourTreeEditor>();

            if (editor && editor._treeView != null)
            {
                editor._treeView.UpdateNodeName();
            }
        }

        public static void UpdateNodeView(Node node)
        {
            var treeView = Instance?._treeView;
            if (treeView?.GetNodeByGuid(node.guid) is NodeView nodeView)
            {
                nodeView.SetSubTitleName();
                nodeView.SetDescription();
            }
        }
    }
}
