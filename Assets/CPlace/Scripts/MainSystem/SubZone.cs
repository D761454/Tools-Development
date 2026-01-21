using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


public class SubZone : MonoBehaviour
{
    public List<Vector3> pointNormals = new List<Vector3>();
    public List<Vector3> pointPositions = new List<Vector3>();

    public void ClearPoints()
    {
        pointNormals.Clear();
        pointPositions.Clear();
    }
}
