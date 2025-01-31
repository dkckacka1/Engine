using System;
using UnityEngine;

namespace Engine.AI.BehaviourTree
{
    public abstract class BehaviourTreeRunner : MonoBehaviour
    {
        public BehaviourTree tree;

        protected virtual void Initialized() 
        {
            tree = tree.Clone();
            tree.Bind();
        }

        public void Start()
        {
            Initialized();
        }

        // Update is called once per frame
        public void Update()
        {
            tree?.Update();
        }
    }
}