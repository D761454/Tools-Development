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
    private VisualElement root;

    [SerializeField] VisualTreeAsset visualTree;
    [SerializeField] VisualTreeAsset groupTree;
    [SerializeField] VisualTreeAsset groupItemTree;

    /// <summary>
    /// Access via menu and create window
    /// </summary>
    [MenuItem("OBJ Placement/Placement Tool Editor _b")]
    public static void Init()
    {
        Debug.Log("init");
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
        Debug.Log("create gui");

        if (serializedClass.root == null)
        {
            #region Set Up VisualTreeasset's
            root = new VisualElement();

            visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/CPlace/UXML/OBJPlacementMainEditor.uxml");
            visualTree.CloneTree(root);

            StyleSheet sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/CPlace/USS/OBJPlacementMainEditor.uss");
            root.styleSheets.Add(sheet);

            groupTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/CPlace/UXML/GroupUI.uxml");
            groupItemTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/CPlace/UXML/GroupItemUI.uxml");
            #endregion

            Func<VisualElement> makeGroup = () => groupTree.Instantiate();
            Func<VisualElement> makeItem = () => groupItemTree.Instantiate();

            // called on creating new item within a group
            Action<VisualElement, int> bindItem = (element, index) =>
            {
                ObjectField objectField = element.Q<ObjectField>();
                objectField.dataSource = objectField.parent.dataSource;

                objectField.label = $"Object {index + 1}:";
                objectField.SetBinding("value", new DataBinding() { dataSourcePath = new Unity.Properties.PropertyPath(index.ItemObj()), bindingMode = BindingMode.TwoWay });
                objectField.Bind(serializedObject);

                Slider slider = element.Q<Slider>("Weight");
                slider.dataSource = slider.parent.dataSource;

                slider.SetBinding("value", new DataBinding() { dataSourcePath = new Unity.Properties.PropertyPath(index.ItemWeight()), bindingMode = BindingMode.TwoWay });
                slider.Bind(serializedObject);

                Slider slider2 = element.Q<Slider>("YOffset");
                slider2.dataSource = slider2.parent.dataSource;

                slider2.SetBinding("value", new DataBinding() { dataSourcePath = new Unity.Properties.PropertyPath(index.ItemOffset()), bindingMode = BindingMode.TwoWay });
                slider2.Bind(serializedObject);
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

                Foldout foldout = element.Q<Foldout>();
                foldout.dataSource = serializedClass;
                foldout.SetBinding("text", new DataBinding() { dataSourcePath = new Unity.Properties.PropertyPath(index.GroupName()), bindingMode = BindingMode.TwoWay });

                TextField textField = element.Q<TextField>();
                textField.dataSource = serializedClass;
                textField.SetBinding("value", new DataBinding() { dataSourcePath = new Unity.Properties.PropertyPath(index.GroupName()), bindingMode = BindingMode.TwoWay });
                textField.Bind(serializedObject);

                ListView listView = element.Q<ListView>();

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
        else
        {
            Debug.Log("loading saved root");
            rootVisualElement.Add(serializedClass.root);
        }
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

    private void OnDestroy()
    {
        //if (serializedClass.root != root)
        //{
        //    Debug.Log("updating saved root");
        //    serializedClass.root = root;
        //}
    }
}

#endif