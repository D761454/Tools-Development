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
    static bool delete = true;

    public static void Init(bool deleteWindow, string title, string text, string firstButtonText, Action method, bool cancel = true, string secondButtonText = "NULL", Action method2 = null, bool doBothMethods = false)
    {
        methodToDo = method;
        methodToDo2 = method2;
        doBothTasks = doBothMethods;
        content = text;
        cancelable = cancel;
        firstButton = firstButtonText;
        secondButton = secondButtonText;
        delete = deleteWindow;

        PopUpWindow window = GetWindow<PopUpWindow>(title);
        window.minSize = new Vector2(500f, 250f);
        window.maxSize = new Vector2(501f, 251f);
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

        UpdateUI();

        rootVisualElement.Add(root);
    }

    public void UpdateUI()
    {
        #region Set Button events
        root.Q<Label>("text").text = content;
        Button y = root.Q<Button>("Y");
        y.clicked -= Confirm;
        y.clicked -= NoDelConfirm;

        if (delete)
        {
            y.clicked += Confirm;
        }
        else
        {
            y.clicked += NoDelConfirm;
        }

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
    }

    void Confirm()
    {
        methodToDo();
        this.Close();
    }

    void NoDelConfirm()
    {
        methodToDo();
    }

    public void EditData(string title, string text, string firstButtonText, Action method)
    {
        methodToDo = null;

        if (method == null)
        {
            methodToDo = () => { this.Close(); };
            delete = true;
        }
        else
        {
            methodToDo = method;
        }
        content = text;
        firstButton = firstButtonText;
        this.titleContent = new GUIContent(title);
        UpdateUI();
    }

    void Confirm2()
    {
        methodToDo2();

        if (doBothTasks)
        {
            methodToDo();
        }

        this.Close();
    }
}

#endif
