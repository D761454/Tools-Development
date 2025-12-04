#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;
using System.Collections.Generic;

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

        //zoneTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/CPlace/UXML/ZoneUI.uxml");
        #endregion

        Func<VisualElement> makeGroup = () => groupTree.Instantiate();
        Func<VisualElement> makeItem = () => groupItemTree.Instantiate();
        //Func<VisualElement> makeZone = () => zoneTree.Instantiate();

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
            // needed due to no struct intiialisation
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
        //zonesList = root.Q<ListView>("ZoneTypeList");
        //zonesList.itemsSource = serializedClass.zoneTypes;

        //zonesList.makeItem = makeZone;

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
        Button btn = root.Q<Button>("resetButton");
        btn.clicked += () =>
        {
            serializedClass.ResetPalette();

        };
        btn.RegisterCallback((MouseOverEvent evt) => btn.style.backgroundColor = Color.red);
        btn.RegisterCallback((MouseLeaveEvent evt) => btn.style.backgroundColor = Color.red * 0.9f);
        #endregion

        rootVisualElement.Add(root);
    }

    /// <summary>
    /// handle gui events, Update Serializable Obj
    /// </summary>
    private void OnGUI()
    {
        if (serializedClass)
        {
            serializedObject = new SerializedObject(serializedClass);
            serializedClass.GenerateSceneOBJGroups();
            serializedObject.Update();
            serializedObject.ApplyModifiedProperties();
        }
    }
}

#endif