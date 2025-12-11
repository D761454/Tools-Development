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
    }
}
