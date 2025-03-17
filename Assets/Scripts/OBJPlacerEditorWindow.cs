using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Unity.VisualScripting;

public class OBJPlacerEditorWindow : EditorWindow
{
    private OBJPlacerScriptableOBJ serializedClass;

    [SerializeField] VisualTreeAsset visualTree;

    [MenuItem("OBJ Placement/Placement Tool")]
    public static void ShowWindow()
    {
        OBJPlacerEditorWindow window = GetWindow<OBJPlacerEditorWindow>();
        window.titleContent = new GUIContent("Placement Tool");
    }

    void OnEnable()
    {
        hideFlags = HideFlags.HideAndDontSave;

        serializedClass = (OBJPlacerScriptableOBJ)Resources.Load("OBJ Placer Scriptable OBJ.asset") as OBJPlacerScriptableOBJ;

        if (serializedClass == null)
        {
            serializedClass = CreateInstance<OBJPlacerScriptableOBJ>();
            AssetDatabase.CreateAsset(serializedClass, "Assets/Scripts/OBJ Placer Scriptable OBJ.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    public void CreateGUI()
    {
        serializedClass.OnGUI(rootVisualElement);
    }
}