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

        public static (float, float) GetDistance(Vector2 a, Vector2 b)
        {
            float xr = (a.x > b.x) ? a.x - b.x : b.x - a.x;
            float yr = (a.y > b.y) ? a.y - b.y : b.y - a.x;

            return (xr, yr);
        }

        public static (Vector2, Vector2) GetMinMax(List<Vector3> pos)
        {
            Vector2 min = new Vector2(pos[0].x, pos[0].z), max = new Vector2(pos[0].x, pos[0].z);

            foreach (Vector3 v in pos)
            {
                if (v.x < min.x)
                {
                    min.x = v.x;
                }
                else if (v.x > max.x)
                {
                    max.x = v.x;
                }
                if (v.z < min.y)
                {
                    min.y = v.z;
                }
                else if (v.z > max.y)
                {
                    max.y = v.z;
                }
            }

            return (min, max);
        }
    }
}
