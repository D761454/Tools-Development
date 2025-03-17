using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Unity.VisualScripting;

public class OBJPlacerEditorWindow : EditorWindow
{
    [Serialize] private int genWidth;
    [Serialize] private int genHeight;
    [Serialize] private float brushSize = 50f;
    private bool brushEnabled = true;

    [MenuItem("OBJ Placement/Placement Tool")]
    public static void ShowWindow()
    {
        OBJPlacerEditorWindow window = GetWindow<OBJPlacerEditorWindow>();
        window.titleContent = new GUIContent("Placement Tool");
    }

    public void CreateGUI()
    {
        VisualElement root = new VisualElement();

        VisualTreeAsset asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UXML/OBJPlacementMainEditor.uxml");
        asset.CloneTree(root);

        StyleSheet sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/USS/OBJPlacementMainEditor.uss");
        root.styleSheets.Add(sheet);

        rootVisualElement.Add(root);

        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.duringSceneGui += OnSceneGUI;
    }

    void OnSceneGUI(SceneView sceneView)
    {
        //https://discussions.unity.com/t/how-to-get-mouseposition-in-scene-view/519147

        if (brushEnabled)
        {
            Handles.BeginGUI();
            Handles.DrawWireDisc(Event.current.mousePosition, Vector3.forward, brushSize);
            Handles.EndGUI();
        }
    }
}