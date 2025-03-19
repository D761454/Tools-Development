using UnityEngine;
using Unity.Properties;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.TerrainTools;
using System;
using System.Collections.Generic;


/// <summary>
/// Hold Data for use on reopenening tool
/// </summary>
[Serializable, CreateAssetMenu(fileName = "OBJ Placer Scriptable OBJ", menuName = "Scriptable Objects/OBJ Placer Scriptable OBJ")]
public class OBJPlacerScriptableOBJ : ScriptableObject
{
    public float brushSize = 50f;
    public bool brushEnabled = false;
    public int density = 50;
    public List<Group> groups = new List<Group>();
}

[Serializable]
public struct GroupItem
{
    public GameObject Object;
    public int weight;
}

[Serializable]
public struct Group
{
    public List<GroupItem> items;
    public int weight;

    public Group(List<GroupItem> items)
    {
        this.items = items;
        weight = 0;
    }
}
