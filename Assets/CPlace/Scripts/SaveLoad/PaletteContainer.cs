using UnityEngine;

/// <summary>
/// holds data for use on save and loading in the tool
/// </summary>
public class PaletteContainer : MonoBehaviour
{
    private int m_id;

    public void SetID(int id)
    {
        m_id = id;
    }
}
