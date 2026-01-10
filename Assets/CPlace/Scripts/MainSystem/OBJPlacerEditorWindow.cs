#if UNITY_EDITOR
using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
public static class IntExtensions
{
    public static string GroupWeight(this int i) => $"groups[{i}].weight";

    public static string GroupName(this int i) => $"groups[{i}].name";

    public static string ItemWeight(this int i) => $"items[{i}].weight";

    public static string ItemObj(this int i) => $"items[{i}].gObject";

    public static string ItemOffset(this int i) => $"items[{i}].yOffset";
}

public class OBJPlacerEditorWindow : EditorWindow
{
    private static OBJPlacerScriptableOBJ serializedClass;
    private SerializedObject serializedObject;

    static PopUpWindow pUp;

    [SerializeField] VisualTreeAsset visualTree;
    [SerializeField] VisualTreeAsset groupTree;
    [SerializeField] VisualTreeAsset groupItemTree;
    [SerializeField] VisualTreeAsset zoneTree;

    ListView zonesList;
    RaycastHit raycastHit;
    static List<SceneZone> outdatedZones = new List<SceneZone>();

    /// <summary>
    /// Access via menu and create window
    /// </summary>
    [MenuItem("Tools/CPlace _b")]
    public static void Init()
    {
        OBJPlacerEditorWindow window = GetWindow<OBJPlacerEditorWindow>();
        window.titleContent = new GUIContent("Placement Tool");

        if (serializedClass && serializedClass.showTutorial)
        {
            Tut1();
        }
    }

    /// <summary>
    /// Get/Create Serializable Obj for data storage
    /// </summary>
    [InitializeOnLoadMethod]
    private static void OnLoad()
    {
        serializedClass = AssetDatabase.LoadAssetAtPath<OBJPlacerScriptableOBJ>("Assets/CPlace/Scripts/SaveLoad/OBJ Placer Scriptable OBJ.asset");

        if (!serializedClass)
        {
            serializedClass = CreateInstance<OBJPlacerScriptableOBJ>();
            AssetDatabase.CreateAsset(serializedClass, "Assets/CPlace/Scripts/SaveLoad/OBJ Placer Scriptable OBJ.asset");
            AssetDatabase.Refresh();
        }

        //UnityEditor.SceneManagement.EditorSceneManager.sceneOpened += CheckPalettesForOutdatedData;
    }

    static void CheckPalettesForOutdatedData(UnityEngine.SceneManagement.Scene scene, UnityEditor.SceneManagement.OpenSceneMode mode)
    {
        outdatedZones.Clear();

        List<SceneZone> zones = Functions.GetAllWithComponent<SceneZone>();
        for (int i = 0; i < zones.Count; i++)
        {
            for (int j = 0; j < serializedClass.zoneTypes.Count; j++)
            {
                if (zones[i].m_zoneName == serializedClass.zoneTypes[j].name)
                {
                    if (zones[i].m_palette != serializedClass.zoneTypes[j].zonePalette || zones[i].m_ID != serializedClass.zoneTypes[j].zonePalette.m_id)
                    {
                        Debug.Log($"Outdated Zone Detected: {zones[i].m_zoneName}");
                        zones[i].m_tempPalette = serializedClass.zoneTypes[j].zonePalette;
                        zones[i].m_tempID = serializedClass.zoneTypes[j].zonePalette.m_id;
                        outdatedZones.Add(zones[i]);
                    }
                }
            }
        }

        if (outdatedZones.Count > 0)
        {
            OutdatedZonesConf();
        }
    }

