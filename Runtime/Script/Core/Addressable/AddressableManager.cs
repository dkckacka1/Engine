using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;

using Zenject;

using Engine.Util;

namespace Engine.Core.Addressable
{
    public enum CachingType
    {
        Permanent,  // 영구적 지속
        Scene,      // 해당씬에서만 지속
        Custom      // 커스텀
    }

    public class CacheData
    {
        public string CachingType;
        public UnityEngine.Object OriginObject;
    }

    public class AddressableManager : SingletonMonoBehaviour<AddressableManager>
    {
        private readonly Dictionary<string, CacheData> _loadedAddressableDic = new();
        private readonly HashSet<string> _loadingHashSet = new();

        private const string PERMANENT_TYPE_NAME = "Permanent";
        private const string SCENE_TYPE_NAME = "Scene";

        protected override void Initialized()
        {
            base.Initialized();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        public async Task<T> LoadAssetAsync<T>(string address, CachingType cachingType = CachingType.Scene, string customTypeName = "Custom") where T : UnityEngine.Object
        {
            if (_loadingHashSet.Contains(address))
                // 동시에 여러개의 로드 호출이 있을 경우 대기
            {
                await UniTask.WaitUntil(() => _loadingHashSet.Contains(address) is false);
            }

            if (_loadedAddressableDic.TryGetValue(address, out var cacheData))
            {
                return cacheData.OriginObject as T;
            }

            _loadingHashSet.Add(address);
            cacheData = new CacheData
            {
                CachingType = GetCachingTypeName(cachingType)
            };

            if (IsComponentType<T>())
                // Component 타입
            {
                var asyncOperationHandle = Addressables.LoadAssetAsync<GameObject>(address);
                await asyncOperationHandle.Task;
                switch (asyncOperationHandle.Status)
                {
                    case AsyncOperationStatus.None:
                        break;
                    case AsyncOperationStatus.Succeeded:
                        {
                            var result = asyncOperationHandle.Result;

                            _loadedAddressableDic.Add(address, cacheData);
                            _loadingHashSet.Remove(address);
                            return result.GetComponent<T>();
                        }
                    case AsyncOperationStatus.Failed:
                        {
                            Debug.LogError(asyncOperationHandle.OperationException.Message);
                        }
                        break;
                }
            }
            else
                // Asset 타입
            {
                var asyncOperationHandle = Addressables.LoadAssetAsync<T>(address);
                await asyncOperationHandle.Task;
                switch (asyncOperationHandle.Status)
                {
                    case AsyncOperationStatus.None:
                        break;
                    case AsyncOperationStatus.Succeeded:
                        {
                            var result = asyncOperationHandle.Result;

                            cacheData.OriginObject = result;
                            _loadedAddressableDic.Add(address, cacheData);
                            _loadingHashSet.Remove(address);
                            return result;
                        }
                    case AsyncOperationStatus.Failed:
                        {
                            Debug.LogError(asyncOperationHandle.OperationException.Message);
                        }
                        break;
                }
            }

            return null;
        }

        public async Task<T> InstantiateObject<T>(
            string address,
            Transform parent = null,
            CachingType cachingType = CachingType.Scene,
            string customTypeName = "Custom",
            Action<AsyncOperationHandle<GameObject>> handleComplete = null,
            DiContainer container = null
            ) where T : UnityEngine.Object
        {
            if (_loadedAddressableDic.ContainsKey(address) is false)
            {
                await LoadAssetAsync<T>(address, cachingType, customTypeName);
            }

            var data = _loadedAddressableDic[address];

            var asyncOperHandle = (!parent)
                ? Addressables.InstantiateAsync(address)
                : Addressables.InstantiateAsync(address, parent);
            asyncOperHandle.Completed += handleComplete;
            await asyncOperHandle.Task;

            switch (asyncOperHandle.Status)
            {
                case AsyncOperationStatus.None:
                    break;
                case AsyncOperationStatus.Succeeded:
                {
                    container?.Inject(asyncOperHandle.Result);

                    if (typeof(T) == typeof(GameObject))
                    {
                        return asyncOperHandle.Result as T;
                    }

                    return asyncOperHandle.Result.GetComponent<T>();
                }
                    break;
                case AsyncOperationStatus.Failed:
                {
                    Debug.LogError(asyncOperHandle.OperationException.Message);
                }
                    break;
            }

            return null;
        }

        public void ReleaseAll(CachingType cachingType, string customName = "Custom")
        {
            var cachingTypeName = GetCachingTypeName(cachingType, customName);

            foreach (var loadedObject in _loadedAddressableDic
                         .Where(loadedObject => loadedObject.Value.CachingType == cachingTypeName))
            {
                Release(loadedObject.Key);
            }
        }

        public void Release(string address)
        {
            if (_loadedAddressableDic.TryGetValue(address, out var data))
            {
                Addressables.Release(data.OriginObject);

                _loadedAddressableDic.Remove(address);
            }
        }
        
        private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            ReleaseAll(CachingType.Scene);
        }

        private static string GetCachingTypeName(CachingType cachingType, string customName = "Custom")
        {
            var result = cachingType switch
            {
                CachingType.Permanent => PERMANENT_TYPE_NAME,
                CachingType.Scene => SCENE_TYPE_NAME,
                CachingType.Custom => customName,
                _ => ""
            };

            return result;
        }

        private static bool IsComponentType<T>()
        {
            return typeof(Component).IsAssignableFrom(typeof(T));
        }
    }
}