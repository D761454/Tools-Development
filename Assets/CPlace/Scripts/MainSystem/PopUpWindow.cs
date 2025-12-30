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
    static string firstButton = string.Empty;
    static string secondButton = string.Empty;
    static Action methodToDo = null;
    static Action methodToDo2 = null;
    static bool cancelable = true;
    static bool doBothTasks = false;

    public static void Init(string title, string text, string firstButtonText, Action method, bool cancel = true, string secondButtonText = "NULL", Action method2 = null, bool doBothMethods = false)
    {
        methodToDo = method;
        methodToDo2 = method2;
        doBothTasks = doBothMethods;
        content = text;
        cancelable = cancel;
        firstButton = firstButtonText;
        secondButton = secondButtonText;

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
        y.text = firstButton;
        y.RegisterCallback((MouseOverEvent evt) => {
            y.style.backgroundColor = Color.cornflowerBlue;
            });
        y.RegisterCallback((MouseLeaveEvent evt) => {
            y.style.backgroundColor = new Color(0.33f, 0.33f, 0.33f);
            });

        Button y2 = root.Q<Button>("Y2");
        if (secondButton != "NULL")
        {
            y2.visible = true;
            y2.PlaceInFront(y);
            y2.text = secondButton;
            y2.clicked += Confirm2;
            y2.RegisterCallback((MouseOverEvent evt) => {
                y2.style.backgroundColor = Color.cornflowerBlue;
            });
            y2.RegisterCallback((MouseLeaveEvent evt) => {
                y2.style.backgroundColor = new Color(0.33f, 0.33f, 0.33f);
            });
        }
        else
        {
            y2.visible = false;
            y2.PlaceBehind(y);
        }

        Button n = root.Q<Button>("N");

        if (cancelable)
        {
            n.visible = true;
            n.clicked += this.Close;
            n.RegisterCallback((MouseOverEvent evt) => {
                n.style.backgroundColor = Color.cornflowerBlue;
            });
            n.RegisterCallback((MouseLeaveEvent evt) => {
                n.style.backgroundColor = new Color(0.33f, 0.33f, 0.33f);
            });
        }
        else
        {
            n.visible = false;
            n.PlaceBehind(y2);
        }


        #endregion

        rootVisualElement.Add(root);
    }

    void Confirm()
    {
        methodToDo();
        this.Close();
    }

    void Confirm2()
    {
        if (doBothTasks)
        {
            methodToDo2();
        }

        methodToDo();
        this.Close();
    }
}

#endif
