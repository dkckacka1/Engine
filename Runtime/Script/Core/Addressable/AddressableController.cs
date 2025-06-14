using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Engine.Util;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Engine.Core.Addressable
{
    public class CacheData
    {
        public string CachingType;
        public UnityEngine.Object OriginObject;
    }

    public class AddressableController
    {
        private readonly Dictionary<string, CacheData> _loadedAddressableDic = new();
        private readonly HashSet<string> _loadingHashSet = new();

        public const string PermanentTypeName = "Permanent"; // 영구적 지속
        public const string SceneTypeName = "Scene"; // 해당씬에서만 지속

        public AddressableController()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        
        public async Task<T> LoadAssetAsync<T>(string address, string cachingTypeName = SceneTypeName) where T : UnityEngine.Object
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
            cacheData = new CacheData();
            cacheData.CachingType = cachingTypeName;

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
        
        public async Task<List<T>> LoadAssetsLabelAsync<T>(string labelName, string customTypeName = SceneTypeName)
            where T : UnityEngine.Object
        {
            var locateList = await LoadAssetLabelLocationList(labelName);
            var resultList = new List<T>();

            foreach (var locate in locateList)
            {
                var loadAsset = await LoadAssetAsync<T>(locate.InternalId);
            }

            return resultList;
        }
        
        public async Task<IList<IResourceLocation>> LoadAssetLabelLocationList(string labelName)
        {
            AssetLabelReference assetLabel = new AssetLabelReference
            {
                labelString = labelName
            };

            if (assetLabel.RuntimeKeyIsValid() is false)
            {
                throw new Exception($"{labelName} Invalid Name");
            }

            return await Addressables.LoadResourceLocationsAsync(labelName);
        }

        
        public async Task<T> InstantiateObject<T>(
            string address,
            Transform parent = null,
            string customTypeName = SceneTypeName,
            Action<AsyncOperationHandle<GameObject>> handleComplete = null
            ) where T : UnityEngine.Object
        {
            if (_loadedAddressableDic.ContainsKey(address) is false)
            {
                await LoadAssetAsync<T>(address, customTypeName);
            }

            var data = _loadedAddressableDic[address];

            var asyncOperHandle = (parent == null) ? Addressables.InstantiateAsync(address) : Addressables.InstantiateAsync(address, parent);
            asyncOperHandle.Completed += handleComplete;
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

        public async UniTask<SceneInstance> LoadSceneAsync(string sceneAddress, LoadSceneMode loadSceneMode)
        {
            return await Addressables.LoadSceneAsync(sceneAddress, loadSceneMode, activateOnLoad: false);
        }

        public void ReleaseAll(string cachingTypeName)
        {
            if (_loadedAddressableDic.ContainsKey(cachingTypeName) is false)
                return;
            
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
            ReleaseAll(SceneTypeName);
        }

        private static bool IsComponentType<T>()
        {
            return typeof(Component).IsAssignableFrom(typeof(T));
        }
    }
}