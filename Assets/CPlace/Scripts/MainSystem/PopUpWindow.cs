#if UNITY_EDITOR
using Helpers;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class PopUpWindow : EditorWindow
{
    [SerializeField] VisualTreeAsset visualTree;
    VisualElement root;

    static string content = string.Empty;
    static Action methodToDo;

    public static void Init(string title, string text, Action method)
    {
        PopUpWindow window = GetWindow<PopUpWindow>();
        methodToDo = method;
        content = text;
        window.titleContent = new GUIContent(title);
        window.minSize = new Vector2(400f, 100f);
        window.maxSize = new Vector2(401f, 101f);
    }

    /// <summary>
    /// generate GUI if no editor updates
    /// </summary>
    public void CreateGUI()
    {
        #region Set Up VisualTreeasset's
        root = new VisualElement();

        visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/CPlace/UXML/PopUpWindow.uxml");
        visualTree.CloneTree(root);

        StyleSheet sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/CPlace/USS/OBJPlacementMainEditor.uss");
        root.styleSheets.Add(sheet);
        #endregion

        #region Set Button events
        root.Q<Label>("text").text = content;
        root.Q<Button>("Y").clicked += Confirm;
        root.Q<Button>("N").clicked += this.Close;
        #endregion

        rootVisualElement.Add(root);
    }

    void Confirm()
    {
        methodToDo();
        this.Close();
    }
}

#endif