    /// <summary>
    /// generate GUI if no editor updates
    /// </summary>
    public void CreateGUI()
    {
        #region Set Up VisualTreeasset's
        VisualElement root = new VisualElement();

        visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/CPlace/UXML/OBJPlacementMainEditor.uxml");
        visualTree.CloneTree(root);

        StyleSheet sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/CPlace/USS/OBJPlacementMainEditor.uss");
        root.styleSheets.Add(sheet);

        groupTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/CPlace/UXML/GroupUI.uxml");
        groupItemTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/CPlace/UXML/GroupItemUI.uxml");

        zoneTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/CPlace/UXML/ZoneUI.uxml");
        #endregion

        Func<VisualElement> makeGroup = () => groupTree.Instantiate();
        Func<VisualElement> makeItem = () => groupItemTree.Instantiate();
        Func<VisualElement> makeZone = () => zoneTree.Instantiate();

        // called on creating new item within a group
        Action<VisualElement, int> bindItem = (element, index) =>
        {
            ObjectField objectField = element.Q<ObjectField>();

            objectField.label = $"Object {index + 1}:";
        };

        // called on creating new group
        Action<VisualElement, int> bindGroup = (element, index) =>
        {
            #region Init Group Struct
            // needed due to no struct initialization
            GroupStruct groupStruct = serializedClass.groups[index];

            // items
            if (serializedClass.groups[index].items == null)
            {
                groupStruct.items = new List<GroupItemStruct>();
            }
            else
            {
                groupStruct.items = serializedClass.groups[index].items;
                groupStruct.weight = serializedClass.groups[index].weight;
            }

            // name
            if (serializedClass.groups[index].name == null || serializedClass.groups[index].name == "")
            {
                groupStruct.name = $"Group {index + 1}";
            }
            else
            {
                groupStruct.name = serializedClass.groups[index].name;
            }
            #endregion

            serializedClass.groups[index] = groupStruct;

            ListView listView = element.Q<ListView>();

            listView.name = $"Group {index + 1} List";

            listView.makeItem = makeItem;
            listView.bindItem = bindItem;
            listView.itemsSource = serializedClass.groups[index].items;
        };

        var groups = root.Q<ListView>("GroupsList");
        zonesList = root.Q<ListView>("ZoneTypeList");

        zonesList.selectionChanged += (items) =>
        {
            foreach (Zone item in items)
            {
                if (!serializedClass.zoneTypes.Contains(item))
                    continue;

                int i = serializedClass.zoneTypes.IndexOf(item);
                serializedClass.activeZoneIndex = i;

                Label zT = root.Q<Label>("zoneTitle");
                zT.dataSource = serializedClass;
                zT.SetBinding("text", new DataBinding() { dataSourcePath = new Unity.Properties.PropertyPath($"zoneTypes[{i}].name"), bindingMode = BindingMode.ToTarget });
                zT.Bind(serializedObject);

                ObjectField zP = root.Q<ObjectField>("zPal");
                zP.UnregisterValueChangedCallback(ChangePalette);
                zP.dataSource = serializedClass;
                zP.SetBinding("value", new DataBinding() { dataSourcePath = new Unity.Properties.PropertyPath($"zoneTypes[{i}].zonePalette"), bindingMode = BindingMode.TwoWay });
                zP.Bind(serializedObject);
                zP.RegisterValueChangedCallback(ChangePalette);

                GameObject par = GameObject.Find(serializedClass.zoneTypes[i].name + "-Parent");

                if (par)
                {
                    Zone t = serializedClass.zoneTypes[i];
                    t.parentObject = par;
                    serializedClass.zoneTypes[i] = t;
                }

                ObjectField sP = root.Q<ObjectField>("sPar");
                sP.dataSource = serializedClass;
                sP.SetBinding("value", new DataBinding() { dataSourcePath = new Unity.Properties.PropertyPath($"zoneTypes[{i}].parentObject"), bindingMode = BindingMode.TwoWay });
                sP.Bind(serializedObject);
            }
        };

        zonesList.itemsRemoved += (items) =>
        {
            serializedClass.activeZoneIndex = -1;
            Label zT = root.Q<Label>("zoneTitle");
            zT.ClearBinding("text");
            zT.text = "Zone Name";
            ObjectField zP = root.Q<ObjectField>("zPal");
            zP.ClearBinding("value");
            zP.value = null;
            ObjectField sP = root.Q<ObjectField>("sPar");
            sP.ClearBinding("value");
            sP.value = null;
            ObjectField aS = root.Q<ObjectField>("aSzone");
            aS.ClearBinding("value");
            aS.value = null;
        };

        zonesList.makeItem = makeZone;
        groups.makeItem = makeGroup;
        groups.bindItem = bindGroup;

        #region Load in Palette
        var pal = root.Q<ObjectField>("lPal");
        pal.RegisterValueChangedCallback(
            evt =>
            {
                SavedPaletteScript temp = (SavedPaletteScript)evt.newValue;
                if (temp != null)
                {
                    List<GroupStruct> gs = new List<GroupStruct>();

                    for (int i = 0; i < temp.m_groups.Count; i++)
                    {
                        GroupStruct g = new GroupStruct();
                        g.weight = temp.m_groups[i].weight;
                        g.name = temp.m_groups[i].name;
                        g.items = new List<GroupItemStruct>();

                        for (int j = 0; j < temp.m_groups[i].items.Count; j++)
                        {
                            GroupItemStruct gi = new GroupItemStruct();
                            gi.weight = temp.m_groups[i].items[j].weight;
                            gi.yOffset = temp.m_groups[i].items[j].yOffset;
                            gi.gObject = temp.m_groups[i].items[j].gObject;
                            g.items.Add(gi);
                        }
                        gs.Add(g);
                    }

                    serializedClass.groups = gs;

                    serializedClass.density = temp.m_density;
                    serializedClass.ignoreLayers = temp.m_ignoreLayers;
                    serializedClass.paletteName = temp.m_paletteName;
                    rootVisualElement.Q<ObjectField>("lPal").value = null;
                }
            });
        #endregion

        #region Set Button events
        root.Q<Button>("regenButton").clicked += serializedClass.RegenWeights;
        root.Q<Button>("saveButton").clicked += serializedClass.SavePalette;
        root.Q<Button>("nPar").clicked += serializedClass.GenerateParent;
        root.Q<Button>("nSzone").clicked += serializedClass.GenerateSubZone;
        root.Q<Button>("pAll").clicked += PaintAllConf;

        root.Q<Button>("pZone").clicked += PaintZoneTypeConf;
        root.Q<Button>("pActive").clicked += PaintActiveZoneConf;
        root.Q<Button>("cAll").clicked += ClearAllConf;
        root.Q<Button>("cAllOfType").clicked += ClearActiveTypeConf;
        root.Q<Button>("cActive").clicked += ClearActiveZoneConf;
        root.Q<Button>("cActivePts").clicked += ClearActiveZonePtsConf;

        Button btn = root.Q<Button>("resetButton");
        btn.clicked += ResetPaletteConf;
        btn.RegisterCallback((MouseOverEvent evt) => btn.style.backgroundColor = Color.red);
        btn.RegisterCallback((MouseLeaveEvent evt) => btn.style.backgroundColor = Color.red * 0.9f);
        #endregion

        rootVisualElement.Add(root);
    }

