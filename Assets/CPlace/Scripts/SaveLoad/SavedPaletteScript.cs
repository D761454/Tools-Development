using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class SavedPaletteScript : ScriptableObject
{
    public List<GroupStruct> m_groups = new List<GroupStruct>();
    public int m_density = 50;
    public int m_ignoreLayers = 4;
    public int m_id = 0;
    public string m_paletteName = string.Empty;
}
