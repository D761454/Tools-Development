#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;
using Unity.Android.Gradle;
using System.Collections.Generic;

public class OBJPlacerEditorWindow : EditorWindow
{
    private static OBJPlacerScriptableOBJ serializedClass;
    private SerializedObject serializedObject;

    [SerializeField] VisualTreeAsset visualTree;
    [SerializeField] VisualTreeAsset groupTree;
    [SerializeField] VisualTreeAsset groupItemTree;

    /// <summary>
    /// Access via menu and create window
    /// </summary>
    [MenuItem("OBJ Placement/Placement Tool Editor _b")]
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
        serializedClass = AssetDatabase.LoadAssetAtPath<OBJPlacerScriptableOBJ>("Assets/Scripts/OBJ Placer Scriptable OBJ.asset");

        if (!serializedClass)
        {
            serializedClass = CreateInstance<OBJPlacerScriptableOBJ>();
            AssetDatabase.CreateAsset(serializedClass, "Assets/Scripts/OBJ Placer Scriptable OBJ.asset");
            AssetDatabase.Refresh();
        }
    }

    /// <summary>
    /// generate GUI if no editor updates
    /// </summary>
    public void CreateGUI()
    {
        VisualElement root = new VisualElement();

        visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UXML/OBJPlacementMainEditor.uxml");
        visualTree.CloneTree(root);

        StyleSheet sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/USS/OBJPlacementMainEditor.uss");
        root.styleSheets.Add(sheet);

        groupTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UXML/GroupUI.uxml");
        groupItemTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UXML/GroupItemUI.uxml");

        Func<VisualElement> makeGroup = () => groupTree.Instantiate();
        Func<VisualElement> makeItem = () => groupItemTree.Instantiate();

        void BindItem(VisualElement element, int index, int group)
        {
            // needed due to no struct intiialisation
            GroupItemStruct groupItem = serializedClass.groups[group].items[index];
            groupItem.gObject = null;
            serializedClass.groups[group].items[index] = groupItem;

            ObjectField objectField = element.Q<ObjectField>();
            objectField.dataSource = serializedClass;

            objectField.label = $"Object {index+1}:";
            objectField.objectType = typeof(GameObject);
            objectField.SetBinding("value", new DataBinding() { dataSourcePath = new Unity.Properties.PropertyPath($"groups[{group}].items[{index}].gObject") });
            objectField.Bind(serializedObject);

            SliderInt slider = element.Q<SliderInt>();
            slider.dataSource = serializedClass;

            slider.fill = true;
            slider.label = "Weight:";
            slider.showInputField = true;
            slider.highValue = 100;
            slider.SetBinding("value", new DataBinding() { dataSourcePath = new Unity.Properties.PropertyPath($"groups[{group}].items[{index}].weight") });
            slider.Bind(serializedObject);
        };

        Action<VisualElement, int> bindGroup = (element, index) =>
        {
            // needed due to no struct intiialisation
            GroupStruct groupStruct = serializedClass.groups[index];
            groupStruct.items = new List<GroupItemStruct>();
            serializedClass.groups[index] = groupStruct;

            Foldout foldout = element.Q<Foldout>();
            foldout.dataSource = serializedClass;
            foldout.SetBinding("text", new DataBinding() { dataSourcePath = new Unity.Properties.PropertyPath($"groups[{index}].name") });

            TextField textField = element.Q<TextField>();
            textField.dataSource = serializedClass;
            textField.SetBinding("value", new DataBinding() { dataSourcePath = new Unity.Properties.PropertyPath($"groups[{index}].name") });
            textField.Bind(serializedObject);

            ListView listView = element.Q<ListView>();
            listView.showBorder = true;
            listView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            listView.showAddRemoveFooter = true;
            listView.allowAdd = true;
            listView.allowRemove = true;
            listView.reorderMode = ListViewReorderMode.Animated;
            listView.showBoundCollectionSize = true;
            listView.headerTitle = "Items:";
            listView.name = $"Group {index + 1} List";

            int group = index;

            listView.makeItem = makeItem;
            listView.bindItem = (element, index) => { BindItem(element, index, group); };
            listView.itemsSource = serializedClass.groups[index].items;
            listView.Bind(serializedObject);

            SliderInt slider = element.Q<SliderInt>();
            slider.dataSource = serializedClass;

            slider.fill = true;
            slider.label = "Weight:";
            slider.showInputField = true;
            slider.highValue = 100;
            slider.SetBinding("value", new DataBinding() { dataSourcePath = new Unity.Properties.PropertyPath($"groups[{index}].weight") });
            slider.Bind(serializedObject);
        };

        var groups = root.Q<ListView>("GroupsList");
        groups.makeItem = makeGroup;
        groups.bindItem = bindGroup;
        groups.itemsSource = serializedClass.groups;

        rootVisualElement.Add(root);
    }

    /// <summary>
    /// handle gui events, Update Serializable Obj
    /// </summary>
    private void OnGUI()
    {
        serializedObject = new SerializedObject(serializedClass);
        serializedObject.Update();

        serializedObject.ApplyModifiedProperties();
    }
}

#endif