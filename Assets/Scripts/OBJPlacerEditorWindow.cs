using UnityEngine;
using UnityEditor;

public class OBJPlacerEditorWindow : EditorWindow
{
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
}
