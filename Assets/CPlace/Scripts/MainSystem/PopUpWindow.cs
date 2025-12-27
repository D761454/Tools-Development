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
    static bool cancelable = true;

    public static void Init(string title, string text, Action method, bool cancel = true)
    {
        methodToDo = method;
        content = text;
        cancelable = cancel;
        PopUpWindow window = GetWindow<PopUpWindow>(title);
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
        Button y = root.Q<Button>("Y");
        y.clicked += Confirm;
        y.RegisterCallback((MouseOverEvent evt) => {
            y.style.backgroundColor = Color.cornflowerBlue;
            });
        y.RegisterCallback((MouseLeaveEvent evt) => {
            y.style.backgroundColor = new Color(0.33f, 0.33f, 0.33f);
            });

        Button n = root.Q<Button>("N");
        n.visible = cancelable;
        n.clicked += this.Close;
        n.RegisterCallback((MouseOverEvent evt) => {
            n.style.backgroundColor = Color.cornflowerBlue;
        });
        n.RegisterCallback((MouseLeaveEvent evt) => {
            n.style.backgroundColor = new Color(0.33f, 0.33f, 0.33f);
        });
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
