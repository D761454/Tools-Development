using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
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
    public List<GroupStruct> groups = new List<GroupStruct>();

    public float brushSize = 50f;
    public int density = 50;
    public Brushes brushType = Brushes.DEFAULT;
    public int ignoreLayers = 4;
    public string paletteName = string.Empty;

    [SerializeField] public List<Zone> zoneTypes = new List<Zone>();
    [SerializeField] public bool showTutorial = true;
    public int activeZoneIndex = -1;

    public GameObject activeSubZone;

    protected void OnEnable()
    {
        brushSize = 50f;
        density = 50;
        brushType = Brushes.DEFAULT;
        ignoreLayers = 4;
        paletteName = string.Empty;
        groups.Clear();
        activeSubZone = null;
        activeZoneIndex = -1;
    }

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

    public void GenerateParent()
    {
        if (activeZoneIndex != -1)
        {
            var zone = zoneTypes[activeZoneIndex];
            if (zone.parentObject == null || !GameObject.Find($"{zone.name}-Parent"))
            {
                GameObject parentObj = new GameObject($"{zone.name}-Parent");
                parentObj.AddComponent<SceneZone>();
                parentObj.GetComponent<SceneZone>().SaveData(zone.name, zone.zonePalette, zone.zonePalette.m_id);
                zone.parentObject = parentObj;

                if (zone.uiColor.a == 0)
                {
                    zone.uiColor.a = 1;
                }
                parentObj.GetComponent<SceneZone>().m_uiColor = zone.uiColor;
                zoneTypes[activeZoneIndex] = zone;
                Undo.RegisterCreatedObjectUndo(parentObj, "Generated Parent");
            }
        }
    }

    public void GenerateSubZone()
    {
        if (activeZoneIndex != -1)
        {
            var zone = zoneTypes[activeZoneIndex];
            if (zone.parentObject == null || !GameObject.Find($"{zone.name}-Parent"))
            {
                GameObject parentObj = new GameObject($"{zone.name}-Parent");
                parentObj.AddComponent<SceneZone>();
                parentObj.GetComponent<SceneZone>().SaveData(zone.name, zone.zonePalette, zone.zonePalette.m_id);
                zone.parentObject = parentObj;

                if (zone.uiColor.a == 0)
                {
                    zone.uiColor.a = 1;
                }
                parentObj.GetComponent<SceneZone>().m_uiColor = zone.uiColor;
                zoneTypes[activeZoneIndex] = zone;
                Undo.RegisterCreatedObjectUndo(parentObj, "Generated Parent");
            }

            GameObject subZoneObj = new GameObject($"{zone.name}-SubZone" + ((zone.parentObject.GetComponentsInChildren<Transform>()).Length + 1));
            subZoneObj.AddComponent<SubZone>();
            subZoneObj.AddComponent<PolygonCollider2D>();
            subZoneObj.transform.parent = zone.parentObject.transform;
            activeSubZone = subZoneObj;
            Undo.RegisterCreatedObjectUndo(subZoneObj, "Generated Sub-Zone");
        }
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
    public GameObject parentObject;
    public Color uiColor;
}