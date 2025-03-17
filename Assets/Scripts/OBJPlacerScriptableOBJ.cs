using UnityEngine;
using Unity.Properties;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.TerrainTools;

[CreateAssetMenu]
public class OBJPlacerScriptableOBJ : ScriptableObject
{
    public float brushSize = 10f;
    public bool brushEnabled = true;

    [SerializeField] VisualTreeAsset visualTree;

    public void OnGUI(VisualElement mainRoot)
    {
        VisualElement root = new VisualElement();

        visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UXML/OBJPlacementMainEditor.uxml");
        visualTree.CloneTree(root);

        StyleSheet sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/USS/OBJPlacementMainEditor.uss");
        root.styleSheets.Add(sheet);
        mainRoot.Add(root);

        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.duringSceneGui += OnSceneGUI;
    }

    void OnSceneGUI(SceneView sceneView)
    {
        //https://discussions.unity.com/t/how-to-get-mouseposition-in-scene-view/519147

        if (brushEnabled)
        {
            Handles.BeginGUI();
            Handles.DrawWireDisc(Event.current.mousePosition, Vector3.forward, brushSize);
            Handles.EndGUI();
        }
    }
}
