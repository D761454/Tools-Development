using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
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
    public string paletteName = string.Empty;

    [SerializeField] public List<Zone> zoneTypes = new List<Zone>();

    protected void OnDisable()
    {
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
    }

    public void SavePalette()
    {
        SavedPaletteScript paletteToSave;

        paletteToSave = AssetDatabase.LoadAssetAtPath<SavedPaletteScript>($"Assets/CPlace/Palettes/{paletteName}-Palette.asset");

        if (paletteToSave)
        {
            paletteToSave.m_id++;
        }
        else
        {
            paletteToSave = CreateInstance<SavedPaletteScript>();
            AssetDatabase.CreateAsset(paletteToSave, $"Assets/CPlace/Palettes/{paletteName}-Palette.asset");
        }

        // might need to place into both if editing values after creating asset does not work
        paletteToSave.m_density = density;

        if (groups.Count > 0)
        {
            List<GroupStruct> gs = new List<GroupStruct>();

            for (int i = 0; i < groups.Count; i++)
            {
                GroupStruct g = new GroupStruct();
                g.weight = groups[i].weight;
                g.name = groups[i].name;
                g.items = new List<GroupItemStruct>();

                for (int j = 0; j < groups[i].items.Count; j++)
                {
                    GroupItemStruct gi = new GroupItemStruct();
                    gi.weight = groups[i].items[j].weight;
                    gi.yOffset = groups[i].items[j].yOffset;
                    gi.gObject = groups[i].items[j].gObject;
                    g.items.Add(gi);
                }
                gs.Add(g);
            }

            paletteToSave.m_groups = gs;
        }
        else
        {
            paletteToSave.m_groups.Clear();
        }

        paletteToSave.m_ignoreLayers = ignoreLayers;
        paletteToSave.m_paletteName = paletteName;

        EditorUtility.SetDirty(paletteToSave);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public void ResetPalette()
    {
        brushSize = 50f;
        density = 50;
        brushType = Brushes.DEFAULT;
        ignoreLayers = 4;
        paletteName = string.Empty;

        int i = groups.Count;
        for (int j = 0; j < i; j++)
        {
            groups[j].items.Clear();
        }
        groups.Clear();

        groupParents.Clear();
    }

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
                GameObject[] parents = GetAllWithComponent<GroupParent>();
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
        temp.AddComponent<GroupParent>();
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

[Serializable]
public struct Zone
{
    public string name;
    public SavedPaletteScript zonePalette;
}