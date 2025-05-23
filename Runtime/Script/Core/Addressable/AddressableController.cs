using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Engine.Util;
using Cysharp.Threading.Tasks;

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
        public string cachingType;
        public UnityEngine.Object originObject;
    }

    public class AddressableManager : SingletonMonoBehaviour<AddressableManager>
    {
        private Dictionary<string, CacheData> loadedAddressableDic = new();
        private HashSet<string> loadingHashSet = new();

        private const string PermanentTypeName = "Permanent";
        private const string SceneTypeName = "Scene";

        private bool isLoading = false;

        public async Task<T> LoadAssetAsync<T>(string address, CachingType cachingType = CachingType.Scene, string customTypeName = "Custom") where T : UnityEngine.Object
        {
            if (loadingHashSet.Contains(address))
                // 동시에 여러개의 로드 호출이 있을 경우 대기
            {
                await UniTask.WaitUntil(() => loadingHashSet.Contains(address) is false);
            }

            if (loadedAddressableDic.TryGetValue(address, out var cacheData))
            {
                return cacheData.originObject as T;
            }

            loadingHashSet.Add(address);
            cacheData = new CacheData();
            cacheData.cachingType = GetCachingTypeName(cachingType);

            if (IsComponentType<T>())
                // Component 타입
            {
                AsyncOperationHandle<GameObject> asyncOperationHandle = Addressables.LoadAssetAsync<GameObject>(address);
                await asyncOperationHandle.Task;
                GameObject result = null;
                switch (asyncOperationHandle.Status)
                {
                    case AsyncOperationStatus.None:
                        break;
                    case AsyncOperationStatus.Succeeded:
                        {
                            result = asyncOperationHandle.Result;

                            loadedAddressableDic.Add(address, cacheData);
                            loadingHashSet.Remove(address);
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
                AsyncOperationHandle<T> asyncOperationHandle = Addressables.LoadAssetAsync<T>(address);
                await asyncOperationHandle.Task;
                T result = null;
                switch (asyncOperationHandle.Status)
                {
                    case AsyncOperationStatus.None:
                        break;
                    case AsyncOperationStatus.Succeeded:
                        {
                            result = asyncOperationHandle.Result;

                            cacheData.originObject = result;
                            loadedAddressableDic.Add(address, cacheData);
                            loadingHashSet.Remove(address);
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
            Action<AsyncOperationHandle<GameObject>> handle_Complete = null
            ) where T : UnityEngine.Object
        {
            if (loadedAddressableDic.ContainsKey(address) is false)
            {
                await LoadAssetAsync<T>(address, cachingType, customTypeName);
            }

            CacheData data = loadedAddressableDic[address];

            AsyncOperationHandle<GameObject> asyncOperHandle = (parent == null) ? Addressables.InstantiateAsync(address) : Addressables.InstantiateAsync(address, parent);
            asyncOperHandle.Completed += handle_Complete;
            await asyncOperHandle.Task;

            switch (asyncOperHandle.Status)
            {
                case AsyncOperationStatus.None:
                    break;
                case AsyncOperationStatus.Succeeded:
                    {
                        if (typeof(T) == typeof(GameObject))
                        {
                            return asyncOperHandle.Result as T;
                        }

                        return asyncOperHandle.Result.GetComponent<T>();
                    }
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
            string cachingTypeName = GetCachingTypeName(cachingType, customName);

            foreach (var loadedObject in loadedAddressableDic)
            {
                if (loadedObject.Value.cachingType == cachingTypeName)
                {
                    Release(loadedObject.Key);
                }
            }
        }

        public void Release(string address)
        {
            if (loadedAddressableDic.TryGetValue(address, out var data))
            {
                Addressables.Release(data.originObject);

                loadedAddressableDic.Remove(address);
            }
        }

        private string GetCachingTypeName(CachingType cachingType, string customName = "Custom")
        {
            string result = "";

            switch (cachingType)
            {
                case CachingType.Permanent:
                    result = PermanentTypeName;
                    break;
                case CachingType.Scene:
                    result = SceneTypeName;
                    break;
                case CachingType.Custom:
                    result = customName;
                    break;
            }

            return result;
        }

        private static bool IsComponentType<T>()
        {
            return typeof(Component).IsAssignableFrom(typeof(T));
        }
    }
}