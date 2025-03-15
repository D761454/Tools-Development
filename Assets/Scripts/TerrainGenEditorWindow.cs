using UnityEngine;
using UnityEditor;

public class TerrainGenEditorWindow : EditorWindow
{
    [MenuItem("Terrain Generator/Main Window")]
    public static void ShowWindow()
    {
        TerrainGenEditorWindow window = GetWindow<TerrainGenEditorWindow>();
        window.titleContent = new GUIContent("Terrain Generator");
    }

    public void OnGUI() 
    {
        EditorGUILayout.LabelField("Helloooo");
    }
}
