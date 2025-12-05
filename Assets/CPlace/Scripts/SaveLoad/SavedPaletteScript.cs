using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class SavedPaletteScript : ScriptableObject
{
    [SerializeField] public List<GroupStruct> m_groups = new List<GroupStruct>();
    [SerializeField] public int m_density = 50;
    [SerializeField] public int m_ignoreLayers = 4;
    [SerializeField] public int m_id = 0;
    [SerializeField] public string m_paletteName = string.Empty;
}
