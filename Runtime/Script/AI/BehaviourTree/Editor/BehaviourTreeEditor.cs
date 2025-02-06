using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Callbacks;
using System;

namespace Engine.AI.BehaviourTree
{
    public class BehaviourTreeEditor : EditorWindow
    {
        BehaviourTreeView treeView;
        InspectorView treeInspectorView;
        InspectorView nodeInspectorView;

        BehaviourTree currentTree;

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
            VisualElement root = rootVisualElement;

            var behaviourTreeData = Resources.Load<BehaviourTreeData>("BehaviourTreeData");

            // Instantiate UXML
            var visualTree = behaviourTreeData.treeAsset;
            visualTree.CloneTree(root);

            var styleSheet = behaviourTreeData.stypeSheet;
            root.styleSheets.Add(styleSheet);

            treeView = root.Q<BehaviourTreeView>();
            treeInspectorView = root.Q<InspectorView>("tree_inspector_container");

            nodeInspectorView = root.Q<InspectorView>("node_inspector_container");

            treeView.OnNodeSelected = OnNodeSelectionChanged;

            OnSelectionChange();
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
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
            }
        }

        private void OnSelectionChange()
        {
            BehaviourTree tree = Selection.activeObject as BehaviourTree;

            if (!tree)
            {
                if (Selection.activeGameObject)
                {
                    BehaviourTreeRunner runner = Selection.activeGameObject.GetComponent<BehaviourTreeRunner>();

                    if (runner)
                    {
                        tree = runner.tree;
                        currentTree = tree;
                    }
                }

                treeInspectorView?.UpdateTreeObject(tree);
            }

            if (Application.isPlaying)
            {
                if (tree && treeView is not null)
                {
                    treeView?.PopulateView(tree);

                    if (currentTree != tree)
                    {
                        treeInspectorView?.UpdateTreeObject(null);
                    }

                    nodeInspectorView?.UpdateSelection(null);
                }
            }
            else
            {
                if (tree && AssetDatabase.CanOpenAssetInEditor(tree.GetInstanceID()))
                {
                    treeView?.PopulateView(tree);
                }

                treeInspectorView?.UpdateTreeObject(null);
                nodeInspectorView?.UpdateSelection(null);
            }
        }

        void OnNodeSelectionChanged(NodeView nodeView)
        {
            nodeInspectorView.UpdateSelection(nodeView);
        }

        private void OnInspectorUpdate()
        {
            if (treeView is null) return;

            treeView.UpdateNodeState();
        }

        public static void UpdateTreeView()
        {
            BehaviourTreeEditor editor = GetWindow<BehaviourTreeEditor>();

            if (editor && editor.treeView != null)
            {
                editor.treeView.UpdateNodeName();
            }
        }
    }
}