    private void ChangePalette(ChangeEvent<UnityEngine.Object> evt)
    {
        GameObject obj = GameObject.Find(serializedClass.zoneTypes[serializedClass.activeZoneIndex].name + "-Parent");
        if (obj)
        {
            obj.GetComponent<SceneZone>().m_palette = (SavedPaletteScript)evt.newValue;
        }
    }

    /// <summary>
    /// handle GUI events, Update Serializable Obj
    /// </summary>
    private void OnGUI()
    {
        if (serializedClass)
        {
            serializedObject = new SerializedObject(serializedClass);
            serializedObject.Update();
            serializedObject.ApplyModifiedProperties();
        }
    }

    #region button methods
    public void PaintAll()
    {
        List<SceneZone> zones = Functions.GetAllWithComponent<SceneZone>();
        Debug.Log("zone count: " + zones.Count);
        int density = 0, rows = 0, columns = 0;
        (Vector2, Vector2) minmax;
        List<PolygonCollider2D> nullZones = new List<PolygonCollider2D>();

        foreach (SceneZone zone in zones)
        {
            if (zone.m_palette == null)
            {
                List<SubZone> subZones = zone.gameObject.GetComponentsInChildren<SubZone>().ToList();
                foreach (SubZone subZone in subZones)
                {
                    nullZones.Add(subZone.GetComponent<PolygonCollider2D>());
                }
            }
        }

        foreach (SceneZone zone in zones)
        {
            if (!zone.m_palette)
            {
                continue;
            }

            density = zone.m_palette.m_density;

            List<SubZone> subZones = zone.gameObject.GetComponentsInChildren<SubZone>().ToList();
            foreach (SubZone subZone in subZones)
            {
                minmax = Functions.GetMinMax(subZone.pointPositions); // get the min and max position - 2D, x and z
                (float, float) distance = Functions.GetDistance(minmax.Item1, minmax.Item2); // get the distances on the x and y for the grid
                float yRayBase = subZone.pointPositions[0].y + 500f;

                // define the grid - at each grid intersection - spawn an object using density as chance, then apply an offset
                rows = (int)(distance.Item1 / (density / 10));
                columns = (int)(distance.Item2 / (density / 10));

                Undo.RecordObject(null, "Base Undo");
                int undoID = Undo.GetCurrentGroup();

                for (int i = 0; i < rows; i++)
                {
                    for (int j = 0; j < columns; j++)
                    {
                        bool breakout = false;
                        Vector3 origin = new Vector3(minmax.Item1.x + (i * (distance.Item1 / rows)), yRayBase, minmax.Item1.y + (j * (distance.Item2 / columns)));
                        Vector2 colliderOverlapCheck = new Vector2(raycastHit.point.x, raycastHit.point.z);
                        Physics.Raycast(origin, Vector3.down, out raycastHit, 1000f, ~zone.m_palette.m_ignoreLayers);

                        (Vector3, Vector3) pos;

                        #region Check if Hit is in zone and not in nullzone.
                        foreach (PolygonCollider2D collider in nullZones)
                        {
                            if (collider.OverlapPoint(colliderOverlapCheck))
                            {
                                breakout = true;
                            }
                        }

                        if (!subZone.GetComponent<PolygonCollider2D>().OverlapPoint(colliderOverlapCheck))
                        {
                            breakout = true;
                        }

                        if (breakout)
                        {
                            continue;
                        }
                        #endregion

                        if (Functions.CheckForMissingReferences(zone.m_palette))
                        {
                            break;
                        }

                        pos = Functions.GenerateRandomSpawnPosition(raycastHit.point, Vector3.up, 2f, zone.m_palette.m_ignoreLayers);

                        if (pos.Item2 == Vector3.down)
                        {
                            continue;
                        }

                        (GameObject, int, int) spawnData = Functions.GetOBJToSpawn(zone.m_palette);

                        if (spawnData.Item1 == null)
                        {
                            continue;
                        }

                        GameObject obj = PrefabUtility.InstantiatePrefab(spawnData.Item1) as GameObject;

                        obj.layer = LayerMask.NameToLayer("Ignore Raycast");

                        obj.GetComponent<Transform>().rotation = Quaternion.FromToRotation(obj.GetComponent<Transform>().up, pos.Item2);
                        obj.GetComponent<Transform>().Rotate(pos.Item2, UnityEngine.Random.Range(0f, 360f), Space.World);

                        pos.Item1 += pos.Item2 * (obj.GetComponent<Renderer>().bounds.size.y / 2);

                        obj.GetComponent<Transform>().position = pos.Item1 + (pos.Item2.normalized * zone.m_palette.m_groups[spawnData.Item2].items[spawnData.Item3].yOffset);
                        obj.GetComponent<Transform>().SetParent(subZone.gameObject.transform);

                        Undo.RegisterCreatedObjectUndo(obj, "New Undo");
                        Undo.CollapseUndoOperations(undoID);
                    }
                }
            }
        }
    }

