using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Unity.VisualScripting;
using UnityEditor.TerrainTools;

public class OBJPlacerEditorWindow : EditorWindow
{
    private OBJPlacerScriptableOBJ serializedClass;
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

    // on load
    void OnEnable()
    {
        hideFlags = HideFlags.HideAndDontSave;

        //serializedClass = (OBJPlacerScriptableOBJ)Resources.Load("OBJ Placer Scriptable OBJ.asset") as OBJPlacerScriptableOBJ;

        if (serializedClass == null)
        {
            serializedClass = CreateInstance<OBJPlacerScriptableOBJ>();
            //AssetDatabase.CreateAsset(serializedClass, "Assets/Scripts/OBJ Placer Scriptable OBJ.asset");
            //AssetDatabase.SaveAssets();
            //AssetDatabase.Refresh();
        }

        serializedObject = new SerializedObject(serializedClass);
    }

    // generate GUI if no editor updates
    public void CreateGUI()
    {
        VisualElement root = new VisualElement();

        visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UXML/OBJPlacementMainEditor.uxml");
        visualTree.CloneTree(root);

        StyleSheet sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/USS/OBJPlacementMainEditor.uss");
        root.styleSheets.Add(sheet);
        rootVisualElement.Add(root);

        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.duringSceneGui += OnSceneGUI;
    }

    void OnSceneGUI(SceneView sceneView)
    {
        Handles.color = Color.yellow;

        Vector3 mousePosition = Event.current.mousePosition;
        //mousePosition.y = sceneView.camera.pixelHeight - mousePosition.y;
        //mousePosition = sceneView.camera.ScreenToWorldPoint(mousePosition);
        //mousePosition.y = -mousePosition.y;

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
        // get UI data - two way via bindings so dont need to set serialized object data
        brushSize = rootVisualElement.Q<Slider>(name: "bSize").value;
        brushEnabled = rootVisualElement.Q<Toggle>(name: "bToggle").value;

        //serializedObject.Update();

        //if (GUI.changed)
        //{
        //    Undo.RecordObject(serializedClass, "Scriptable Modify");
        //    EditorUtility.SetDirty(serializedClass);
        //}

        //serializedObject.ApplyModifiedProperties();
    }
}