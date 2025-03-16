using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

public class OBJPlacerEditorWindow : EditorWindow
{
    private int genWidth;
    private int genHeight;

    [MenuItem("OBJ Placement/Placement Tool")]
    public static void ShowWindow()
    {
        OBJPlacerEditorWindow window = GetWindow<OBJPlacerEditorWindow>();
        window.titleContent = new GUIContent("Placement Tool");
    }

    public void OnGUI() 
    {
        EditorGUILayout.LabelField("Helloooo");
    }

    public void CreateGUI()
    {
        VisualElement root = new VisualElement();

        VisualTreeAsset asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UXML/terrainGeneratorEditor.uxml");
        asset.CloneTree(root);

        rootVisualElement.Add(root);
    }
}