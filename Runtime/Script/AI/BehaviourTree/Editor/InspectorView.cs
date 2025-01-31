using UnityEditor;
using UnityEngine.UIElements;
using Engine.AI.BehaviourTree;

[UxmlElement("InspectorView")]
public partial class InspectorView : VisualElement
{
    Editor editor;

    public InspectorView()
    {

    }

    internal void UpdateTreeObject(BehaviourTree tree)
    {
        Clear();

        UnityEngine.Object.DestroyImmediate(editor);

        if (tree is null) return;

        editor = Editor.CreateEditor(tree);
        IMGUIContainer container = new IMGUIContainer(() =>
        {
            if (editor.target)
            {
                editor.OnInspectorGUI();
            }
        });
        Add(container);
    }

    internal void UpdateSelection(NodeView nodeView)
    {
        Clear();

        UnityEngine.Object.DestroyImmediate(editor);
        editor = Editor.CreateEditor(nodeView.node);

        IMGUIContainer container = new IMGUIContainer(() =>
        { 
            if (editor.target)
            {
                editor.OnInspectorGUI(); 
            }
        });
        Add(container);
    }
}
