#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Unity.VisualScripting;
using UnityEditor.EditorTools;
using UnityEditor.ShortcutManagement;

[EditorTool("OBJ Brush")]
public class OBJPlacerEditorTool : EditorTool, IDrawSelectedHandles
{
    private static OBJPlacerScriptableOBJ serializedClass;
    Vector3 mouseWorldPos;
    

    /// <summary>
    /// Get/Create Serializable Obj for data storage
    /// </summary>
    void OnEnable()
    {
        serializedClass = AssetDatabase.LoadAssetAtPath<OBJPlacerScriptableOBJ>("Assets/Scripts/OBJ Placer Scriptable OBJ.asset");

        if (!serializedClass)
        {
            serializedClass = CreateInstance<OBJPlacerScriptableOBJ>();
            AssetDatabase.CreateAsset(serializedClass, "Assets/Scripts/OBJ Placer Scriptable OBJ.asset");
            AssetDatabase.Refresh();
        }
    }

    [Shortcut("OBJ Placement/OBJBrushTool", null, KeyCode.U, ShortcutModifiers.None)]
    public static void CustomEnable()
    {
        ToolManager.SetActiveTool<OBJPlacerEditorTool>();
    }

    void OnDisable()
    {
        
    }

    /// <summary>
    /// Disable Obj grabbing while painting
    /// </summary>
    public override void OnActivated()
    {
        SceneVisibilityManager.instance.DisableAllPicking();
    }

    /// <summary>
    /// Enable Obj grabbing
    /// </summary>
    public override void OnWillBeDeactivated()
    {
        SceneVisibilityManager.instance.EnableAllPicking();
    }

    /// <summary>
    /// Equivalent to Editor.OnSceneGUI. Handle events for spawning Objs
    /// </summary>
    /// <param name="window"></param>
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
                    // Note - allow overlapping Objs for forests etc - can also edit objs post placement - stretch task - add toggle to enable/disable overlap
                    float area = Mathf.PI * (serializedClass.serializableData.brushSize * serializedClass.serializableData.brushSize);

                    float avgObjRadius = serializedClass.serializableData.tempObj.gameObject.GetComponent<Renderer>().bounds.extents.magnitude;

                    float objMax = area / ((avgObjRadius * avgObjRadius) * Mathf.Sqrt(12));

                    Debug.Log(objMax);

                    int objToSpawn = (int)(objMax * (serializedClass.serializableData.density / 100));

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