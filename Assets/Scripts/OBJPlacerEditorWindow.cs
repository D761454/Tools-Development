using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Unity.VisualScripting;
using UnityEditor.TerrainTools;

public class OBJPlacerEditorWindow : EditorWindow
{
    private static OBJPlacerScriptableOBJ serializedClass;
    private SerializedObject serializedObject;

    [SerializeField] VisualTreeAsset visualTree;

    public float brushSize = 100f;
    public bool brushEnabled = false;

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

        var objField = new ObjectField();
        objField.objectType = typeof(GameObject);
        objField.label = "Object:";
        objField.AddToClassList("objectField");
        
        var groups = root.Query<VisualElement>(name: "group").ToList();
        foreach ( var group in groups )
        {
            group.Add(objField);
        }

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

        serializedObject.ApplyModifiedProperties();
    }
}