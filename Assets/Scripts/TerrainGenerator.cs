using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

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
        

        return null;
    }
}
