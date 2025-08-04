using System.IO;
using Engine.EditorUtil;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class SetDependenciesEditorWindow : EditorWindow
{
    [SerializeField] 
    private VisualTreeAsset windowTree;
    
    [SerializeField]
    private VisualTreeAsset element;

    [SerializeField] 
    private DependenceData dependenceData;
    
    private ScrollView _elementScrollView;

    [MenuItem("Tools/DependenciesWindow")]
    public static void ShowExample()
    {
        var wnd = GetWindow<SetDependenciesEditorWindow>();
        wnd.titleContent = new GUIContent("SetDependenciesEditorWindow");
    }

    public void CreateGUI()
    {
        windowTree.CloneTree(rootVisualElement);
        
        BindUIElement();
    }

    private void BindUIElement()
    {
        _elementScrollView = rootVisualElement.Q<ScrollView>("ElementScrollView");

        var showManifestButton = rootVisualElement.Q<Button>("ShowManifestButton");
        showManifestButton.clicked += () =>
        {
            var manifestPath = Path.Combine(Application.dataPath.Replace("Assets", string.Empty), "Packages/manifest.json");
            if (File.Exists(manifestPath))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                {
                    FileName = manifestPath,
                    UseShellExecute = true
                });
            }
            else
            {
                Debug.LogError("manifest.json not found at: " + manifestPath);
            }
        };
        
        foreach (var dependency in dependenceData.gitDataList)
        {
            var elementClone = element.CloneTree();
            SetData(elementClone, dependency);
            _elementScrollView.contentContainer.Add(elementClone);
        }
    }

    private void SetData(VisualElement elementTree, GitData dependency)
    {
        var packageNameLabel = elementTree.Q<Label>("PackageName");
        var packageVersionLabel = elementTree.Q<Label>("PackageVersion");
        var packageAddressLabel = elementTree.Q<Label>("PackageAddress");
        
        packageNameLabel.text = dependency.GitName;
        packageVersionLabel.text = dependency.GitVersion;
        packageAddressLabel.text = dependency.GitURL;
        
        var isInstalled = SetDependencies.CheckPackageInstalled(dependency.GitURL, out var manifestText);
        
        var addPackageButton = elementTree.Q<Button>("AddPackageButton");
        addPackageButton.SetEnabled(!isInstalled);
        
        addPackageButton.clicked += () =>
        {
            SetDependencies.AddPackage(dependency.GitName, dependency.GitURL + $"#{dependency.GitVersion}");
            Refresh();
        };
        
        var removePackageButton = elementTree.Q<Button>("DeletePackageButton");
        removePackageButton.SetEnabled(isInstalled);
        
        removePackageButton.clicked += () =>
        {
            SetDependencies.RemovePackage(dependency.GitName);
            Refresh();
        };
    }

    private void Refresh()
    {
        Repaint(); // 창 다시 그리기 (OnGUI, UI Toolkit 적용 시에도 효과)
        GUI.changed = true; // GUI가 변경되었음을 Unity에 알림
    }
}
