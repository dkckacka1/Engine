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
using Object = UnityEngine.Object;

namespace Engine.Core.Addressable
{
    public class CacheData
    {
        public string CachingType;
        public Object OriginObject;
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
        
        public async Task<T> LoadAssetAsync<T>(string address, string cachingTypeName = PermanentTypeName) where T : UnityEngine.Object
        {
            if (_loadingHashSet.Contains(address))
                // 동시에 여러개의 로드 호출이 있을 경우 대기
            {
                await UniTask.WaitUntil(() => _loadingHashSet.Contains(address) is false);
            }

            if (_loadedAddressableDic.TryGetValue(address, out var cacheData))
            {
                if (IsComponentType<T>())
                {
                    return ((GameObject)cacheData.OriginObject).GetComponent<T>();
                }
                else
                {
                    return cacheData.OriginObject as T;
                }
            }

            _loadingHashSet.Add(address);
            cacheData = new CacheData
            {
                CachingType = cachingTypeName
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

                            cacheData.OriginObject = result;
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
        
        public async Task<List<T>> LoadAssetsLabelAsync<T>(string labelName, string customTypeName = PermanentTypeName)
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
            string customTypeName = PermanentTypeName
            ) where T : Object
        {
            if (_loadedAddressableDic.TryGetValue(address, out var cacheData) is false)
            {
                await LoadAssetAsync<T>(address, customTypeName);
                cacheData = _loadedAddressableDic[address];
            }

            var originObject = cacheData.OriginObject;
            var asyncOperHandle = (parent == null)
                ? Object.InstantiateAsync(originObject)
                : Object.InstantiateAsync(originObject, parent); 
            await asyncOperHandle;

            if (IsComponentType<T>())
            {
                return (asyncOperHandle.Result[0] as GameObject)?.GetComponent<T>();
            }
            else
            {
                return asyncOperHandle.Result[0] as T;
            }
        }

        public async Task<SceneInstance> LoadSceneAsync(string sceneAddress, LoadSceneMode loadSceneMode)
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