using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Player))]
public class PlayerEditor : Editor
{
    private SerializedProperty healthProperty;
    private SerializedProperty speedProperty;

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

        serializedObject.ApplyModifiedProperties();
    }
}
