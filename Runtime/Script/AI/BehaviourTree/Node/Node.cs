using UnityEngine;

namespace Engine.AI.BehaviourTree
{
    public abstract class Node : ScriptableObject
    {
        public enum State
        {
            Running,
            Failure,
            Success,
        }

        [HideInInspector] public State state = State.Running;
        [HideInInspector] public bool started = false;
        [HideInInspector] public string guid;
        [HideInInspector] public Vector2 position;
        [TextArea] public string description;
        [HideInInspector] public Blackboard blackboard;

        public virtual string GetTitleName => this.GetType().Name;
        public virtual string GetSubTitleName => string.Empty;
        public virtual string GetDescription => string.Empty;

        private void OnValidate()
        {
            BehaviourTreeEditor.UpdateNodeView(this);
        }

        public State Update()
        {
            if (!started)
            {
                OnStart();
                started = true;
            }

            state = OnUpdate();

            if (state == State.Failure || state == State.Success)
            {
                OnStop();
                started = false;
            }

            return state;
        }

        public virtual Node Clone()
        {
            return Instantiate(this);
        }

        protected abstract void OnStart();
        protected abstract void OnStop();
        protected abstract State OnUpdate();

        protected bool TryGetBlackboard<T>(out T blackborad) where T : Blackboard
        {
            blackborad = blackboard as T;

            return blackborad is not null ? true : false;
        }
    }
}