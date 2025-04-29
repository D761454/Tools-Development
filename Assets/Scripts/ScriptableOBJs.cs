using UnityEngine;
using System;
using System.Collections.Generic;


/// <summary>
/// Hold Data for use on reopenening tool
/// </summary>
[Serializable, CreateAssetMenu(fileName = "OBJ Placer Scriptable OBJ", menuName = "Scriptable Objects/OBJ Placer Scriptable OBJ")]
public class OBJPlacerScriptableOBJ : ScriptableObject
{
    public List<GameObject> groupParents = new List<GameObject>();
    public List<GroupStruct> groups = new List<GroupStruct>();

    public float brushSize = 50f;
    public int density = 50;

    /// <summary>
    /// scale all weights, keeping the same ratio and making them add up to 100
    /// </summary>
    public void RegenWeights()
    {
        float total = 0;
        float[] gTotal = new float[groups.Count];
        for (int i= 0; i < groups.Count; i++)
        {
            total += groups[i].weight;
            for (int j = 0; j < groups[i].items.Count; j++)
            {
                gTotal[i] += groups[i].items[j].weight;
            }
        }

        for (int i = 0; i < groups.Count; i++)
        {
            var group = groups[i];
            group.weight = ((groups[i].weight / (float)total) * 100);
            
            for (int j = 0; j < groups[i].items.Count; j++)
            {
                var item = group.items[j];
                item.weight = ((groups[i].items[j].weight / (float)gTotal[i]) * 100);
                group.items[j] = item;
            }

            groups[i] = group;
        }
    }

    /// <summary>
    /// generate objects in the scene to act as groups for instantiated prefabs to parent to
    /// </summary>
    public void GenerateSceneOBJGroups(){
        for (int i = 0; i < groups.Count; i++)
        {
            if (groupParents.Count <= i)
            {
                GameObject temp = new GameObject(groups[i].name);
                groupParents.Add(temp);
            }
            else if (groupParents[i] == null)
            {
                GameObject temp = new GameObject(groups[i].name);
                groupParents[i] = temp;
            }
            else
            {
                groupParents[i].name = groups[i].name;
            }
        }

        // fix to remove group 0

        if (groupParents.Count > groups.Count)
        {
            for (int i = groups.Count; i < groupParents.Count; i++)
            {
                DestroyImmediate(groupParents[i]);
            }
            groupParents.RemoveRange(groups.Count, groupParents.Count - groups.Count);
        }
    }
}

[Serializable]
public struct GroupItemStruct
{
    public GameObject gObject;
    public float weight;
}

[Serializable]
public struct GroupStruct
{
    public string name;
    public List<GroupItemStruct> items;
    public float weight;
}
