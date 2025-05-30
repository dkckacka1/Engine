using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

using Engine.Util;
using Object = UnityEngine.Object;

namespace Engine.Core
{
    public struct ObjectPoolEvent<T> where T : Object
    {
        public readonly System.Func<T> CreateFunc;
        public readonly System.Action<T> ActionOnGet;
        public readonly System.Action<T> ActionOnRelease;
        public readonly System.Action<T> ActionOnDestroy;

        public ObjectPoolEvent(System.Func<T> createFunc, Action<T> actionOnGet, Action<T> actionOnRelease, Action<T> actionOnDestroy) : this()
        {
            this.CreateFunc = createFunc;
            ActionOnGet = actionOnGet;
            ActionOnRelease = actionOnRelease;
            ActionOnDestroy = actionOnDestroy;
        }
    }

    public class ObjectPoolController
    {
        private readonly Dictionary<string, IObjectPool<Object>> _poolDic;

        public ObjectPoolController()
        {
            _poolDic = new Dictionary<string, IObjectPool<Object>>();
        }

        public void CreatePool<T>(string key, ObjectPoolEvent<T> poolEvent, int defaultCap = 10, int maxSize = 100) where T : Object
        {
            var pool = new ObjectPool<T>(poolEvent.CreateFunc, poolEvent.ActionOnGet, poolEvent.ActionOnRelease, poolEvent.ActionOnDestroy, true, defaultCap, maxSize); ;

            _poolDic.TryAdd(key, (IObjectPool<Object>)pool);
        }

        public T Get<T>(string key) where T : Object
        {
            T result = null;

            if (_poolDic.TryGetValue(key, out var pool))
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

            if (_poolDic.TryGetValue(key, out IObjectPool<Object> pool))
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
            if (_poolDic.ContainsKey(key))
            {
                _poolDic[key].Clear();
                _poolDic.Remove(key);
            }
        }
    }
}