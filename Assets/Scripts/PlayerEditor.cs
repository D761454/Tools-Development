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

    // for fade out
    string m_String;
    Color m_Color = Color.white;
    int m_Number = 0;

    // for fake transform
    bool fold = true;
    Vector4 rotationComponents;
    Transform selectedTransform;

    // pop up / selector
    public string[] options = new string[] { "Cube", "Sphere", "Plane" };
    public int index = 0;

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
            index = EditorGUILayout.Popup(index, options);
            if (GUILayout.Button("Create"))
            {
                // do stuff
                //switch (index)
                //{
                //    case 0:
                //        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                //        cube.transform.position = Vector3.zero;
                //        break;
                //    case 1:
                //        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                //        sphere.transform.position = Vector3.zero;
                //        break;
                //    case 2:
                //        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
                //        plane.transform.position = Vector3.zero;
                //        break;
                //    default:
                //        Debug.LogError("Unrecognized Option");
                //        break;
                //}
            }

            if (Selection.activeGameObject)
            {
                selectedTransform = Selection.activeGameObject.transform;

                fold = EditorGUILayout.InspectorTitlebar(fold, selectedTransform);
                if (fold)
                {
                    selectedTransform.position = EditorGUILayout.Vector3Field("Position", selectedTransform.position);
                    EditorGUILayout.Space();

                    rotationComponents = EditorGUILayout.Vector4Field("Detailed Rotation", QuaternionToVector4(selectedTransform.localRotation));
                    EditorGUILayout.Space();

                    selectedTransform.localScale = EditorGUILayout.Vector3Field("Scale", selectedTransform.localScale);
                }

                selectedTransform.localRotation = ConvertToQuaternion(rotationComponents);
                EditorGUILayout.Space();
            }

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

    Quaternion ConvertToQuaternion(Vector4 v4)
    {
        return new Quaternion(v4.x, v4.y, v4.z, v4.w);
    }

    Vector4 QuaternionToVector4(Quaternion q)
    {
        return new Vector4(q.x, q.y, q.z, q.w);
    }
}