    public void PaintActiveZone()
    {
        int density = 0, rows = 0, columns = 0;
        (Vector2, Vector2) minmax;
        List<PolygonCollider2D> nullZones = new List<PolygonCollider2D>();

        if (serializedClass.activeSubZone)
        {
            SubZone aZone = serializedClass.activeSubZone.GetComponent<SubZone>();
            List<SceneZone> zones = Functions.GetAllWithComponent<SceneZone>();

            foreach (SceneZone zone in zones)
            {
                if (zone.m_palette == null)
                {
                    List<SubZone> subZones = zone.gameObject.GetComponentsInChildren<SubZone>().ToList();
                    foreach (SubZone subZone in subZones)
                    {
                        nullZones.Add(subZone.GetComponent<PolygonCollider2D>());
                    }
                }
            }

            SceneZone par = aZone.GetComponentInParent<SceneZone>();

            if (par.m_palette)
            {
                density = par.m_palette.m_density;

                minmax = Functions.GetMinMax(aZone.pointPositions); // get the min and max position - 2D, x and z
                (float, float) distance = Functions.GetDistance(minmax.Item1, minmax.Item2); // get the distances on the x and y for the grid
                float yRayBase = aZone.pointPositions[0].y + 500f;

                // define the grid - at each grid intersection - spawn an object using density as chance, then apply an offset
                rows = (int)(distance.Item1 / (density / 10));
                columns = (int)(distance.Item2 / (density / 10));

                Undo.RecordObject(null, "Base Undo");
                int undoID = Undo.GetCurrentGroup();

                for (int i = 0; i < rows; i++)
                {
                    for (int j = 0; j < columns; j++)
                    {
                        bool breakout = false;
                        Vector3 origin = new Vector3(minmax.Item1.x + (i * (distance.Item1 / rows)), yRayBase, minmax.Item1.y + (j * (distance.Item2 / columns)));
                        Vector2 colliderOverlapCheck = new Vector2(raycastHit.point.x, raycastHit.point.z);
                        Physics.Raycast(origin, Vector3.down, out raycastHit, 1000f, ~par.m_palette.m_ignoreLayers);

                        (Vector3, Vector3) pos;

                        #region Check if Hit is in zone and not in nullzone.
                        foreach (PolygonCollider2D collider in nullZones)
                        {
                            if (collider.OverlapPoint(colliderOverlapCheck))
                            {
                                breakout = true;
                            }
                        }

                        if (!aZone.GetComponent<PolygonCollider2D>().OverlapPoint(colliderOverlapCheck))
                        {
                            breakout = true;
                        }

                        if (breakout)
                        {
                            continue;
                        }
                        #endregion

                        if (Functions.CheckForMissingReferences(par.m_palette))
                        {
                            break;
                        }

                        pos = Functions.GenerateRandomSpawnPosition(raycastHit.point, Vector3.up, 2f, par.m_palette.m_ignoreLayers);

                        if (pos.Item2 == Vector3.down)
                        {
                            continue;
                        }

                        (GameObject, int, int) spawnData = Functions.GetOBJToSpawn(par.m_palette);

                        if (spawnData.Item1 == null)
                        {
                            continue;
                        }

                        GameObject obj = PrefabUtility.InstantiatePrefab(spawnData.Item1) as GameObject;

                        obj.layer = LayerMask.NameToLayer("Ignore Raycast");

                        obj.GetComponent<Transform>().rotation = Quaternion.FromToRotation(obj.GetComponent<Transform>().up, pos.Item2);
                        obj.GetComponent<Transform>().Rotate(pos.Item2, UnityEngine.Random.Range(0f, 360f), Space.World);

                        pos.Item1 += pos.Item2 * (obj.GetComponent<Renderer>().bounds.size.y / 2);

                        obj.GetComponent<Transform>().position = pos.Item1 + (pos.Item2.normalized * par.m_palette.m_groups[spawnData.Item2].items[spawnData.Item3].yOffset);
                        obj.GetComponent<Transform>().SetParent(aZone.gameObject.transform);

                        Undo.RegisterCreatedObjectUndo(obj, "New Undo");
                        Undo.CollapseUndoOperations(undoID);
                    }
                }
            }
        }
    }

