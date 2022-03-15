using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AI.Pathing
{
    public static class CollisionAvoidance
    {
        public static Vector3 AvoidObstacles(Vector3 heading, Collider self, float radius, params Collider[] ignore)
        {
            Collider[] obstacles = Physics.OverlapSphere(self.transform.position, radius);
            Vector3 newHeading = heading;


            Vector3 colClosest, selfClosest, avoidDirection;
            float distance, impact;
            List<Vector3> evalutated = new List<Vector3>();
            foreach(Collider collider in obstacles)
            {
                if (collider != self && !ignore.Contains(collider) && !ignore.Any(i => collider.transform.IsChildOf(i.transform)) && !collider.transform.IsChildOf(self.transform) && !evalutated.Contains(collider.transform.position))
                {
                    colClosest = collider.ClosestPointOnBounds(self.transform.position);
                    selfClosest = self.ClosestPointOnBounds(colClosest);
                    distance = Vector3.Distance(colClosest, selfClosest);

                    avoidDirection = (selfClosest - colClosest).UNormalized();
                    impact = (reciprocal(radius, distance) + sine(radius, distance)) / 2 * Vector3.Project(-avoidDirection, heading).magnitude;

                    newHeading += avoidDirection * impact;
                    evalutated.Add(collider.transform.position);
                    //Debug.Log($"Collider {collider.name} in range {distance} with impact {impact} and projection multiplier {Vector3.Project(-avoidDirection, heading).magnitude}");
                }
            }
            return newHeading.UNormalized();
        }

        public static float reciprocal(float radius, float distance)
        {
            return radius / distance - 1;
        }

        public static float sine(float radius, float distance)
        {
            return Mathf.Sin( (radius - distance) * Mathf.PI / (radius * 2)) * 10;
        }
    }
}
