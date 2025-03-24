#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Unity.VisualScripting;
using System.Collections.Generic;
using UnityEditor.EditorTools;
using UnityEditor.ShortcutManagement;

[EditorTool("OBJ Brush")]
public class OBJPlacerEditorTool : EditorTool, IDrawSelectedHandles
{
    private static OBJPlacerScriptableOBJ serializedClass;
    Vector3 mouseWorldPos;

    void OnEnable()
    {
        serializedClass = AssetDatabase.LoadAssetAtPath<OBJPlacerScriptableOBJ>("Assets/Scripts/OBJ Placer Scriptable OBJ.asset");

        if (!serializedClass)
        {
            serializedClass = CreateInstance<OBJPlacerScriptableOBJ>();
            AssetDatabase.CreateAsset(serializedClass, "Assets/Scripts/OBJ Placer Scriptable OBJ.asset");
            AssetDatabase.Refresh();
        }

        //SceneView.duringSceneGui += OnScene;
    }

    void OnDisable()
    {
        
    }

    // Called when the active tool is set to this tool instance.
    public override void OnActivated()
    {
        SceneVisibilityManager.instance.DisableAllPicking();
    }

    // Called before the active tool is changed, or destroyed.
    public override void OnWillBeDeactivated()
    {
        SceneVisibilityManager.instance.EnableAllPicking();
    }

    // Equivalent to Editor.OnSceneGUI.
    public override void OnToolGUI(EditorWindow window)
    {
        Event e = Event.current;

        Handles.color = Color.yellow;
        Gizmos.color = Color.yellow;

        Vector3 mousePosition = e.mousePosition;

        Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100))
        {
            mouseWorldPos = hit.point;
            if (serializedClass.serializableData.brushEnabled)
            {
                Vector3 surfaceNormal = hit.normal.normalized;
                Handles.DrawWireDisc(hit.point, hit.normal, serializedClass.serializableData.brushSize/2);
                if (serializedClass.serializableData.tempObj != null && e.type == EventType.MouseDown && e.button == 0)
                {
                    int rand = Random.Range(0, 100);
                    if (rand < serializedClass.serializableData.density)
                    {
                        var obj = PrefabUtility.InstantiatePrefab(serializedClass.serializableData.tempObj);
                        SceneVisibilityManager.instance.DisablePicking((GameObject)obj, false);
                        
                        obj.GetComponent<Transform>().rotation = Quaternion.FromToRotation(obj.GetComponent<Transform>().up, surfaceNormal);

                        Vector3 spawnPos = hit.point; spawnPos += surfaceNormal * (obj.GetComponent<Renderer>().bounds.size.y / 2);
                        obj.GetComponent<Transform>().position = spawnPos;
                    }
                }
            }
        }
    }

    public void OnDrawHandles()
    {

    }
}

#endif