using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using Unity.VisualScripting;

public class TerrainGenerator : MonoBehaviour
{
    [SerializeField] private int genWidth;
    [SerializeField] private int genHeight;
}

[CustomEditor(typeof(TerrainGenerator)), CanEditMultipleObjects]
public class TerrainGeneratorEditor : Editor
{
    public override VisualElement CreateInspectorGUI()
    {
        // load UXML
        VisualElement root = new VisualElement();

        VisualTreeAsset asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UXML/terrainGeneratorEditor.uxml");
        asset.CloneTree(root);

        return root;
    }
}
