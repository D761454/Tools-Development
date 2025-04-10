#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;
using Unity.Android.Gradle;
using System.Collections.Generic;
using static UnityEditor.UIElements.CurveField;

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
        groups.selectionType = SelectionType.None;
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