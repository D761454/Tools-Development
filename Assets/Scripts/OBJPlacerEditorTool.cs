#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.ShortcutManagement;
using System.Collections.Generic;

[EditorTool("Brush Tool")]
[Icon("Assets/Scripts/tool-Icon.png")]
public class OBJPlacerEditorTool : EditorTool, IDrawSelectedHandles
{
    private static OBJPlacerScriptableOBJ serializedClass;

    List<Vector3> brushPts = new List<Vector3>();

    RaycastHit raycastHit;

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

        if (!EditorWindow.HasOpenInstances<OBJPlacerEditorWindow>())
        {
            OBJPlacerEditorWindow.Init();
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
    (Vector3, Vector3) GenerateRandomSpawnPosition(Vector3 hitPos, Vector3 surfaceNormal, float scalar)
    {
        // uniform distribution
        float randRadius = Mathf.Sqrt(Random.value) * ((serializedClass.brushSize / 2) * scalar);
        
        float randomRotation = Random.Range(0f, 360f);

        float rad = randomRotation * Mathf.Deg2Rad;
        Vector3 randomPos = new Vector3(Mathf.Cos(rad) - Mathf.Sin(rad), 0, Mathf.Sin(rad) + Mathf.Cos(rad));
        randomPos.Normalize();
        randomPos *= randRadius;

        // use random pos to get new ray origin to then re calc ray along offset to get prefab specific normal
        RaycastHit newPosHit;

        Vector3 newOirigin = (hitPos + randomPos) + (surfaceNormal * 50f);

        if (Physics.Raycast(newOirigin, -surfaceNormal, out newPosHit, 1000f)){
            if ((serializedClass.ignoreLayers & (1 << newPosHit.collider.gameObject.layer)) == 0)
            {
                return (newPosHit.point, newPosHit.normal.normalized);
            }
            return (Vector3.zero, Vector3.down);
        }
        return (Vector3.zero, Vector3.down);
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

    /// <summary>
    /// returns a random object based on the weightings
    /// </summary>
    /// <returns></returns>
    (GameObject, int, int) GetOBJToSpawn()
    {
        float rand = Random.Range(0f, 100f);
        float temp = 0;

        for (int i = 0; i < serializedClass.groups.Count; i++)
        {
            if (i > 0)
            {
                temp += serializedClass.groups[i-1].weight;
            }

            if (rand > temp && rand <= temp + serializedClass.groups[i].weight)
            {
                float rand2 = Random.Range(0f, 100f);
                float temp2 = 0;

                for (int j = 0; j < serializedClass.groups[i].items.Count; j++)
                {
                    if (j > 0)
                    {
                        temp2 += serializedClass.groups[i].items[j-1].weight;
                    }

                    if (rand2 > temp2 && rand2 <= temp2 + serializedClass.groups[i].items[j].weight)
                    {
                        return (serializedClass.groups[i].items[j].gObject, i, j);
                    }
                }
            }
        }

        return (null, 0, 0);
    }

    /// <summary>
    /// generate the point array for the brush UI
    /// </summary>
    void GenerateBrushOutlinePoints()
    {
        brushPts = new List<Vector3>();
        float angle = 0f;

        for (int i = 0; i < 129; i++)
        {
            float rad = angle * Mathf.Deg2Rad;
            angle += 2.8125f;
            Vector3 rPos = new Vector3(Mathf.Cos(rad) - Mathf.Sin(rad), 0, Mathf.Sin(rad) + Mathf.Cos(rad));
            rPos.Normalize();

            float distance = serializedClass.brushSize / 2;
            bool found = false;

            while (!found)
            {
                RaycastHit hit;
                Vector3 sPos = rPos * distance;
                Vector3 nOg = (raycastHit.point + sPos) + (Vector3.up * 50f);

                if (Physics.Raycast(nOg, -Vector3.up, out hit, 1000f, ~serializedClass.ignoreLayers))
                {
                    brushPts.Add(hit.point);
                    found = true;
                }
                distance -= 0.1f;
            }
        }
        brushPts.Add(brushPts[0]);
    }

    void DrawHandles()
    {
        Handles.DrawAAPolyLine(2f, brushPts.ToArray());
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

        if (Physics.Raycast(ray, out raycastHit, 1000f, ~serializedClass.ignoreLayers))
        {
            GenerateBrushOutlinePoints();
            DrawHandles();

            if (e.type == EventType.MouseDown && e.button == 0 && !CheckForMissingReferences())
            {
                int total = ObjectsToSpawn();
                    
                for (int i = 0; i < total; i++)
                {
                    (Vector3, Vector3) pos;

                    if (serializedClass.brushType == Brushes.DEFAULT) // default brush
                    {
                        pos = GenerateRandomSpawnPosition(raycastHit.point, Vector3.up, 1f);
                    }
                    else // faded brush
                    {
                        if (i < total / 3)
                        {
                            pos = GenerateRandomSpawnPosition(raycastHit.point, Vector3.up, 0.4f);
                        }
                        else if ( i < ((total / 3) * 2))
                        {
                            pos = GenerateRandomSpawnPosition(raycastHit.point, Vector3.up, 0.7f);
                        }
                        else
                        {
                            pos = GenerateRandomSpawnPosition(raycastHit.point, Vector3.up, 1f);
                        }
                    }

                    if (pos.Item2 != Vector3.down)  // if valid ray cast
                    {
                        (GameObject, int, int) spawnData = GetOBJToSpawn();

                        if (spawnData.Item1 != null)
                        {
                            GameObject obj = PrefabUtility.InstantiatePrefab(spawnData.Item1) as GameObject;
                            SceneVisibilityManager.instance.DisablePicking(obj, false);

                            obj.layer = LayerMask.NameToLayer("Ignore Raycast");

                            obj.GetComponent<Transform>().SetParent(serializedClass.groupParents[spawnData.Item2].transform, true);

                            obj.GetComponent<Transform>().rotation = Quaternion.FromToRotation(obj.GetComponent<Transform>().up, pos.Item2);
                            obj.GetComponent<Transform>().Rotate(pos.Item2, Random.Range(0f, 360f), Space.World);

                            pos.Item1 += pos.Item2 * (obj.GetComponent<Renderer>().bounds.size.y / 2);

                            obj.GetComponent<Transform>().position = pos.Item1 + (pos.Item2.normalized * serializedClass.groups[spawnData.Item2].items[spawnData.Item3].yOffset);
                        }
                    }
                }
            }
        }

        HandleUtility.Repaint();
    }

    public void OnDrawHandles()
    {

    }
}

#endif