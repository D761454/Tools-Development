using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Unity.VisualScripting;
using UnityEditor.TerrainTools;
using System.Collections.Generic;

public class OBJPlacerEditorWindow : EditorWindow
{
    private static OBJPlacerScriptableOBJ serializedClass;
    private SerializedObject serializedObject;

    [SerializeField] VisualTreeAsset visualTree;
    [SerializeField] VisualTreeAsset groupTree;

    public float brushSize = 100f;
    public bool brushEnabled = false;
    public int density = 50;
    public List<GroupStruct> groups;
    public GameObject tempObj;

    [MenuItem("OBJ Placement/Placement Tool")]
    public static void Init()
    {
        OBJPlacerEditorWindow window = GetWindow<OBJPlacerEditorWindow>();
        window.titleContent = new GUIContent("Placement Tool");
    }

    [InitializeOnLoadMethod]
    private static void OnLoad()
    {
        serializedClass = AssetDatabase.LoadAssetAtPath<OBJPlacerScriptableOBJ>("Assets/Scripts/OBJ Placer Scriptable OBJ.asset");

        if (!serializedClass)
        {
            serializedClass = CreateInstance<OBJPlacerScriptableOBJ>();
            AssetDatabase.CreateAsset(serializedClass, "Assets/Scripts/OBJ Placer Scriptable OBJ.asset");
            AssetDatabase.Refresh();
        }
    }

    // generate GUI if no editor updates
    public void CreateGUI()
    {
        VisualElement root = new VisualElement();

        visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UXML/OBJPlacementMainEditor.uxml");
        visualTree.CloneTree(root);

        StyleSheet sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/USS/OBJPlacementMainEditor.uss");
        root.styleSheets.Add(sheet);

        groupTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UXML/GroupUI.uxml");

        var groups = root.Q<ListView>();
        groups.makeItem = groupTree.CloneTree;

        rootVisualElement.Add(root);

        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.duringSceneGui += OnSceneGUI;
    }

    void OnSceneGUI(SceneView sceneView)
    {
        Handles.color = Color.yellow;

        Vector3 mousePosition = Event.current.mousePosition;

        Handles.BeginGUI();
        if (brushEnabled)
        {
            Handles.DrawWireDisc(mousePosition, Vector3.forward, brushSize);
        }
        Handles.EndGUI();
    }

    // handle gui events
    private void OnGUI()
    {
        serializedObject = new SerializedObject(serializedClass);
        serializedObject.Update();

        // get UI data - two way via bindings so dont need to set serialized object data
        brushSize = rootVisualElement.Q<Slider>(name: "bSize").value;
        brushEnabled = rootVisualElement.Q<Toggle>(name: "bToggle").value;
        tempObj = (GameObject)rootVisualElement.Q<ObjectField>().value;

        serializedObject.ApplyModifiedProperties();
    }
}