using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Unity.Template.VR
{
    public static class AddressableUtil
    {
        /// <summary>
        /// Loads the addressable with the given key and parses the given bytes/text to the provided type
        /// </summary>
        /// <param name="key"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static async Task<T> LoadAndParseText<T>(string key) where T: class
        {
            var loadTask = Addressables.LoadAssetAsync<TextAsset>(key);
            await loadTask.Task;

            T result = default(T);

            if (loadTask.Status == AsyncOperationStatus.Failed)
            {
                Debug.LogError("Failed to load initial song entries");
            }
            else
            {
                var rawData = loadTask.Result.bytes;
                try
                {
                    result = JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(rawData));
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to parse data: {ex.Message}");
                }
            }
            
            Addressables.Release(loadTask);
            return result;
        }
    }
}