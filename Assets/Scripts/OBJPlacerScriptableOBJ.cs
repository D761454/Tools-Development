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
    public float brushSize = 100f;
    public bool brushEnabled = false;
    public int density = 50;
    public List<Group> groups = new List<Group>();
}

[Serializable]
public class GroupItem
{
    public GameObject Object;
    public int weight;

    public GroupItem()
    {
        Object = null;
        weight = 0;
    }

    public GroupItem(GameObject obj, int w)
    {
        Object = obj;
        weight = w;
    }
}

[Serializable]
public class Group
{
    public List<GroupItem> items;
    public int weight;

    public Group()
    {
        items = new List<GroupItem>();
        weight = 0;
    }

    public Group(int w)
    {
        items = new List<GroupItem>();
        weight = w;
    }
}
