using UnityEngine;

namespace Engine.AI.BehaviourTree
{
    public class BehaviourTreeRunner : MonoBehaviour
    {
        public BehaviourTree tree;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            tree = tree.Clone();
            tree.Bind();
        }

        // Update is called once per frame
        void Update()
        {
            tree?.Update();
        }
    }
}