#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Unity.VisualScripting;
using UnityEditor.EditorTools;
using UnityEditor.ShortcutManagement;
using System.ComponentModel;
using System.Collections.Generic;

[EditorTool("Brush Tool")]
[Icon("Assets/Scripts/tool-Icon.png")]
public class OBJPlacerEditorTool : EditorTool, IDrawSelectedHandles
{
    private static OBJPlacerScriptableOBJ serializedClass;

    List<GameObject> groupParents;

    /// <summary>
    /// Get/Create Serializable Obj for data storage
    /// </summary>
    void OnEnable()
    {
        groupParents = new List<GameObject>();

        serializedClass = AssetDatabase.LoadAssetAtPath<OBJPlacerScriptableOBJ>("Assets/Scripts/OBJ Placer Scriptable OBJ.asset");

        if (!serializedClass)
        {
            serializedClass = CreateInstance<OBJPlacerScriptableOBJ>();
            AssetDatabase.CreateAsset(serializedClass, "Assets/Scripts/OBJ Placer Scriptable OBJ.asset");
            AssetDatabase.Refresh();
        }
    }

    /// <summary>
    /// shortcut U to enable tool
    /// </summary>
    [Shortcut("OBJ Placement/OBJBrushTool", null, KeyCode.U, ShortcutModifiers.None)]
    public static void CustomEnable()
    {
        ToolManager.SetActiveTool<OBJPlacerEditorTool>();
    }

    void OnDisable()
    {
        
    }

    /// <summary>
    /// Disable Obj grabbing while painting + update / make group parents
    /// </summary>
    public override void OnActivated()
    {
        for (int i = 0; i < serializedClass.groups.Count; i++)
        {
            if (groupParents.Count <= i)
            {
                GameObject temp = new GameObject(serializedClass.groups[i].name);
                groupParents.Add(temp);
            }
            else
            {
                groupParents[i].name = serializedClass.groups[i].name;
            }
        }

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
    /// calculate total objects that can fit within the brush area * density
    /// </summary>
    /// <returns></returns>
    int ObjectsToSpawn()
    {
        // Note - allow overlapping Objs for forests etc - can also edit objs post placement - stretch task - add toggle to enable/disable overlap
        float area = Mathf.PI * (serializedClass.brushSize * serializedClass.brushSize);

        float avgObjRadius = serializedClass.groups[0].items[0].gObject.GetComponent<Renderer>().bounds.extents.magnitude;

        float objMax = area / ((avgObjRadius * avgObjRadius) * Mathf.Sqrt(12));

        int objToSpawn = (int)(objMax * (serializedClass.density / 100f));

        return objToSpawn;
    }

    /// <summary>
    /// use both a random radius and random rotation for the right vector to generate a random spawn position from placement origin
    /// </summary>
    /// <returns></returns>
    Vector3 GenerateRandomSpawnPosition()
    {
        // uniform distribution
        float randRadius = Mathf.Sqrt(Random.value) * serializedClass.brushSize / 2;
        float randomRotation = Random.Range(0f, 360f);

        float rad = randomRotation * Mathf.Deg2Rad;
        Vector3 randomPos = new Vector3(Mathf.Cos(rad) - Mathf.Sin(rad), 0, Mathf.Sin(rad) + Mathf.Cos(rad));
        randomPos.Normalize();
        randomPos *= randRadius;

        return randomPos;
    }

    /// <summary>
    /// check for missing refences in the list of groups
    /// </summary>
    /// <returns></returns>
    bool CheckForMissingReferences(){
        bool output = false;

        // check for missing references
        for (int i = 0; i < serializedClass.groups.Count; i++)
        {
            if (serializedClass.groups[i].items != null)
            {
                for (int j = 0; j < serializedClass.groups[i].items.Count; j++)
                {
                    if (serializedClass.groups[i].items[j].gObject == null)
                    {
                        Debug.LogException(new System.Exception($"Group Item(s) is missing an assigned Object! {serializedClass.groups[i].name}, Object: {j+1}"));
                        output = true;
                    }
                }
            }
        }

        return output;
    }

    GameObject GetOBJToSpawn(){
        
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
            Vector3 surfaceNormal = hit.normal.normalized;
            Handles.DrawWireDisc(hit.point, hit.normal, serializedClass.brushSize/2);

            if (e.type == EventType.MouseDown && e.button == 0 && !CheckForMissingReferences())
            {
                int total = ObjectsToSpawn();
                    
                for (int i = 0; i < total; i++)
                {
                    Vector3 pos = GenerateRandomSpawnPosition();

                    if (surfaceNormal != Vector3.up)
                    {
                        pos.y = pos.z;
                        pos.z = 0;
                        Vector2 right = Vector3.Cross(surfaceNormal, Vector3.up).normalized;
                        float angle = Vector2.SignedAngle(right, pos);
                        Quaternion rot = Quaternion.AngleAxis(angle, surfaceNormal);
                        Vector3 newPos = rot * right;
                        newPos *= pos.magnitude;
                        pos = newPos;
                    }

                    int index = Random.Range(0, serializedClass.groups.Count);

                    var obj = PrefabUtility.InstantiatePrefab(serializedClass.groups[index].items[0].gObject);
                    SceneVisibilityManager.instance.DisablePicking((GameObject)obj, false);

                    obj.GetComponent<Transform>().SetParent(groupParents[index].transform, true);

                    obj.GetComponent<Transform>().rotation = Quaternion.FromToRotation(obj.GetComponent<Transform>().up, surfaceNormal);

                    Vector3 spawnPos = hit.point + pos;
                    spawnPos += surfaceNormal * (obj.GetComponent<Renderer>().bounds.size.y / 2);

                    obj.GetComponent<Transform>().position = spawnPos;
                }
            }
        }
    }

    public void OnDrawHandles()
    {

    }
}

#endif