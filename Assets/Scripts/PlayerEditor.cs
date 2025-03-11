using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;
using System;

[CustomEditor(typeof(Player))]
public class PlayerEditor : Editor
{
    private SerializedProperty healthProperty;
    private SerializedProperty speedProperty;
    private int selectedIndex = 0;
    private AnimBool showExtra;

    string m_String;
    Color m_Color = Color.white;
    int m_Number = 0;

    private void OnEnable()
    {
        healthProperty = serializedObject.FindProperty("m_health");
        speedProperty = serializedObject.FindProperty("m_moveSpeed");
        showExtra = new AnimBool(false);
        showExtra.valueChanged.AddListener(Repaint);
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // sets index to value, button remains blue / selected
        selectedIndex = GUILayout.SelectionGrid(selectedIndex, new string[] { "Custom", "Default"}, 2);
        if (selectedIndex == 0)
        {
            EditorGUILayout.Slider(healthProperty, 0f, 100f);
            EditorGUILayout.Slider(speedProperty, 0f, 25f);

            EditorGUILayout.LabelField($"hp - {healthProperty.floatValue}, speed - {speedProperty.floatValue}");

            // button row   -   can also use begin vertical
            EditorGUILayout.BeginHorizontal();
            GUILayout.Button("1");
            GUILayout.Button("2");
            GUILayout.Button("3");
            EditorGUILayout.EndHorizontal();

            showExtra.target = EditorGUILayout.ToggleLeft("Show Extra Fields", showExtra.target);

            if (EditorGUILayout.BeginFadeGroup(showExtra.faded))
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PrefixLabel("Color");
                m_Color = EditorGUILayout.ColorField(m_Color);
                EditorGUILayout.PrefixLabel("Text");
                m_String = EditorGUILayout.TextField(m_String);
                EditorGUILayout.PrefixLabel("Number");
                m_Number = EditorGUILayout.IntSlider(m_Number, 0, 10);

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFadeGroup();

            GUILayout.Box("Something");
        }
        else
        {
            this.DrawDefaultInspector();
        }
        



        serializedObject.ApplyModifiedProperties();
    }
}
