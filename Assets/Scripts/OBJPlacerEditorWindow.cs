#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;

public static class IntExtensions
{
    public static string GroupWeight(this int i) => $"groups[{i}].weight";

    public static string GroupName(this int i) => $"groups[{i}].name";

    public static string ItemWeight(this int i) => $"items[{i}].weight";

    public static string ItemObj(this int i) => $"items[{i}].gObject";
}

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

        Action<VisualElement, int> bindItem = (element, index) =>
        {
            ObjectField objectField = element.Q<ObjectField>();
            objectField.dataSource = objectField.parent.dataSource;

            objectField.label = $"Object {index+1}:";
            objectField.SetBinding("value", new DataBinding() { dataSourcePath = new Unity.Properties.PropertyPath(index.ItemObj()), bindingMode = BindingMode.TwoWay });
            objectField.Bind(serializedObject);

            Slider slider = element.Q<Slider>();
            slider.dataSource = slider.parent.dataSource;

            slider.SetBinding("value", new DataBinding() { dataSourcePath = new Unity.Properties.PropertyPath(index.ItemWeight()), bindingMode = BindingMode.TwoWay });
            slider.Bind(serializedObject);
        };

        Action<VisualElement, int> bindGroup = (element, index) =>
        {
            // needed due to no struct intiialisation
            GroupStruct groupStruct = serializedClass.groups[index];

            if (serializedClass.groups[index].items == null){
                groupStruct.items = new List<GroupItemStruct>();
            }
            else
            {
                groupStruct.items = serializedClass.groups[index].items;
            }

            if (serializedClass.groups[index].name == null || serializedClass.groups[index].name == "")
            {
                groupStruct.name = $"Group {index + 1}";
            }
            else
            {
                groupStruct.name = serializedClass.groups[index].name;
            }
            
            serializedClass.groups[index] = groupStruct;

            Foldout foldout = element.Q<Foldout>();
            foldout.dataSource = serializedClass;
            foldout.SetBinding("text", new DataBinding() { dataSourcePath = new Unity.Properties.PropertyPath(index.GroupName()), bindingMode = BindingMode.TwoWay });

            TextField textField = element.Q<TextField>();
            textField.dataSource = serializedClass;
            textField.SetBinding("value", new DataBinding() { dataSourcePath = new Unity.Properties.PropertyPath(index.GroupName()), bindingMode = BindingMode.TwoWay });
            textField.Bind(serializedObject);

            ListView listView = element.Q<ListView>();

            listView.headerTitle = "Items:";
            listView.name = $"Group {index + 1} List";

            listView.makeItem = makeItem;
            listView.bindItem = bindItem;
            listView.itemsSource = serializedClass.groups[index].items;
            listView.dataSource = serializedClass.groups[index];
            listView.Bind(serializedObject);

            Slider slider = element.Q<Slider>();
            slider.dataSource = serializedClass;

            slider.SetBinding("value", new DataBinding() { dataSourcePath = new Unity.Properties.PropertyPath(index.GroupWeight()), bindingMode = BindingMode.TwoWay });
            slider.Bind(serializedObject);
        };

        var groups = root.Q<ListView>("GroupsList");

        groups.makeItem = makeGroup;
        groups.bindItem = bindGroup;
        groups.itemsSource = serializedClass.groups;

        root.Q<Button>("regenButton").clicked += serializedClass.RegenWeights;

        var toggle = root.Q<ToggleButtonGroup>();

        toggle.Add(new Button() { text = "Default", tooltip = "Default Brush." });
        toggle.Add(new Button() { text = "Faded", tooltip = "Faded Brush." });

        /*toggle.dataSource = serializedClass;
        toggle.SetBinding("value", new DataBinding() { dataSourcePath = new Unity.Properties.PropertyPath(brushType), bindingMode = BindingMode.TwoWay });
        toggle.Bind(serializedObject);*/

        rootVisualElement.Add(root);
    }

    /// <summary>
    /// handle gui events, Update Serializable Obj
    /// </summary>
    private void OnGUI()
    {
        serializedObject = new SerializedObject(serializedClass);
        serializedClass.GenerateSceneOBJGroups();
        serializedObject.Update();

        serializedObject.ApplyModifiedProperties();
    }
}

#endif