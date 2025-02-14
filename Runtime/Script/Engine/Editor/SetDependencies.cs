using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

namespace Engine.EditorUtil
{
    [InitializeOnLoad]
    public class SetDependencies : Editor
    {
        private static DependenceData dependenceData;

        static SetDependencies()
        {
            dependenceData = Resources.Load<DependenceData>("DependenceData");

            foreach (var dependence in dependenceData.gitDataList)
            {
                Debug.Log($"CheckDependence : {dependence.GitName}");
                bool checkUnitaskInstalled = CheckPackageInstalled(dependence.GitName);

                if (!checkUnitaskInstalled)
                {
                    AddPackage(dependence.GitName, dependence.GitURL);
                }
                else
                {
                    Debug.Log($"{dependence.GitName} is Loaded!");
                }
            }
        }

        private static void AddPackage(string name, string url)
        {
            string manifestPath = Path.Combine(Application.dataPath.Replace("Assets", string.Empty), "Packages/manifest.json");
            if (!File.Exists(manifestPath))
            {
                Debug.LogError($"manifest.json not found at '{manifestPath}'");
                return;
            }

            string manifestText = File.ReadAllText(manifestPath);
            if (!manifestText.Contains(name))
            {
                Debug.Log($"{name} not found in manifest.json");
                var modifiedText = manifestText.Insert(manifestText.IndexOf("dependencies") + 17, $"\t\"{name}\": \"{url}\",\n");
                File.WriteAllText(manifestPath, modifiedText);
                Debug.Log($"Added {name} to manifest.json");
            }
            UnityEditor.PackageManager.Client.Resolve();
        }

        private static bool CheckPackageInstalled(string packageName)
        {
            string manifestPath = Path.Combine(Application.dataPath.Replace("Assets", string.Empty), "Packages/manifest.json");
            string manifestText = File.ReadAllText(manifestPath);
            return manifestText.Contains(packageName);
        }
    }
}