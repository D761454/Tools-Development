#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.ShortcutManagement;
using System.Collections.Generic;
using Helpers;

[EditorTool("Brush Tool")]
[Icon("Assets/CPlace/Images/tool-Icon.png")]
public class OBJPlacerEditorTool : EditorTool, IDrawSelectedHandles
{
    private static OBJPlacerScriptableOBJ serializedClass;

    List<Vector3> brushPts = new List<Vector3>();

    RaycastHit raycastHit;

    float CheckDiff = 20f;
    float handleScale = 0.01f;
    int reposPt = -1;

    /// <summary>
    /// Get/Create Serializable Obj for data storage
    /// </summary>
    void OnEnable()
    {
        serializedClass = AssetDatabase.LoadAssetAtPath<OBJPlacerScriptableOBJ>("Assets/CPlace/Scripts/SaveLoad/OBJ Placer Scriptable OBJ.asset");

        if (!serializedClass)
        {
            serializedClass = CreateInstance<OBJPlacerScriptableOBJ>();
            AssetDatabase.CreateAsset(serializedClass, "Assets/CPlace/Scripts/SaveLoad/OBJ Placer Scriptable OBJ.asset");
            AssetDatabase.Refresh();
        }
    }

    /// <summary>
    /// shortcut U to enable tool
    /// </summary>
    [Shortcut("Tools/CPlace", null, KeyCode.U, ShortcutModifiers.None)]
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

