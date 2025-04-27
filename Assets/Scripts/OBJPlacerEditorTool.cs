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
        serializedClass.GenerateSceneOBJGroups();

        serializedClass.RegenWeights();

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
        float area = Mathf.PI * ((serializedClass.brushSize/2) * (serializedClass.brushSize/2));

        float avgObjRadius = 0;
        int count = 0;

        for (int i = 0; i < serializedClass.groups.Count; i++)
        {
            for (int j = 0; j < serializedClass.groups[i].items.Count; j++)
            {
                avgObjRadius += serializedClass.groups[i].items[j].gObject.GetComponent<Renderer>().bounds.extents.magnitude;
                count ++;
            }
        }

        avgObjRadius /= count;

        float objMax = area / ((avgObjRadius * avgObjRadius) * Mathf.Sqrt(12));

        int objToSpawn = (int)(objMax * (serializedClass.density / 100f));

        Debug.Log(objToSpawn);

        return objToSpawn;
    }

    /// <summary>
    /// use both a random radius and random rotation for the right vector to generate a random spawn position from placement origin
    /// </summary>
    /// <returns></returns>
    (Vector3, Vector3) GenerateRandomSpawnPosition(Vector3 hitPos, Vector3 surfaceNormal)
    {
        // uniform distribution
        float randRadius = Mathf.Sqrt(Random.value) * serializedClass.brushSize / 2;
        float randomRotation = Random.Range(0f, 360f);

        float rad = randomRotation * Mathf.Deg2Rad;
        Vector3 randomPos = new Vector3(Mathf.Cos(rad) - Mathf.Sin(rad), 0, Mathf.Sin(rad) + Mathf.Cos(rad));
        randomPos.Normalize();
        randomPos *= randRadius;

        if (surfaceNormal != Vector3.up){
            randomPos.y = randomPos.z;
            randomPos.z = 0;
            Vector2 right = Vector3.Cross(surfaceNormal, Vector3.up).normalized;
            float angle = Vector2.SignedAngle(right, randomPos);
            Quaternion rot = Quaternion.AngleAxis(angle, surfaceNormal);
            Vector3 newPos = rot * right;
            newPos *= randomPos.magnitude;
            randomPos = newPos;
        }

        // use random pos to get new ray origin to then re calc ray along offset to get prefab specific normal
        RaycastHit newPosHit;

        Vector3 newOirigin = (hitPos + randomPos) + (surfaceNormal * 10f);

        if (Physics.Raycast(newOirigin, -surfaceNormal, out newPosHit, 100f)){
            return (newPosHit.point, newPosHit.normal.normalized);
        }
        else{
            return (Vector3.zero, Vector3.down);
        }
    }

    /// <summary>
    /// check for missing refences in the list of groups
    /// </summary>
    /// <returns></returns>
    bool CheckForMissingReferences()
    {
        bool output = false;

        if (serializedClass.groups == null || serializedClass.groups.Count == 0)
        {
            Debug.LogException(new System.Exception($"Tool Group(s) missing / empty!"));
            return true;
        }

        // check for missing references
        for (int i = 0; i < serializedClass.groups.Count; i++)
        {
            if (serializedClass.groups[i].items == null || serializedClass.groups[i].items.Count == 0)
            {
                Debug.LogException(new System.Exception($"Group Item(s) is missing / empty! {serializedClass.groups[i].name}"));
                output = true;
            }

            for (int j = 0; j < serializedClass.groups[i].items.Count; j++)
            {
                if (serializedClass.groups[i].items[j].gObject == null)
                {
                    Debug.LogException(new System.Exception($"Group Item(s) is missing an assigned Object! {serializedClass.groups[i].name}, Object: {j+1}"));
                    output = true;
                }
            }
        }

        return output;
    }

    (GameObject, int) GetOBJToSpawn()
    {
        int rand = Random.Range(0, 100);
        int temp = 0;

        for (int i = 0; i < serializedClass.groups.Count; i++)
        {
            if (i > 0)
            {
                temp += serializedClass.groups[i-1].weight;
            }

            if (rand > temp && rand <= temp + serializedClass.groups[i].weight)
            {
                int rand2 = Random.Range(0, 100);
                int temp2 = 0;

                for (int j = 0; j < serializedClass.groups[i].items.Count; j++)
                {
                    if (j > 0)
                    {
                        temp2 += serializedClass.groups[i].items[j-1].weight;
                    }

                    if (rand2 > temp2 && rand2 <= temp2 + serializedClass.groups[i].items[j].weight)
                    {
                        return (serializedClass.groups[i].items[j].gObject, i);
                    }
                }
            }
        }

        return (null, 0);
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
                    (Vector3, Vector3) pos = GenerateRandomSpawnPosition(hit.point, surfaceNormal);

                    if (pos.Item2 != Vector3.down)  // if valid ray cast
                    {
                        (GameObject, int) spawnData = GetOBJToSpawn();

                        if (spawnData.Item1 != null)
                        {
                            GameObject obj = PrefabUtility.InstantiatePrefab(spawnData.Item1) as GameObject;
                            SceneVisibilityManager.instance.DisablePicking(obj, false);

                            obj.layer = LayerMask.NameToLayer("Ignore Raycast");

                            obj.GetComponent<Transform>().SetParent(serializedClass.groupParents[spawnData.Item2].transform, true);

                            obj.GetComponent<Transform>().rotation = Quaternion.FromToRotation(obj.GetComponent<Transform>().up, pos.Item2);

                            pos.Item1 += pos.Item2 * (obj.GetComponent<Renderer>().bounds.size.y / 2);

                            obj.GetComponent<Transform>().position = pos.Item1;
                        }
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