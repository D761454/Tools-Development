using UnityEngine;
using Unity.Properties;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.TerrainTools;
using System;


/// <summary>
/// Hold Data for use on reopenening tool
/// </summary>
[Serializable, CreateAssetMenu(fileName = "OBJ Placer Scriptable OBJ", menuName = "Scriptable Objects/OBJ Placer Scriptable OBJ")]
public class OBJPlacerScriptableOBJ : ScriptableObject
{
    public float brushSize = 100f;
    public bool brushEnabled = false;
}

struct group
{
    public GameObject Object;

}
