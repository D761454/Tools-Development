using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

public class OBJPlacerEditorWindow : EditorWindow
{
    [SerializeField] private int genWidth;
    [SerializeField] private int genHeight;
    [SerializeField] private static float brushSize = 50f;

    [MenuItem("OBJ Placement/Placement Tool")]
    public static void ShowWindow()
    {
        OBJPlacerEditorWindow window = GetWindow<OBJPlacerEditorWindow>();
        window.titleContent = new GUIContent("Placement Tool");
        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.duringSceneGui += OnSceneGUI;
    }

    public void CreateGUI()
    {
        VisualElement root = new VisualElement();

        VisualTreeAsset asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UXML/OBJPlacementMainEditor.uxml");
        asset.CloneTree(root);

        root.Bind();

        StyleSheet sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/USS/OBJPlacementMainEditor.uss");
        root.styleSheets.Add(sheet);

        rootVisualElement.Add(root);
    }

    static void OnSceneGUI(SceneView sceneView)
    {
        //https://discussions.unity.com/t/how-to-get-mouseposition-in-scene-view/519147

        Handles.BeginGUI();
        Handles.DrawWireDisc(Event.current.mousePosition, Vector3.forward, brushSize);
        Handles.EndGUI();
    }
}