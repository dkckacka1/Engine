using UnityEngine;

namespace Engine.Util
{
    public class SingletonMonoBehaviour<T> : MonoBehaviour where T : Component
    {
        public static T Instance
        {
            get
            {
                if (instance is null)
                {
                    var newObject = new GameObject();
                    newObject.name = typeof(T).Name;

                    var ins = newObject.AddComponent<T>();

                    instance = ins; 
                }

                return instance;
            }
        }

        public static bool HasInstance
        {
            get
            {
                return instance is not null;
            }
        }

        private static T instance;

        private void Awake()
        {
            if (instance is null)
            {
                instance = GetComponent<T>();
                DontDestroyOnLoad(this.gameObject);
            }
            else
            {
                Debug.Log($"Removed Object {typeof(T).Name}");
                Destroy(this.gameObject);
            }

            Initialized();
        }

        public virtual void Initialized() { }
    }
}
