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

    [SerializeField] VisualTreeAsset visualTree;
    [SerializeField] VisualTreeAsset groupTree;
    [SerializeField] VisualTreeAsset groupItemTree;
    [SerializeField] VisualTreeAsset zoneTree;

    ListView zonesList;
    RaycastHit raycastHit;

    /// <summary>
    /// Access via menu and create window
    /// </summary>
    [MenuItem("Tools/CPlace _b")]
    public static void Init()
    {
        OBJPlacerEditorWindow window = GetWindow<OBJPlacerEditorWindow>();
        window.titleContent = new GUIContent("Placement Tool");
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
        btn.clicked += serializedClass.ResetPalette;
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
            obj.GetComponent<SceneZone>().m_currentPalette = (SavedPaletteScript)evt.newValue;
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
            if (zone.m_currentPalette == null)
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
            if (!zone.m_currentPalette)
            {
                continue;
            }

            density = zone.m_currentPalette.m_density;

            List<SubZone> subZones = zone.gameObject.GetComponentsInChildren<SubZone>().ToList();
            foreach (SubZone subZone in subZones)
            {
                minmax = Functions.GetMinMax(subZone.pointPositions); // get the min and max position - 2D, x and z
                (float, float) distance = Functions.GetDistance(minmax.Item1, minmax.Item2); // get the distances on the x and y for the grid
                float yRayBase = subZone.pointPositions[0].y + 500f;

                // define the grid - at each grid intersection - spawn an object using density as chance, then apply an offset
                rows = (int)(distance.Item1 / (density / 10));
                columns = (int)(distance.Item2 / (density / 10));

                for (int i = 0; i < rows; i++)
                {
                    for (int j = 0; j < columns; j++)
                    {
                        bool breakout = false;
                        Vector3 origin = new Vector3(minmax.Item1.x + (i * (distance.Item1 / rows)), yRayBase, minmax.Item1.y + (j * (distance.Item2 / columns)));
                        Vector2 colliderOverlapCheck = new Vector2(raycastHit.point.x, raycastHit.point.z);
                        Physics.Raycast(origin, Vector3.down, out raycastHit, 1000f, ~zone.m_currentPalette.m_ignoreLayers);

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

                        if (Functions.CheckForMissingReferences(zone.m_currentPalette))
                        {
                            break;
                        }

                        pos = Functions.GenerateRandomSpawnPosition(raycastHit.point, Vector3.up, 2f, zone.m_currentPalette.m_ignoreLayers);

                        if (pos.Item2 == Vector3.down)
                        {
                            continue;
                        }

                        (GameObject, int, int) spawnData = Functions.GetOBJToSpawn(zone.m_currentPalette);

                        if (spawnData.Item1 == null)
                        {
                            continue;
                        }

                        GameObject obj = PrefabUtility.InstantiatePrefab(spawnData.Item1) as GameObject;
                        SceneVisibilityManager.instance.DisablePicking(obj, false);

                        obj.layer = LayerMask.NameToLayer("Ignore Raycast");

                        obj.GetComponent<Transform>().rotation = Quaternion.FromToRotation(obj.GetComponent<Transform>().up, pos.Item2);
                        obj.GetComponent<Transform>().Rotate(pos.Item2, UnityEngine.Random.Range(0f, 360f), Space.World);

                        pos.Item1 += pos.Item2 * (obj.GetComponent<Renderer>().bounds.size.y / 2);

                        obj.GetComponent<Transform>().position = pos.Item1 + (pos.Item2.normalized * zone.m_currentPalette.m_groups[spawnData.Item2].items[spawnData.Item3].yOffset);
                        obj.GetComponent<Transform>().SetParent(subZone.gameObject.transform);
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
                if (zone.m_currentPalette == null)
                {
                    List<SubZone> subZones = zone.gameObject.GetComponentsInChildren<SubZone>().ToList();
                    foreach (SubZone subZone in subZones)
                    {
                        nullZones.Add(subZone.GetComponent<PolygonCollider2D>());
                    }
                }
            }

            SceneZone par = aZone.GetComponentInParent<SceneZone>();

            if (par.m_currentPalette)
            {
                density = par.m_currentPalette.m_density;

                minmax = Functions.GetMinMax(aZone.pointPositions); // get the min and max position - 2D, x and z
                (float, float) distance = Functions.GetDistance(minmax.Item1, minmax.Item2); // get the distances on the x and y for the grid
                float yRayBase = aZone.pointPositions[0].y + 500f;

                // define the grid - at each grid intersection - spawn an object using density as chance, then apply an offset
                rows = (int)(distance.Item1 / (density / 10));
                columns = (int)(distance.Item2 / (density / 10));

                for (int i = 0; i < rows; i++)
                {
                    for (int j = 0; j < columns; j++)
                    {
                        bool breakout = false;
                        Vector3 origin = new Vector3(minmax.Item1.x + (i * (distance.Item1 / rows)), yRayBase, minmax.Item1.y + (j * (distance.Item2 / columns)));
                        Vector2 colliderOverlapCheck = new Vector2(raycastHit.point.x, raycastHit.point.z);
                        Physics.Raycast(origin, Vector3.down, out raycastHit, 1000f, ~par.m_currentPalette.m_ignoreLayers);

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

                        if (Functions.CheckForMissingReferences(par.m_currentPalette))
                        {
                            break;
                        }

                        pos = Functions.GenerateRandomSpawnPosition(raycastHit.point, Vector3.up, 2f, par.m_currentPalette.m_ignoreLayers);

                        if (pos.Item2 == Vector3.down)
                        {
                            continue;
                        }

                        (GameObject, int, int) spawnData = Functions.GetOBJToSpawn(par.m_currentPalette);

                        if (spawnData.Item1 == null)
                        {
                            continue;
                        }

                        GameObject obj = PrefabUtility.InstantiatePrefab(spawnData.Item1) as GameObject;
                        SceneVisibilityManager.instance.DisablePicking(obj, false);

                        obj.layer = LayerMask.NameToLayer("Ignore Raycast");

                        obj.GetComponent<Transform>().rotation = Quaternion.FromToRotation(obj.GetComponent<Transform>().up, pos.Item2);
                        obj.GetComponent<Transform>().Rotate(pos.Item2, UnityEngine.Random.Range(0f, 360f), Space.World);

                        pos.Item1 += pos.Item2 * (obj.GetComponent<Renderer>().bounds.size.y / 2);

                        obj.GetComponent<Transform>().position = pos.Item1 + (pos.Item2.normalized * par.m_currentPalette.m_groups[spawnData.Item2].items[spawnData.Item3].yOffset);
                        obj.GetComponent<Transform>().SetParent(aZone.gameObject.transform);
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
                if (zone.m_currentPalette == null)
                {
                    List<SubZone> sZs = zone.gameObject.GetComponentsInChildren<SubZone>().ToList();
                    foreach (SubZone subZone in sZs)
                    {
                        nullZones.Add(subZone.GetComponent<PolygonCollider2D>());
                    }
                }
            }

            if (par.m_currentPalette)
            {
                density = par.m_currentPalette.m_density;

                foreach (SubZone subZone in subZones)
                {
                    minmax = Functions.GetMinMax(subZone.pointPositions); // get the min and max position - 2D, x and z
                    (float, float) distance = Functions.GetDistance(minmax.Item1, minmax.Item2); // get the distances on the x and y for the grid
                    float yRayBase = subZone.pointPositions[0].y + 500f;

                    // define the grid - at each grid intersection - spawn an object using density as chance, then apply an offset
                    rows = (int)(distance.Item1 / (density / 10));
                    columns = (int)(distance.Item2 / (density / 10));

                    for (int i = 0; i < rows; i++)
                    {
                        for (int j = 0; j < columns; j++)
                        {
                            bool breakout = false;
                            Vector3 origin = new Vector3(minmax.Item1.x + (i * (distance.Item1 / rows)), yRayBase, minmax.Item1.y + (j * (distance.Item2 / columns)));
                            Vector2 colliderOverlapCheck = new Vector2(raycastHit.point.x, raycastHit.point.z);
                            Physics.Raycast(origin, Vector3.down, out raycastHit, 1000f, ~par.m_currentPalette.m_ignoreLayers);

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

                            if (Functions.CheckForMissingReferences(par.m_currentPalette))
                            {
                                break;
                            }

                            pos = Functions.GenerateRandomSpawnPosition(raycastHit.point, Vector3.up, 2f, par.m_currentPalette.m_ignoreLayers);

                            if (pos.Item2 == Vector3.down)
                            {
                                continue;
                            }

                            (GameObject, int, int) spawnData = Functions.GetOBJToSpawn(par.m_currentPalette);

                            if (spawnData.Item1 == null)
                            {
                                continue;
                            }

                            GameObject obj = PrefabUtility.InstantiatePrefab(spawnData.Item1) as GameObject;
                            SceneVisibilityManager.instance.DisablePicking(obj, false);

                            obj.layer = LayerMask.NameToLayer("Ignore Raycast");

                            obj.GetComponent<Transform>().rotation = Quaternion.FromToRotation(obj.GetComponent<Transform>().up, pos.Item2);
                            obj.GetComponent<Transform>().Rotate(pos.Item2, UnityEngine.Random.Range(0f, 360f), Space.World);

                            pos.Item1 += pos.Item2 * (obj.GetComponent<Renderer>().bounds.size.y / 2);

                            obj.GetComponent<Transform>().position = pos.Item1 + (pos.Item2.normalized * par.m_currentPalette.m_groups[spawnData.Item2].items[spawnData.Item3].yOffset);
                            obj.GetComponent<Transform>().SetParent(subZone.gameObject.transform);
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

    public void ClearAllOfActiveZoneType()
    {
        if (serializedClass.activeSubZone)
        {
            GameObject par = serializedClass.activeSubZone.GetComponentInParent<SceneZone>().gameObject;

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
        PopUpWindow.Init("Paint All Zones Confirmation.", "Are you sure you want to Paint ALL zones present within the Scene? \nDoing so will NOT delete any objects already placed within zones.", PaintAll);
    }

    private void PaintZoneTypeConf()
    {
        PopUpWindow.Init($"Paint All Sub Zones of {serializedClass.zoneTypes[serializedClass.activeZoneIndex].name}.", $"Are you sure you want to Paint ALL Sub-Zones of Zone {serializedClass.zoneTypes[serializedClass.activeZoneIndex].name} present within the Scene? \nDoing so will NOT delete any objects already placed within related Sub-Zones.", PaintActiveZoneType);
    }

    private void PaintActiveZoneConf()
    {
        PopUpWindow.Init("Paint Active Sub-Zone Confirmation.", $"Are you sure you want to Paint the Active Sub-Zone? \nDoing so will NOT delete any objects already placed within the zone.\nActive Sub-Zone: {serializedClass.activeSubZone.name}", PaintActiveZone);
    }

    private void ClearAllConf()
    {
        PopUpWindow.Init("Clear All Zones Confirmation.", "Are you sure you want to Clear ALL zones present within the Scene?", ClearAllObjects);
    }

    private void ClearActiveTypeConf()
    {
        PopUpWindow.Init($"Clear All Sub Zones of {serializedClass.zoneTypes[serializedClass.activeZoneIndex].name}.", $"Are you sure you want to Clear ALL Sub-Zones of Zone {serializedClass.zoneTypes[serializedClass.activeZoneIndex].name} present within the Scene?", ClearAllOfActiveZoneType);
    }

    private void ClearActiveZoneConf()
    {
        PopUpWindow.Init("Clear Active Sub-Zone Confirmation.", $"Are you sure you want to Clear the Active Sub-Zone?\nActive Sub-Zone: {serializedClass.activeSubZone.name}", ClearActiveZone);
    }

    private void ClearActiveZonePtsConf()
    {
        PopUpWindow.Init("Clear Active Sub-Zone Location Vertices Confirmation.", $"Are you sure you want to Clear ALL Location Vertices for the Active Sub-Zone? \nDoing so will require the replacement of zone bounds for the Sub-Zone to be used during Painting.\nActive Sub-Zone: {serializedClass.activeSubZone.name}", ClearZonePointsInActiveZone);
    }

    #endregion
}

#endif