    // active zone is based on zone selected for editing - not active sub zone
    public void PaintActiveZoneType()
    {
        int density = 0, rows = 0, columns = 0;
        (Vector2, Vector2) minmax;
        List<PolygonCollider2D> nullZones = new List<PolygonCollider2D>();

        if (serializedClass.activeZoneIndex != -1)
        {
            SceneZone par = GameObject.Find(serializedClass.zoneTypes[serializedClass.activeZoneIndex].name + "-Parent").GetComponent<SceneZone>();
            List<SubZone> subZones = par.gameObject.GetComponentsInChildren<SubZone>().ToList();
            List<SceneZone> zones = Functions.GetAllWithComponent<SceneZone>();

            foreach (SceneZone zone in zones)
            {
                if (zone.m_palette == null)
                {
                    List<SubZone> sZs = zone.gameObject.GetComponentsInChildren<SubZone>().ToList();
                    foreach (SubZone subZone in sZs)
                    {
                        nullZones.Add(subZone.GetComponent<PolygonCollider2D>());
                    }
                }
            }

            if (par.m_palette)
            {
                density = par.m_palette.m_density;

                foreach (SubZone subZone in subZones)
                {
                    minmax = Functions.GetMinMax(subZone.pointPositions); // get the min and max position - 2D, x and z
                    (float, float) distance = Functions.GetDistance(minmax.Item1, minmax.Item2); // get the distances on the x and y for the grid
                    float yRayBase = subZone.pointPositions[0].y + 500f;

                    // define the grid - at each grid intersection - spawn an object using density as chance, then apply an offset
                    rows = (int)(distance.Item1 / (density / 10));
                    columns = (int)(distance.Item2 / (density / 10));

                    Undo.RecordObject(null, "Base Undo");
                    int undoID = Undo.GetCurrentGroup();

                    for (int i = 0; i < rows; i++)
                    {
                        for (int j = 0; j < columns; j++)
                        {
                            bool breakout = false;
                            Vector3 origin = new Vector3(minmax.Item1.x + (i * (distance.Item1 / rows)), yRayBase, minmax.Item1.y + (j * (distance.Item2 / columns)));
                            Physics.Raycast(origin, Vector3.down, out raycastHit, 1000f, ~par.m_palette.m_ignoreLayers);
                            Vector2 colliderOverlapCheck = new Vector2(raycastHit.point.x, raycastHit.point.z); // swapped with above line

                            (Vector3, Vector3) pos;

                            #region Check if Hit is in zone and not in nullzone.
                            foreach (PolygonCollider2D collider in nullZones)
                            {
                                if (collider.OverlapPoint(colliderOverlapCheck))
                                {
                                    breakout = true;
                                }
                            }

                            if (!subZone.GetComponent<PolygonCollider2D>().OverlapPoint(colliderOverlapCheck))
                            {
                                breakout = true;
                            }

                            if (breakout)
                            {
                                continue;
                            }
                            #endregion

                            if (Functions.CheckForMissingReferences(par.m_palette))
                            {
                                break;
                            }

                            pos = Functions.GenerateRandomSpawnPosition(raycastHit.point, Vector3.up, 2f, par.m_palette.m_ignoreLayers);

                            if (pos.Item2 == Vector3.down)
                            {
                                continue;
                            }

                            (GameObject, int, int) spawnData = Functions.GetOBJToSpawn(par.m_palette);

                            if (spawnData.Item1 == null)
                            {
                                continue;
                            }

                            GameObject obj = PrefabUtility.InstantiatePrefab(spawnData.Item1) as GameObject;

                            obj.layer = LayerMask.NameToLayer("Ignore Raycast");

                            obj.GetComponent<Transform>().rotation = Quaternion.FromToRotation(obj.GetComponent<Transform>().up, pos.Item2);
                            obj.GetComponent<Transform>().Rotate(pos.Item2, UnityEngine.Random.Range(0f, 360f), Space.World);

                            pos.Item1 += pos.Item2 * (obj.GetComponent<Renderer>().bounds.size.y / 2);

                            obj.GetComponent<Transform>().position = pos.Item1 + (pos.Item2.normalized * par.m_palette.m_groups[spawnData.Item2].items[spawnData.Item3].yOffset);
                            obj.GetComponent<Transform>().SetParent(subZone.gameObject.transform);

                            Undo.RegisterCreatedObjectUndo(obj, "New Undo");
                            Undo.CollapseUndoOperations(undoID);
                        }
                    }
                }
            }
        }
    }

