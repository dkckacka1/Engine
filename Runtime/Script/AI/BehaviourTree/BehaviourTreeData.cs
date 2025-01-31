using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Engine.AI.BehaviourTree
{
    //[CreateAssetMenu(fileName = "BehaviourTreeData", menuName = "Scriptable Objects/BehaviourTreeData")]
    public class BehaviourTreeData : ScriptableObject
    {
        public VisualTreeAsset treeAsset;
        public StyleSheet stypeSheet;

        public VisualTreeAsset nodeView;
    }
}