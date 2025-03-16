using UnityEngine;
using UnityEditor;

public class OBJPrefabditorWindow : EditorWindow
{
    [MenuItem("OBJ Placement/Prefab Editor")]
    public static void ShowWindow()
    {
        OBJPrefabditorWindow window = GetWindow<OBJPrefabditorWindow>();
        window.titleContent = new GUIContent("OBJ Placement Prefab Editor");
    }

    public void OnGUI() 
    {
        EditorGUILayout.LabelField("Helloooo");
    }
}
