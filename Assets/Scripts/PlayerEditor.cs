using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Player))]
public class PlayerEditor : Editor
{
    private SerializedProperty healthProperty;
    private SerializedProperty speedProperty;
    private int selectedIndex;

    private void OnEnable()
    {
        healthProperty = serializedObject.FindProperty("m_health");
        speedProperty = serializedObject.FindProperty("m_moveSpeed");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Slider(healthProperty, 0f, 100f);
        EditorGUILayout.Slider(speedProperty, 0f, 25f);

        EditorGUILayout.LabelField($"hp - {healthProperty.floatValue}, speed - {speedProperty.floatValue}");

        // button row   -   can also use begin vertical
        EditorGUILayout.BeginHorizontal();
        GUILayout.Button("1");
        GUILayout.Button("2");
        GUILayout.Button("3");
        EditorGUILayout.EndHorizontal();

        // sets index to value, button remains blue / selected
        selectedIndex = GUILayout.SelectionGrid(selectedIndex, new string[] { "1..", "2..", "3..", "4.." }, 2);

        serializedObject.ApplyModifiedProperties();
    }
}