    public void ClearAllObjects()
    {
        List<SceneZone> zones = Functions.GetAllWithComponent<SceneZone>();

        foreach (SceneZone zone in zones)
        {
            List<SubZone> subZones = zone.gameObject.GetComponentsInChildren<SubZone>().ToList();

            foreach (SubZone subZone in subZones)
            {
                List<Transform> objects = subZone.gameObject.GetComponentsInChildren<Transform>().ToList();

                for (int i = 0; i < objects.Count; i++)
                {
                    if (objects[i].gameObject == subZone.gameObject)
                    {
                        continue;
                    }
                    DestroyImmediate(objects[i].gameObject);
                }
            }
        }
    }

    // active zone is based on zone selected for editing - not active sub zone
    public void ClearAllOfActiveZoneType()
    {
        if (serializedClass.activeZoneIndex != -1)
        {
            SceneZone par = GameObject.Find(serializedClass.zoneTypes[serializedClass.activeZoneIndex].name + "-Parent").GetComponent<SceneZone>();

            List<SubZone> subZones = par.GetComponentsInChildren<SubZone>().ToList();

            foreach (SubZone subZone in subZones)
            {
                List<Transform> objects = subZone.gameObject.GetComponentsInChildren<Transform>().ToList();

                for (int i = 0; i < objects.Count; i++)
                {
                    if (objects[i].gameObject == subZone.gameObject)
                    {
                        continue;
                    }
                    DestroyImmediate(objects[i].gameObject);
                }
            }
        }
    }