        Vector3 newOirigin = (hitPos + randomPos) + (surfaceNormal * CheckDiff);

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
                Vector3 nOg = (raycastHit.point + sPos) + (Vector3.up * CheckDiff);

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
        for (int  i = 0;  i < serializedClass.zoneTypes.Count;  i++)
        {
            GameObject gO = GameObject.Find(serializedClass.zoneTypes[i].name + "-Parent");

            if (gO)
            {
                Handles.color = gO.GetComponent<SceneZone>().m_uiColor;
                Gizmos.color = gO.GetComponent<SceneZone>().m_uiColor;

                SubZone[] sZ = gO.GetComponentsInChildren<SubZone>();

                if (sZ.Length > 0)
                {
                    for (int j = 0; j < sZ.Length; j++)
                    {
                        Handles.DrawAAPolyLine(2f, sZ[j].pointPositions.ToArray());
                        for (int k = 0; k < sZ[j].points.Count; k++)
                        {
                            Vector3 dist = sZ[j].points[k].point - SceneView.lastActiveSceneView.camera.transform.position;
                            Handles.DrawSolidDisc(sZ[j].points[k].point, sZ[j].points[k].normal, dist.magnitude * handleScale);
                        }
                    }
                }
            }
        }
    }

    void DrawRayHandles()
    {
        Handles.color = Color.yellow;
        Gizmos.color = Color.yellow;

        //Handles.DrawAAPolyLine(2f, brushPts.ToArray());

        if (serializedClass.activeSubZone)
        {
            Handles.color = serializedClass.activeSubZone.GetComponentInParent<SceneZone>().m_uiColor;
            Handles.DrawSolidDisc(raycastHit.point, raycastHit.normal, raycastHit.distance * handleScale);
        }
    }

    /// <summary>
    /// Equivalent to Editor.OnSceneGUI. Handle events for spawning Objs
    /// </summary>
    /// <param name="window"></param>
    public override void OnToolGUI(EditorWindow window)
    {

        Event e = Event.current;

        Vector3 mousePosition = e.mousePosition;

        Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);

        DrawHandles();

        // stop dragging point
        if (e.type == EventType.MouseUp)
        {
            reposPt = -1;
        }

        if (Physics.Raycast(ray, out raycastHit, 1000f, ~serializedClass.ignoreLayers))
        {
            //GenerateBrushOutlinePoints();
            DrawRayHandles();

            // drag point if point selected
            if (reposPt != -1)
            {
                RepositionPoint(ref raycastHit, reposPt);
                return;
            }

            if (serializedClass.activeSubZone && e.button == 0)
            {
                if (e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
                {
                    if (serializedClass.activeSubZone.GetComponent<SubZone>().points.Count > 0)
                    {
                        if (reposPt == -1)
                        {
                            for (int i = 0; i < serializedClass.activeSubZone.GetComponent<SubZone>().points.Count; i++)
                            {
                                if (Vector3.Distance(serializedClass.activeSubZone.GetComponent<SubZone>().points[i].point, raycastHit.point) < 2.0f)
                                {
                                    // delete point
                                    if (e.shift)
                                    {
                                        serializedClass.activeSubZone.GetComponent<SubZone>().points.RemoveAt(i);
                                        serializedClass.activeSubZone.GetComponent<SubZone>().pointPositions.RemoveAt(i);

                                        if (serializedClass.activeSubZone.GetComponent<SubZone>().points.Count == 1) // handles the wrap around entry
                                        {
                                            serializedClass.activeSubZone.GetComponent<SubZone>().points.RemoveAt(0);
                                            serializedClass.activeSubZone.GetComponent<SubZone>().pointPositions.RemoveAt(0);
                                        }

                                        // handles deleting start / end points
                                        if (i == 0)
                                        {
                                            serializedClass.activeSubZone.GetComponent<SubZone>().points[serializedClass.activeSubZone.GetComponent<SubZone>().points.Count - 1] = serializedClass.activeSubZone.GetComponent<SubZone>().points[0];
                                            serializedClass.activeSubZone.GetComponent<SubZone>().pointPositions[serializedClass.activeSubZone.GetComponent<SubZone>().pointPositions.Count - 1] = serializedClass.activeSubZone.GetComponent<SubZone>().pointPositions[0];
                                        }
                                        else if (i == serializedClass.activeSubZone.GetComponent<SubZone>().points.Count)
                                        {
                                            serializedClass.activeSubZone.GetComponent<SubZone>().points[0] = serializedClass.activeSubZone.GetComponent<SubZone>().points[serializedClass.activeSubZone.GetComponent<SubZone>().points.Count - 1];
                                            serializedClass.activeSubZone.GetComponent<SubZone>().pointPositions[0] = serializedClass.activeSubZone.GetComponent<SubZone>().pointPositions[serializedClass.activeSubZone.GetComponent<SubZone>().points.Count - 1];
                                        }

                                        serializedClass.activeSubZone.GetComponent<PolygonCollider2D>().points = serializedClass.activeSubZone.GetComponent<SubZone>().pointPositions.To2DVectorArray();
                                        return;
                                    }

                                    reposPt = i;
                                }
                            }
                            if (reposPt == -1)
                            {
                                AddNewPoint();
                            }
                        }
                    }
                    else
                    {
                        AddNewPoint();
                    }
                }
            }

            //if (e.type == EventType.MouseDown && e.button == 1 && !CheckForMissingReferences())
            //{
            //    int total = ObjectsToSpawn();

            //    for (int i = 0; i < total; i++)
            //    {
            //        (Vector3, Vector3) pos;

            //        if (serializedClass.brushType == Brushes.DEFAULT) // default brush
            //        {
            //            pos = GenerateRandomSpawnPosition(raycastHit.point, Vector3.up, 1f);
            //        }
            //        else // faded brush
            //        {
            //            if (i < total / 3)
            //            {
            //                pos = GenerateRandomSpawnPosition(raycastHit.point, Vector3.up, 0.4f);
            //            }
            //            else if ( i < ((total / 3) * 2))
            //            {
            //                pos = GenerateRandomSpawnPosition(raycastHit.point, Vector3.up, 0.7f);
            //            }
            //            else
            //            {
            //                pos = GenerateRandomSpawnPosition(raycastHit.point, Vector3.up, 1f);
            //            }
            //        }

            //        if (pos.Item2 != Vector3.down)  // if valid ray cast
            //        {
            //            (GameObject, int, int) spawnData = GetOBJToSpawn();

            //            if (spawnData.Item1 != null)
            //            {
            //                GameObject obj = PrefabUtility.InstantiatePrefab(spawnData.Item1) as GameObject;
            //                SceneVisibilityManager.instance.DisablePicking(obj, false);

            //                obj.layer = LayerMask.NameToLayer("Ignore Raycast");

            //                //obj.GetComponent<Transform>().SetParent(serializedClass.groupParents[spawnData.Item2].transform, true);

            //                obj.GetComponent<Transform>().rotation = Quaternion.FromToRotation(obj.GetComponent<Transform>().up, pos.Item2);
            //                obj.GetComponent<Transform>().Rotate(pos.Item2, Random.Range(0f, 360f), Space.World);

            //                pos.Item1 += pos.Item2 * (obj.GetComponent<Renderer>().bounds.size.y / 2);

            //                obj.GetComponent<Transform>().position = pos.Item1 + (pos.Item2.normalized * serializedClass.groups[spawnData.Item2].items[spawnData.Item3].yOffset);
            //            }
            //        }
            //    }
            //}
        }

        HandleUtility.Repaint();
    }

    public void OnDrawHandles()
    {

    }

    private void RepositionPoint(ref RaycastHit raycastHit, int i)
    {
        Handles.color = Color.red;
        Handles.DrawSolidDisc(serializedClass.activeSubZone.GetComponent<SubZone>().points[i].point, serializedClass.activeSubZone.GetComponent<SubZone>().points[i].normal, raycastHit.distance * handleScale);

        if (i == 0)
        {
            int j = serializedClass.activeSubZone.GetComponent<SubZone>().points.Count - 1;
            serializedClass.activeSubZone.GetComponent<SubZone>().points[j] = raycastHit;
            serializedClass.activeSubZone.GetComponent<SubZone>().pointPositions[j] = raycastHit.point;
        }
        else if (i == serializedClass.activeSubZone.GetComponent<SubZone>().points.Count - 1)
        {
            serializedClass.activeSubZone.GetComponent<SubZone>().points[0] = raycastHit;
            serializedClass.activeSubZone.GetComponent<SubZone>().pointPositions[0] = raycastHit.point;
        }

        serializedClass.activeSubZone.GetComponent<SubZone>().points[i] = raycastHit;
        serializedClass.activeSubZone.GetComponent<SubZone>().pointPositions[i] = raycastHit.point;
        serializedClass.activeSubZone.GetComponent<PolygonCollider2D>().points = serializedClass.activeSubZone.GetComponent<SubZone>().pointPositions.To2DVectorArray();
    }

    private void AddNewPoint()
    {
        int count = serializedClass.activeSubZone.GetComponent<SubZone>().points.Count;

        if (count > 2)
        {
            Vector3 closestPt = Functions.GetClosestPoint(serializedClass.activeSubZone.GetComponent<SubZone>().pointPositions, raycastHit.point);
            bool added = false;

            int index = serializedClass.activeSubZone.GetComponent<SubZone>().pointPositions.IndexOf(closestPt);
            Vector3 pt1, pt2;

            if (index != 0 && index != count -1)
            {
                pt1 = serializedClass.activeSubZone.GetComponent<SubZone>().pointPositions[index - 1];
                pt2 = serializedClass.activeSubZone.GetComponent<SubZone>().pointPositions[index + 1];

                if (Mathf.Abs((Vector3.Distance(closestPt, raycastHit.point) + Vector3.Distance(raycastHit.point, pt1)) - Vector3.Distance(closestPt, pt1)) < 0.1f)
                {
                    serializedClass.activeSubZone.GetComponent<SubZone>().points.Insert(index, raycastHit);
                    serializedClass.activeSubZone.GetComponent<SubZone>().pointPositions.Insert(index, raycastHit.point);
                    added = true;
                }
                else if (Mathf.Abs((Vector3.Distance(closestPt, raycastHit.point) + Vector3.Distance(raycastHit.point, pt2)) - Vector3.Distance(closestPt, pt2)) < 0.1f)
                {
                    serializedClass.activeSubZone.GetComponent<SubZone>().points.Insert(index + 1, raycastHit);
                    serializedClass.activeSubZone.GetComponent<SubZone>().pointPositions.Insert(index + 1, raycastHit.point);
                    added = true;
                }
            }

            if (!added)
            {
                AddPointToEnd();
            }
        }
        else
        {
            serializedClass.activeSubZone.GetComponent<SubZone>().points.Add(raycastHit);
            serializedClass.activeSubZone.GetComponent<SubZone>().pointPositions.Add(raycastHit.point);

            serializedClass.activeSubZone.GetComponent<SubZone>().points.Add(serializedClass.activeSubZone.GetComponent<SubZone>().points[0]);
            serializedClass.activeSubZone.GetComponent<SubZone>().pointPositions.Add(serializedClass.activeSubZone.GetComponent<SubZone>().pointPositions[0]);
        }

        serializedClass.activeSubZone.GetComponent<PolygonCollider2D>().points = serializedClass.activeSubZone.GetComponent<SubZone>().pointPositions.To2DVectorArray();
    }

    private void AddPointToEnd()
    {
        serializedClass.activeSubZone.GetComponent<SubZone>().points.RemoveAt(serializedClass.activeSubZone.GetComponent<SubZone>().points.Count - 1);
        serializedClass.activeSubZone.GetComponent<SubZone>().pointPositions.RemoveAt(serializedClass.activeSubZone.GetComponent<SubZone>().pointPositions.Count - 1);

        serializedClass.activeSubZone.GetComponent<SubZone>().points.Add(raycastHit);
        serializedClass.activeSubZone.GetComponent<SubZone>().pointPositions.Add(raycastHit.point);

        serializedClass.activeSubZone.GetComponent<SubZone>().points.Add(serializedClass.activeSubZone.GetComponent<SubZone>().points[0]);
        serializedClass.activeSubZone.GetComponent<SubZone>().pointPositions.Add(serializedClass.activeSubZone.GetComponent<SubZone>().pointPositions[0]);
    }
}

#endif