using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

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
        public List<GameObject> instanceObjectList;
    }

    public class AddressableController
    {
        private Dictionary<string, CacheData> loadedAddressableDic = new();

        private const string PermanentTypeName = "Permanent";
        private const string SceneTypeName = "Scene";


        public async Task<T> LoadAssetAsync<T>(string address, CachingType cachingType = CachingType.Scene, string customTypeName = "Custom", Action<AsyncOperationHandle<T>> handle_Complete = null) where T : UnityEngine.Object
        {
            if (loadedAddressableDic.TryGetValue(address, out var cacheData))
            {
                return cacheData.originObject as T;
            }

            AsyncOperationHandle<T> asyncOperHandle = Addressables.LoadAssetAsync<T>(address);
            asyncOperHandle.Completed += handle_Complete;
            await asyncOperHandle.Task;
            T result = null;

            switch (asyncOperHandle.Status)
            {
                case AsyncOperationStatus.None:
                    break;
                case AsyncOperationStatus.Succeeded:
                    {
                        result = asyncOperHandle.Result;

                        cacheData = new CacheData();
                        cacheData.cachingType = GetCachingTypeName(cachingType);
                        cacheData.originObject = result;
                        cacheData.instanceObjectList = new();
                        loadedAddressableDic.TryAdd(address, cacheData);
                    }
                    break;
                case AsyncOperationStatus.Failed:
                    {
                        Debug.LogError(asyncOperHandle.OperationException.Message);
                    }
                    break;
            }

            return result;
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
            asyncOperHandle.Completed += (_) =>
            {
                data.instanceObjectList.Add(asyncOperHandle.Result);
            };
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
                foreach (var instanceObject in data.instanceObjectList)
                {
                    Addressables.ReleaseInstance(instanceObject);
                }

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
    }
}