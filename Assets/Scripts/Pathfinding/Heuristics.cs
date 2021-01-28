using UnityEngine;
using System.Collections.Generic;
namespace Pathfinding
{
    public static class Heuristics
    {
        public enum HeuristicType
        {
            Manhattan
        }
        public delegate float Heuristic(Vector3Int a, Vector3Int b);

        public static Dictionary<HeuristicType, Heuristic> HeuristicPairs = new Dictionary<HeuristicType, Heuristic>()
        {
            { HeuristicType.Manhattan, new Heuristic(Manhattan) }
        };

        
        private static float Manhattan(Vector3Int a, Vector3Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) + Mathf.Abs(a.z - b.z);
        }
        
    }
}
