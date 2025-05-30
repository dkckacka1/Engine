using UnityEditor;
using UnityEngine.UIElements;
using Engine.AI.BehaviourTree;

[UxmlElement("InspectorView")]
public partial class InspectorView : VisualElement
{
    private Editor _editor;

    public InspectorView()
    {

    }

    internal void UpdateTreeObject(BehaviourTree tree)
    {
        Clear();

        UnityEngine.Object.DestroyImmediate(_editor);

        if (tree is null) return;

        _editor = Editor.CreateEditor(tree);
        IMGUIContainer container = new IMGUIContainer(() =>
        {
            if (_editor.target)
            {
                _editor.OnInspectorGUI();
            }
        });
        Add(container);
    }

    internal void UpdateSelection(NodeView nodeView)
    {
        Clear();

        UnityEngine.Object.DestroyImmediate(_editor);

        if (nodeView is null) return;

        _editor = Editor.CreateEditor(nodeView.Node);

        IMGUIContainer container = new IMGUIContainer(() =>
        { 
            if (_editor.target)
            {
                _editor.OnInspectorGUI(); 
            }
        });
        Add(container);
    }
}
