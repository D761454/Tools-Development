using UnityEngine;

public class SceneZone : MonoBehaviour
{
    public string m_zoneName = string.Empty;
    public SavedPaletteScript m_palette;
    public int m_ID;

    public SavedPaletteScript m_tempPalette;
    public int m_tempID;

    public Color m_uiColor = Color.cyan;

    public void SaveData(string name, SavedPaletteScript palette, int id)
    {
        m_zoneName = name;
        m_palette = palette;
        m_ID = id;
        m_tempPalette = palette;
        m_tempID = id;
    }

    public void UpdateToCurrent()
    {
        m_palette = m_tempPalette;
        m_ID = m_tempID;
    }
}
