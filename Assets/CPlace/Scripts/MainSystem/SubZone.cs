using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


public class SubZone : MonoBehaviour
{
    public List<RaycastHit> points = new List<RaycastHit>();
    public List<Vector3> pointPositions = new List<Vector3>();

    /// <summary>
    /// temporary start method to clear points on start
    /// </summary>
    private void Start()
    {
        ClearPoints();
    }

    public void ClearPoints()
    {
        points.Clear();
        pointPositions.Clear();
    }
}
