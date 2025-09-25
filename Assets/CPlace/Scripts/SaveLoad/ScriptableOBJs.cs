using UnityEngine;
using System;
using System.Collections.Generic;
public enum Brushes
{
    DEFAULT,
    FADED
}

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
    public Brushes brushType = Brushes.DEFAULT;
    public int ignoreLayers = 4;

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

        float totalGroup = 0, totalItem = 0;

        for (int i = 0; i < groups.Count; i++)
        {
            var group = groups[i];
            if ( i == groups.Count-1)
            {
                group.weight = 100f - totalGroup;
            }
            else
            {
                if (groups[i].weight == 0)
                {
                    group.weight = 0f;
                }
                else
                {
                    group.weight = ((groups[i].weight / (float)total) * 100);
                }
                
                totalGroup += group.weight;
            }
            
            for (int j = 0; j < groups[i].items.Count; j++)
            {
                var item = group.items[j];

                if (j == groups[i].items.Count-1)
                {
                    item.weight = 100f - totalItem;
                }
                else
                {
                    if (group.items[j].weight == 0)
                    {
                        item.weight = 0f;
                    }
                    else
                    {
                        item.weight = ((groups[i].items[j].weight / (float)gTotal[i]) * 100);
                    }
                    
                    totalItem += item.weight;
                }

                group.items[j] = item;
            }

            groups[i] = group;
        }
    }

    /// <summary>
    /// generate objects in the scene to act as groups for instantiated prefabs to parent to
    /// </summary>
    public void GenerateSceneOBJGroups()
    {
        for (int i = 0; i < groups.Count; i++)
        {
            if (groupParents.Count <= i) // if more groups than group parents, make new group parent
            {
                GenerateNewParent(i);
            }
            else if (groupParents[i] == null)
            {
                GameObject[] parents = GetAllWithComponent<PaletteContainer>();
                bool exists = false;
                
                for (int j = 0; j < parents.Length; j++)
                {
                    if (parents[j].name == groups[i].name)
                    {
                        groupParents[i] = parents[j];
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    GenerateNewParent(i);
                }
            }
            else // update name
            {
                groupParents[i].name = groups[i].name;
            }
        }
    }

    private GameObject[] GetAllWithComponent<T>() where T : Component
    {
        return FindObjectsByType<T>(FindObjectsSortMode.None) as GameObject[];
    }

    private void GenerateNewParent(int id)
    {
        GameObject temp = new GameObject(groups[id].name);
        temp.AddComponent<PaletteContainer>();
        temp.GetComponent<PaletteContainer>().SetID(id);
        groupParents.Add(temp);
    }
}

[Serializable]
public struct GroupItemStruct
{
    public GameObject gObject;
    public float weight;
    public float yOffset;
}

[Serializable]
public struct GroupStruct
{
    public string name;
    public List<GroupItemStruct> items;
    public float weight;
}
