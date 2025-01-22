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

    internal void UpdateSelection(NodeView nodeView)
    {
        Clear();

        UnityEngine.Object.DestroyImmediate(editor);
        editor = Editor.CreateEditor(nodeView.node);

        IMGUIContainer container = new IMGUIContainer(() => { editor.OnInspectorGUI(); });
        Add(container);
    }
}
