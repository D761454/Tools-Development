using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Helpers
{
    public static class Functions
    {
        public static List<GameObject> GetAllObjectsWithComponent<T>() where T : Component
        {
            return (MonoBehaviour.FindObjectsByType<T>(FindObjectsSortMode.None) as GameObject[]).ToList();
        }

        public static List<T> GetAllWithComponent<T>() where T : Component
        {
            return MonoBehaviour.FindObjectsByType<T>(FindObjectsSortMode.None).ToList<T>();
        }

        public static Vector2[] To2DVectorArray(this List<Vector3> i)
        {
            List<Vector2> list = new List<Vector2>();

            foreach (var v in i)
            {
                Vector2 temp = new Vector2(v.x, v.z);
                list.Add(temp);
            }

            return list.ToArray();
        }
    }
}
