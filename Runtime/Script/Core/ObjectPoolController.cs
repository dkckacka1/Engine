using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

using Engine.Util;

namespace Engine.Core
{
    public struct ObjectPoolEvent<T> where T : Object
    {
        public System.Func<T> createFunc;
        public System.Action<T> actionOnGet;
        public System.Action<T> actionOnRelease;
        public System.Action<T> actionOnDestroy;

        public ObjectPoolEvent(System.Func<T> createFunc) : this()
        {
            this.createFunc = createFunc;
        }
    }

    public class ObjectPoolController
    {
        private Dictionary<string, IObjectPool<Object>> poolDic;

        public ObjectPoolController()
        {
            poolDic = new Dictionary<string, IObjectPool<Object>>();
        }

        public void CreatePool<T>(string key, ObjectPoolEvent<T> poolEvent, int defaultCap = 10, int maxSize = 100) where T : Object
        {
            var pool = new ObjectPool<T>(poolEvent.createFunc, poolEvent.actionOnGet, poolEvent.actionOnRelease, poolEvent.actionOnDestroy, true, defaultCap, maxSize); ;

            poolDic.TryAdd(key, (IObjectPool<Object>)pool);
        }

        public T Get<T>(string key) where T : Object
        {
            T result = null;

            if (poolDic.TryGetValue(key, out var pool))
            {
                result = pool.Get() as T;

                if (result is null)
                {
                    Debug.LogError($"{key} Pool is not {nameof(T)}");
                }
            }
            else
            {
                Debug.LogError($"{nameof(T)} Pool was not created");
            }

            return result;
        }

        public void Release<T>(string key, T element) where T : Object
        {
            if (element is null) return;

            if (poolDic.TryGetValue(key, out IObjectPool<Object> pool))
            {
                if (pool is IObjectPool<T>)
                {
                    pool.Release(element);
                }
                else
                {
                    Debug.LogError($"{key} Pool is not {nameof(T)}");
                }
            }
            else
            {
                Debug.LogError($"{nameof(T)} Pool was not created");
            }
        }

        public void Clear(string key)
        {
            if (poolDic.ContainsKey(key))
            {
                poolDic[key].Clear();
                poolDic.Remove(key);
            }
        }
    }
}