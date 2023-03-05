using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;


public class EmitterModulationCustomEditor : EditorWindow
{
    [MenuItem("Window/UI Toolkit/EmitterModulationCustomEditor")]
    public static void ShowExample()
    {
        EmitterModulationCustomEditor wnd = GetWindow<EmitterModulationCustomEditor>();
        wnd.titleContent = new GUIContent("EmitterModulationCustomEditor");
    }

    public void CreateGUI()
    {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        // VisualElements objects can contain other VisualElement following a tree hierarchy.
        VisualElement label = new Label("Hello World! From C#");
        root.Add(label);

        // Import UXML
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/_Project/Editor/UIToolkit/EmitterModulationCustomEditor.uxml");
        VisualElement labelFromUXML = visualTree.Instantiate();
        root.Add(labelFromUXML);
    }
}