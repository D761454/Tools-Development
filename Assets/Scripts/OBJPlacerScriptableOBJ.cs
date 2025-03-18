using UnityEngine;
using Unity.Properties;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.TerrainTools;
using System;

[Serializable]
public class OBJPlacerScriptableOBJ : ScriptableObject
{
    public float brushSize = 100f;
    public bool brushEnabled = true;

    [SerializeField] VisualTreeAsset visualTree;

    private void OnEnable()
    {
        hideFlags = HideFlags.HideAndDontSave;
    }

    public void CreateGUICustom(VisualElement mainRoot)
    {
        VisualElement root = new VisualElement();

        visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UXML/OBJPlacementMainEditor.uxml");
        visualTree.CloneTree(root);

        StyleSheet sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/USS/OBJPlacementMainEditor.uss");
        root.styleSheets.Add(sheet);
        mainRoot.Add(root);

        brushSize = root.Q<Slider>(name: "bSize").value;

        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.duringSceneGui += OnSceneGUI;
    }

    void OnSceneGUI(SceneView sceneView)
    {
        if (brushEnabled)
        {
            Vector3 mousePosition = Event.current.mousePosition;
            //mousePosition.y = sceneView.camera.pixelHeight - mousePosition.y;
            //mousePosition = sceneView.camera.ScreenToWorldPoint(mousePosition);
            //mousePosition.y = -mousePosition.y;

            Handles.BeginGUI();
            Handles.DrawWireDisc(mousePosition, Vector3.forward, brushSize);
            Handles.EndGUI();
        }
    }

    public void OnGUICustom()
    {

    }
}
