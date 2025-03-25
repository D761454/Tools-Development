using UnityEngine;
using System;
using System.Collections.Generic;


/// <summary>
/// Hold Data for use on reopenening tool
/// </summary>
[Serializable, CreateAssetMenu(fileName = "OBJ Placer Scriptable OBJ", menuName = "Scriptable Objects/OBJ Placer Scriptable OBJ")]
public class OBJPlacerScriptableOBJ : ScriptableObject
{
    public List<GroupStruct> groups = new List<GroupStruct>();

    public ToolData serializableData = new ToolData(50f, true, 50, 50);
}

[Serializable]
public struct GroupItemStruct
{
    public UnityEngine.Object gObject;
    public int weight;
}

[Serializable]
public struct GroupStruct
{
    public List<GroupItemStruct> items;
    public int weight;

    public GroupStruct(List<GroupItemStruct> items)
    {
        this.items = items;
        weight = 0;
    }
}

[Serializable]
public struct ToolData
{
    public float brushSize;
    public bool brushEnabled;
    public int density;
    public GameObject tempObj;
    public int tempWeight;

    public ToolData(float size, bool enabled, int density, int weight)
    {
        brushSize = size;
        brushEnabled = enabled;
        this.density = density;
        tempObj = null;
        tempWeight = weight;
    }
}
