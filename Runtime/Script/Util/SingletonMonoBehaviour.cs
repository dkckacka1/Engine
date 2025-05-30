using UnityEngine;

namespace Engine.Util
{
    public class SingletonMonoBehaviour<T> : MonoBehaviour where T : Component
    {
        public static T Instance
        {
            get
            {
                if (_instance is null)
                {
                    var newObject = new GameObject();
                    newObject.name = typeof(T).Name;

                    var ins = newObject.AddComponent<T>();

                    _instance = ins; 
                }

                return _instance;
            }
        }

        public static bool HasInstance => _instance is not null;

        private static T _instance;

        private void Awake()
        {
            if (_instance is null)
            {
                _instance = GetComponent<T>();
                DontDestroyOnLoad(this.gameObject);
            }
            else
            {
                Debug.Log($"Removed Object {typeof(T).Name}");
                Destroy(this.gameObject);
            }

            Initialized();
        }

        protected virtual void Initialized() { }
    }
}
