using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Engine.Util.Extension;

namespace Engine.Core.Addressable
{
    public enum CachingType
    {
        Permanent,  // 영구적 지속
        Scene,      // 해당씬에서만 지속
        Custom      // 커스텀
    }

    public struct CacheData
    {
        public UnityEngine.Object originObject;
        public List<GameObject> instanceObjectList;
    }

    public class AddressableController
    {
        private Dictionary<string, Dictionary<string, CacheData>> loadedAddressableDic = new();

        private const string PermanentTypeName = "Permanent";
        private const string SceneTypeName = "Scene";

        private AddressableController()
        {
            EnumExtension.Foreach<CachingType>((type) =>
            {
                loadedAddressableDic.TryAdd(GetCachingTypeName(type), new Dictionary<string, CacheData>());
            });
        }

        public async Task<T> LoadAssetAsync<T>(string address, CachingType cachingType = CachingType.Scene, Action<AsyncOperationHandle<T>> handle_Complete = null) where T : UnityEngine.Object
        {
            if(loadedAddressableDic.TryGetValue(GetCachingTypeName(cachingType), out var cachingDataDic))
            {
                if(cachingDataDic.TryGetValue(address, out var cachingData))

                return cachingData.originObject as T;
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

                        CacheData cacheData = new CacheData();
                        cacheData.originObject = result;
                        cacheData.instanceObjectList = new();

                        loadedAddressableDic[GetCachingTypeName(cachingType)].Add(address, cacheData);
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

        public async Task<T> InstantiateObject<T>(string address, Transform parent = null, Action<AsyncOperationHandle<GameObject>> handle_Complete = null) where T : UnityEngine.Object
        {
            if(loadedAddressableDic.ContainsKey(address) is false)
            {
                await LoadAssetAsync<T>(address);
            }

            AsyncOperationHandle<GameObject> asyncOperHandle = Addressables.InstantiateAsync(address, parent);
            asyncOperHandle.Completed += handle_Complete;
            asyncOperHandle.Completed += (resultObj) =>
            {
                resultObj.Result.AddComponent<SelfCleanUp>();
            };

            await asyncOperHandle.Task;

            switch (asyncOperHandle.Status)
            {
                case AsyncOperationStatus.None:
                    break;
                case AsyncOperationStatus.Succeeded:
                    break;
                case AsyncOperationStatus.Failed:
                    {
                        Debug.LogError(asyncOperHandle.OperationException.Message);
                    }
                    break;
            }

            return asyncOperHandle.Result.GetComponent<T>();
        }

        public void ReleaseAll(CachingType cachingType, string customName = "Custom")
        {
            foreach(CacheData cacheData in loadedAddressableDic[GetCachingTypeName(cachingType, customName)].Values)
            {
                foreach(var instanceObject in cacheData.instanceObjectList)
                {
                    Addressables.ReleaseInstance(instanceObject);
                }

                cacheData.instanceObjectList.Clear();
                Addressables.Release(cacheData.originObject);
            }

            loadedAddressableDic[GetCachingTypeName(cachingType, customName)].Clear();
        }

        public void Release(CachingType cachingType, string key ,string customName = "Custom")
        {
            if(loadedAddressableDic.TryGetValue(GetCachingTypeName(cachingType, customName), out var dic))
            {
                if (dic.TryGetValue(key, out var cacheData))
                {
                    foreach(var instanceObject in cacheData.instanceObjectList)
                    {
                        Addressables.ReleaseInstance(instanceObject);
                    }

                    Addressables.Release(cacheData.originObject);

                    dic.Remove(key);
                }
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

        private class SelfCleanUp : MonoBehaviour
        {
            private void OnDestroy()
            {
                Addressables.ReleaseInstance(gameObject);
            }
        }
    }
}