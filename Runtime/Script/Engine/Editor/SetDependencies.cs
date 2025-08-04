#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;

namespace Engine.EditorUtil
{
    public class SetDependencies : Editor
    {
        public static void AddPackage(string name, string url)
        {
            var manifestPath = Path.Combine(Application.dataPath.Replace("Assets", string.Empty), "Packages/manifest.json");

            var manifestText = File.ReadAllText(manifestPath);
            if (!manifestText.Contains(name))
            {
                var modifiedText = manifestText.Insert(manifestText.IndexOf("dependencies") + 16, $"\n\t\"{name}\": \"{url}\",\n");
                File.WriteAllText(manifestPath, modifiedText);
                Debug.Log($"Added {name} to manifest.json");
                UnityEditor.PackageManager.Client.Resolve();
            }
        }

        public static void RemovePackage(string name)
        {
            var manifestPath = Path.Combine(Application.dataPath.Replace("Assets", string.Empty), "Packages/manifest.json");
            if (!File.Exists(manifestPath))
            {
                Debug.LogError($"manifest.json not found at '{manifestPath}'");
                return;
            }
            
            var manifestText = File.ReadAllText(manifestPath);

            // "name": "url" 패턴 찾기 (뒤에 , 또는 줄바꿈이 있을 수 있음)
            var pattern = $"\\s*\"{Regex.Escape(name)}\"\\s*:\\s*\"[^\"]*\",?\\s*\\n?";
            var newManifestText = Regex.Replace(manifestText, pattern, string.Empty);

            if (manifestText != newManifestText)
            {
                File.WriteAllText(manifestPath, newManifestText);
                Debug.Log($"Removed {name} from manifest.json");
                UnityEditor.PackageManager.Client.Resolve();
            }
            else
            {
                Debug.LogWarning($"{name} not found in manifest.json");
            }
        }

        public static bool CheckPackageInstalled(string packageName, out string manifestText)
        {
            var manifestPath = Path.Combine(Application.dataPath.Replace("Assets", string.Empty), "Packages/manifest.json");
            if (!File.Exists(manifestPath))
            {
                Debug.LogError($"manifest.json not found at '{manifestPath}'");
                manifestText = string.Empty;
                return false;
            }
            
            manifestText = File.ReadAllText(manifestPath);
            return manifestText.Contains(packageName);
        }
    }
}
#endif
