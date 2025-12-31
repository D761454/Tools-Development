using UnityEngine;

public class SceneZone : MonoBehaviour
{
    public string m_zoneName = string.Empty;
    public SavedPaletteScript m_currentPalette;
    public SavedPaletteScript m_previousPalette;
    public int m_currentID;
    public int m_previousID;
    public Color m_uiColor = Color.cyan;

    public void UpdateToCurrent()
    {
        m_previousID = m_currentID;
        m_previousPalette = m_currentPalette;
    }
}
