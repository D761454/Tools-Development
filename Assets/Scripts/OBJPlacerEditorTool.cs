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

    public float brushSize = 100f;
    public bool brushEnabled = false;
    public int density = 50;
    public List<GroupStruct> groups;
    public GameObject tempObj;
    public int tempWeight = 50;

    public Dictionary<string, object> toolData = new Dictionary<string, object>()
    {
        { "brushSizeFloat", 50f },
        { "brushEnabledBool", false },
        { "densityInt", 50 },
        { "tempObj", null },
        { "tempWeight", 50 }
    };

    void OnEnable()
    {
        serializedClass = AssetDatabase.LoadAssetAtPath<OBJPlacerScriptableOBJ>("Assets/Scripts/OBJ Placer Scriptable OBJ.asset");

        if (!serializedClass)
        {
            serializedClass = CreateInstance<OBJPlacerScriptableOBJ>();
            AssetDatabase.CreateAsset(serializedClass, "Assets/Scripts/OBJ Placer Scriptable OBJ.asset");
            AssetDatabase.Refresh();
        }
        toolData["brushSizeFloat"] = serializedClass.data["brushSizeFloat"];
        toolData["brushEnabledBool"] = serializedClass.data["brushEnabledBool"];
        toolData["densityInt"] = serializedClass.data["densityInt"];
        toolData["tempObj"] = serializedClass.data["tempObj"];
        toolData["tempWeight"] = serializedClass.data["tempWeight"];

        //brushSize = serializedClass.brushSize;
        //brushEnabled = serializedClass.brushEnabled;
        //density = serializedClass.density;
        //groups = serializedClass.groups;
        //tempObj = serializedClass.tempObj;
        //tempWeight = serializedClass.tempWeight;
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

        Vector3 mousePosition = e.mousePosition;

        Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
        RaycastHit hit;

        // update info on gui changes
        if (EditorWindow.HasOpenInstances<OBJPlacerEditorWindow>() && toolData != serializedClass.data)
        {
            toolData["brushSizeFloat"] = serializedClass.data["brushSizeFloat"];
            toolData["brushEnabledBool"] = serializedClass.data["brushEnabledBool"];
            toolData["densityInt"] = serializedClass.data["densityInt"];
            toolData["tempObj"] = serializedClass.data["tempObj"];
            toolData["tempWeight"] = serializedClass.data["tempWeight"];
        }
        else if (Physics.Raycast(ray, out hit, 100))
        {
            Handles.BeginGUI();
            if ((bool)toolData["brushEnabledBool"])
            {
                Handles.DrawWireDisc(mousePosition, Vector3.forward, (float)toolData["brushSizeFloat"]);
                if ((GameObject)toolData["tempObj"] != null && e.type == EventType.MouseDown && e.button == 0)
                {
                    int rand = Random.Range(0, 100);
                    if (rand < (int)toolData["tempWeight"])
                    {
                        var obj = PrefabUtility.InstantiatePrefab((GameObject)toolData["tempObj"]);
                        SceneVisibilityManager.instance.DisablePicking((GameObject)obj, false);
                        Vector3 spawnPos = hit.point; spawnPos.y += obj.GetComponent<Renderer>().bounds.size.y / 2;
                        Vector3 surfaceNormal = hit.normal.normalized;

                        obj.GetComponent<Transform>().position = spawnPos;
                        obj.GetComponent<Transform>().rotation = Quaternion.FromToRotation(obj.GetComponent<Transform>().up, surfaceNormal);

                        // note: do pos after rotation to move obj along surface normal instead of world y
                    }
                }
            }
            Handles.EndGUI();
        }
    }

    public void OnDrawHandles()
    {
        
    }
}

#endif