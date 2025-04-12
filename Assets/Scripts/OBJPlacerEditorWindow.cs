#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using TreeView = UnityEngine.UIElements.TreeView;

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
    /// scale all weights, keeping the same ratio and making them add up to 100
    /// </summary>
    void RegenWeights()
    {
        int total = 0;
        int[] gTotal = new int[serializedClass.groups.Count];
        for (int i= 0; i < serializedClass.groups.Count; i++)
        {
            total += serializedClass.groups[i].weight;
            for (int j = 0; j < serializedClass.groups[i].items.Count; j++)
            {
                gTotal[i] += serializedClass.groups[i].items[j].weight;
            }
        }

        for (int i = 0; i < serializedClass.groups.Count; i++)
        {
            serializedClass.groups[i].weight = Mathf.RoundToInt((serializedClass.groups[i].weight / (float)total) * 100);
            for (int j = 0; j < serializedClass.groups[i].items.Count; j++)
            {
                serializedClass.groups[i].items[j].weight = Mathf.RoundToInt((serializedClass.groups[i].items[j].weight / (float)gTotal[i]) * 100);
            }
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

        Func<VisualElement> makeGroup = () => MakeGroup();
        Func<VisualElement> makeItem = () => groupItemTree.Instantiate();

        VisualElement MakeGroup(){
            var temp = root.Q<ListView>().Query<Foldout>().ToList();

            // close all foldouts on making a new group
            for (int i = 1; i < temp.Count; i++)
            {
                temp[i].value = false;
            }
            
            return groupTree.Instantiate();
        }

        Action<VisualElement, int> bindItem = (element, index) =>
        {
            ObjectField objectField = element.Q<ObjectField>();
            objectField.dataSource = objectField.parent.dataSource;

            objectField.label = $"Object {index+1}:";
            objectField.SetBinding("value", new DataBinding() { dataSourcePath = new Unity.Properties.PropertyPath($"items[{index}].gObject"), bindingMode = BindingMode.TwoWay });
            objectField.Bind(serializedObject);

            SliderInt slider = element.Q<SliderInt>();
            slider.dataSource = slider.parent.dataSource;

            slider.SetBinding("value", new DataBinding() { dataSourcePath = new Unity.Properties.PropertyPath($"items[{index}].weight"), bindingMode = BindingMode.TwoWay });
            slider.Bind(serializedObject);
        };

        Action<VisualElement, int> bindGroup = (element, index) =>
        {
            // needed due to no struct intiialisation
            GroupStruct groupStruct = serializedClass.groups[index];
            groupStruct.items = new List<GroupItemStruct>();
            groupStruct.name = $"Group {index}";
            serializedClass.groups[index] = groupStruct;

            Foldout foldout = element.Q<Foldout>();
            foldout.dataSource = serializedClass;
            foldout.SetBinding("text", new DataBinding() { dataSourcePath = new Unity.Properties.PropertyPath($"groups[{index}].name"), bindingMode = BindingMode.TwoWay });

            TextField textField = element.Q<TextField>();
            textField.dataSource = serializedClass;
            textField.SetBinding("value", new DataBinding() { dataSourcePath = new Unity.Properties.PropertyPath($"groups[{index}].name"), bindingMode = BindingMode.TwoWay });
            textField.Bind(serializedObject);

            ListView listView = element.Q<ListView>();

            //https://discussions.unity.com/t/runtime-list-view-overflows-visible-area/783169/4

            listView.headerTitle = "Items:";
            listView.name = $"Group {index + 1} List";

            listView.makeItem = makeItem;
            listView.bindItem = bindItem;
            listView.itemsSource = serializedClass.groups[index].items;
            listView.dataSource = serializedClass.groups[index];
            listView.Bind(serializedObject);

            SliderInt slider = element.Q<SliderInt>();
            slider.dataSource = serializedClass;

            slider.SetBinding("value", new DataBinding() { dataSourcePath = new Unity.Properties.PropertyPath($"groups[{index}].weight"), bindingMode = BindingMode.TwoWay });
            slider.Bind(serializedObject);
        };

        var groups = root.Q<ListView>("GroupsList");
        groups.makeItem = makeGroup;
        groups.bindItem = bindGroup;
        groups.itemsSource = serializedClass.groups;

        root.Q<Button>().clicked += RegenWeights;

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