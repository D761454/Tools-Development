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

    public float brushSize = 50f;
    public int density = 50;
    public GameObject tempObj = null;
    public int tempWeight = 50;
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
}