    public void ClearActiveZone()
    {
        if (serializedClass.activeSubZone)
        {
            List<Transform> objects = serializedClass.activeSubZone.GetComponentsInChildren<Transform>().ToList();

            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i].gameObject == serializedClass.activeSubZone)
                {
                    continue;
                }
                DestroyImmediate(objects[i].gameObject);
            }
        }
    }

    public static void RegenerateZones()
    {
        #region Clear Outdated Zones
        for (int i = 0; i < outdatedZones.Count; i++)
        {
            List<SubZone> subZones = outdatedZones[i].GetComponentsInChildren<SubZone>().ToList();

            Debug.Log(subZones.Count);

            foreach (SubZone subZone in subZones)
            {
                List<Transform> objects = subZone.gameObject.GetComponentsInChildren<Transform>().ToList();

                for (int j = 0; j < objects.Count; j++)
                {
                    if (objects[j].gameObject == subZone.gameObject)
                    {
                        continue;
                    }
                    DestroyImmediate(objects[j].gameObject);
                }
            }
        }
        #endregion

        RaycastHit raycastHitS;

        #region Repaint Outdated Zones
        for (int i = 0; i < outdatedZones.Count; i++)
        {
            int density = 0, rows = 0, columns = 0;
            (Vector2, Vector2) minmax;
            List<PolygonCollider2D> nullZones = new List<PolygonCollider2D>();

            List<SubZone> subZones = outdatedZones[i].gameObject.GetComponentsInChildren<SubZone>().ToList();
            List<SceneZone> zones = Functions.GetAllWithComponent<SceneZone>();

            foreach (SceneZone zone in zones)
            {
                if (zone.m_tempPalette == null)
                {
                    List<SubZone> sZs = zone.gameObject.GetComponentsInChildren<SubZone>().ToList();
                    foreach (SubZone subZone in sZs)
                    {
                        nullZones.Add(subZone.GetComponent<PolygonCollider2D>());
                    }
                }
            }

            if (outdatedZones[i].m_tempPalette)
            {
                density = outdatedZones[i].m_tempPalette.m_density;

                foreach (SubZone subZone in subZones)
                {
                    minmax = Functions.GetMinMax(subZone.pointPositions); // get the min and max position - 2D, x and z
                    (float, float) distance = Functions.GetDistance(minmax.Item1, minmax.Item2); // get the distances on the x and y for the grid
                    float yRayBase = subZone.pointPositions[0].y + 500f;

                    // define the grid - at each grid intersection - spawn an object using density as chance, then apply an offset
                    rows = (int)(distance.Item1 / (density / 10));
                    columns = (int)(distance.Item2 / (density / 10));

                    for (int i2 = 0; i2 < rows; i2++)
                    {
                        for (int j = 0; j < columns; j++)
                        {
                            bool breakout = false;
                            Vector3 origin = new Vector3(minmax.Item1.x + (i2 * (distance.Item1 / rows)), yRayBase, minmax.Item1.y + (j * (distance.Item2 / columns)));
                            Physics.Raycast(origin, Vector3.down, out raycastHitS, 1000f, ~outdatedZones[i].m_tempPalette.m_ignoreLayers);
                            Vector2 colliderOverlapCheck = new Vector2(raycastHitS.point.x, raycastHitS.point.z);

                            (Vector3, Vector3) pos;

                            #region Check if Hit is in zone and not in nullzone.
                            foreach (PolygonCollider2D collider in nullZones)
                            {
                                if (collider.OverlapPoint(colliderOverlapCheck))
                                {
                                    breakout = true;
                                }
                            }

                            if (!subZone.GetComponent<PolygonCollider2D>().OverlapPoint(colliderOverlapCheck))
                            {
                                breakout = true;
                            }

                            if (breakout)
                            {
                                continue;
                            }
                            #endregion

                            if (Functions.CheckForMissingReferences(outdatedZones[i].m_tempPalette))
                            {
                                break;
                            }

                            pos = Functions.GenerateRandomSpawnPosition(raycastHitS.point, Vector3.up, 2f, outdatedZones[i].m_tempPalette.m_ignoreLayers);

                            if (pos.Item2 == Vector3.down)
                            {
                                continue;
                            }

                            (GameObject, int, int) spawnData = Functions.GetOBJToSpawn(outdatedZones[i].m_tempPalette);

                            if (spawnData.Item1 == null)
                            {
                                continue;
                            }

                            GameObject obj = PrefabUtility.InstantiatePrefab(spawnData.Item1) as GameObject;

                            obj.layer = LayerMask.NameToLayer("Ignore Raycast");

                            obj.GetComponent<Transform>().rotation = Quaternion.FromToRotation(obj.GetComponent<Transform>().up, pos.Item2);
                            obj.GetComponent<Transform>().Rotate(pos.Item2, UnityEngine.Random.Range(0f, 360f), Space.World);

                            pos.Item1 += pos.Item2 * (obj.GetComponent<Renderer>().bounds.size.y / 2);

                            obj.GetComponent<Transform>().position = pos.Item1 + (pos.Item2.normalized * outdatedZones[i].m_tempPalette.m_groups[spawnData.Item2].items[spawnData.Item3].yOffset);
                            obj.GetComponent<Transform>().SetParent(subZone.gameObject.transform);
                        }
                    }
                }
            }
        }
        #endregion

        for (int i = 0; i < outdatedZones.Count; i++)
        {
            outdatedZones[i].UpdateToCurrent();
        }
    }

    public void ClearZonePointsInActiveZone()
    {
        if (serializedClass.activeSubZone)
        {
            serializedClass.activeSubZone.GetComponent<SubZone>().pointPositions.Clear();
            // could cause issues - may need to just assign an empty vector array
            serializedClass.activeSubZone.GetComponent<PolygonCollider2D>().points = serializedClass.activeSubZone.GetComponent<SubZone>().pointPositions.To2DVectorArray();
            serializedClass.activeSubZone.GetComponent<SubZone>().ClearPoints();
        }
    }
    #endregion

    #region popup methods
    private void PaintAllConf()
    {
        PopUpWindow.Init(true, "Paint All Zones Confirmation.", "Are you sure you want to Paint ALL zones present within the Scene? \nDoing so will NOT delete any objects already placed within zones.", "Confirm", PaintAll);
    }

    private void PaintZoneTypeConf()
    {
        PopUpWindow.Init(true, $"Paint All Sub Zones of {serializedClass.zoneTypes[serializedClass.activeZoneIndex].name}.", $"Are you sure you want to Paint ALL Sub-Zones of Zone {serializedClass.zoneTypes[serializedClass.activeZoneIndex].name} present within the Scene? \nDoing so will NOT delete any objects already placed within related Sub-Zones.", "Confirm", PaintActiveZoneType);
    }

    private void PaintActiveZoneConf()
    {
        PopUpWindow.Init(true, "Paint Active Sub-Zone Confirmation.", $"Are you sure you want to Paint the Active Sub-Zone? \nDoing so will NOT delete any objects already placed within the zone.\nActive Sub-Zone: {serializedClass.activeSubZone.name}", "Confirm", PaintActiveZone);
    }

    private void ClearAllConf()
    {
        PopUpWindow.Init(true, "Clear All Zones Confirmation.", "Are you sure you want to Clear ALL zones present within the Scene?", "Confirm", ClearAllObjects);
    }

    private void ClearActiveTypeConf()
    {
        PopUpWindow.Init(true, $"Clear All Sub Zones of {serializedClass.zoneTypes[serializedClass.activeZoneIndex].name}.", $"Are you sure you want to Clear ALL Sub-Zones of Zone {serializedClass.zoneTypes[serializedClass.activeZoneIndex].name} present within the Scene?", "Confirm", ClearAllOfActiveZoneType);
    }

    private void ClearActiveZoneConf()
    {
        PopUpWindow.Init(true, "Clear Active Sub-Zone Confirmation.", $"Are you sure you want to Clear the Active Sub-Zone?\nActive Sub-Zone: {serializedClass.activeSubZone.name}", "Confirm", ClearActiveZone);
    }

    private void ClearActiveZonePtsConf()
    {
        PopUpWindow.Init(true, "Clear Active Sub-Zone Location Vertices Confirmation.", $"Are you sure you want to Clear ALL Location Vertices for the Active Sub-Zone? \nDoing so will require the replacement of zone bounds for the Sub-Zone to be used during Painting.\nActive Sub-Zone: {serializedClass.activeSubZone.name}", "Confirm", ClearZonePointsInActiveZone);
    }

    private void ResetPaletteConf()
    {
        PopUpWindow.Init(true, "Reset Palette Confirmation.", "Are you sure you want to Reset the current Palette? \nThis will clear all Groups and Items within the Palette.", "Confirm", serializedClass.ResetPalette, true, "Save and Reset", serializedClass.SavePalette, true);
    }

    private static void OutdatedZonesConf()
    {
        PopUpWindow.Init(true, "Zones Using Outdated Palette Data.", $"Some Zones present within the level have NOT been re-painted following changes to their associated Palettes.\nWould you like to automatically regenerate these Zones?", "Regenerate Zones", RegenerateZones);
    }

    #region Tutorial Popups
    private static void Tut1()
    {
        PopUpWindow.Init(false, "Welcome to CPlace!", "This tutorial will guide you through the basic functionality of CPlace.\n\nThis initial menu is used for setting up object palettes for later use to paint the world! Firstly, try naming your palette to something unique.", "Done!", Tut2, false);
        pUp = GetWindow<PopUpWindow>();
    }

    private static void Tut2()
    {
        pUp.EditData("Palette Walkthrough!", "Now that our palette has a unique identifier, lets try editing the slider.\n\nThe slider edits the Density, this is how many objects are placed within the associated zones for the palette.", "Done!", Tut3);
    }

    private static void Tut3()
    {
        pUp.EditData("Palette Walkthrough!", "Now, lets modify the layers objects from our palette can be placed on.\n\nThe multi-selection list outlines what layers will NOT be drawn on.", "Done!", Tut4);
    }

    private static void Tut4()
    {
        pUp.EditData("Palette Walkthrough!", "This is the most important part of any palette, the objects associated with it.\n\n" +
            "Here you can create a Groups for associated objects (e.g. trees), with a group name and an associated % chance to be placed when an object is placed from this palette.\n\n" +
            "Within these groups, we can assign objects with an associated % chance to be placed when the parent group is selected.\nThere is also a Y offset for objects with a non-centered pivot.", "Done!", Tut5);
    }

    private static void Tut5()
    {
        pUp.EditData("Palette Walkthrough!", "Now that we have a full palette, lets save it to use later.\n\nClick the save palette Button to save it to the Palettes Folder.", "Done!", Tut6);
    }

    private static void Tut6()
    {
        pUp.EditData("Zones Walkthrough!", "Now we have a saved palette that we can either reload into the palette editor or use for zones.\n\nLet's switch to the Zone tab to see the other half of the tool.", "Done!", Tut6);
    }
    #endregion

    #endregion
}

#endif