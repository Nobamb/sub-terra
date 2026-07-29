using System.Collections.Generic;
using UnityEngine;

namespace SubTerra.Gameplay.Drone
{
    public static class DroneContextCalculator
    {
        public static int CalculateDepth(float surfaceY, float playerY) => Mathf.Max(0, Mathf.RoundToInt(surfaceY - playerY));

        public static float FindNearestDistance(Vector2 origin, IEnumerable<Transform> bases)
        {
            float nearest = float.PositiveInfinity;
            if (bases == null) return nearest;
            foreach (Transform candidate in bases)
            {
                if (candidate != null) nearest = Mathf.Min(nearest, Vector2.Distance(origin, candidate.position));
            }
            return nearest;
        }
    }
}
