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
            float yr = (a.y > b.y) ? a.y - b.y : b.y - a.y;

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
        public static bool CheckForMissingReferences(SavedPaletteScript palette)
        {
            bool output = false;

            if (palette.m_groups == null || palette.m_groups.Count == 0)
            {
                Debug.LogException(new System.Exception($"Tool Group(s) missing / empty!"));
                return true;
            }

            // check for missing references
            for (int i = 0; i < palette.m_groups.Count; i++)
            {
                if (palette.m_groups[i].items == null || palette.m_groups[i].items.Count == 0)
                {
                    Debug.LogException(new System.Exception($"Group Item(s) is missing / empty! {palette.m_groups[i].name}"));
                    output = true;
                }

                for (int j = 0; j < palette.m_groups[i].items.Count; j++)
                {
                    if (palette.m_groups[i].items[j].gObject == null)
                    {
                        Debug.LogException(new System.Exception($"Group Item(s) is missing an assigned Object! {palette.m_groups[i].name}, Object: {j + 1}"));
                        output = true;
                    }
                }
            }

            return output;
        }

        public static (Vector3, Vector3) GenerateRandomSpawnPosition(Vector3 hitPos, Vector3 surfaceNormal, float scalar, int ignore)
        {
            float CheckDiff = 20f;
            float randRadius = Mathf.Sqrt(Random.value) * scalar;
            float randomRotation = Random.Range(0f, 360f);

            float rad = randomRotation * Mathf.Deg2Rad;
            Vector3 randomPos = new Vector3(Mathf.Cos(rad) - Mathf.Sin(rad), 0, Mathf.Sin(rad) + Mathf.Cos(rad));
            randomPos.Normalize();
            randomPos *= randRadius;

            // use random pos to get new ray origin to then re calc ray along offset to get prefab specific normal
            RaycastHit newPosHit;
            Vector3 newOirigin = (hitPos + randomPos) + (surfaceNormal * CheckDiff);

            if (Physics.Raycast(newOirigin, -surfaceNormal, out newPosHit, 1000f))
            {
                if ((ignore & (1 << newPosHit.collider.gameObject.layer)) == 0)
                {
                    return (newPosHit.point, newPosHit.normal.normalized);
                }
                return (Vector3.zero, Vector3.down);
            }
            return (Vector3.zero, Vector3.down);
        }

        public static (GameObject, int, int) GetOBJToSpawn(SavedPaletteScript palette)
        {
            float rand = Random.Range(0f, 100f);
            float temp = 0;

            for (int i = 0; i < palette.m_groups.Count; i++)
            {
                if (i > 0)
                {
                    temp += palette.m_groups[i - 1].weight;
                }

                if (rand > temp && rand <= temp + palette.m_groups[i].weight)
                {
                    float rand2 = Random.Range(0f, 100f);
                    float temp2 = 0;

                    for (int j = 0; j < palette.m_groups[i].items.Count; j++)
                    {
                        if (j > 0)
                        {
                            temp2 += palette.m_groups[i].items[j - 1].weight;
                        }

                        if (rand2 > temp2 && rand2 <= temp2 + palette.m_groups[i].items[j].weight)
                        {
                            return (palette.m_groups[i].items[j].gObject, i, j);
                        }
                    }
                }
            }

            return (null, 0, 0);
        }

        public static Vector3 GetClosestPoint(List<Vector3> points, Vector3 point)
        {
            int closestIndex = -1;
            float closestDistance = Mathf.Infinity;

            for (int i = 0; i < points.Count; i++)
            {
                float distance = Vector3.Distance(point, points[i]);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestIndex = i;
                }
            }

            return points[closestIndex];
        }
    }
}
