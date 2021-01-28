using Entities;
using System.Threading;
using UnityAsync;
using UnityEngine;

namespace AI.Actions
{
    public class MoveTo : AIAction
    {
        Ship Self;
        Vector3 Target;
        protected override async void Behaviour(CancellationToken token)
        {
            float targetSpeed, distanceToTarget;
            Vector3 toTarget, heading;
            Collider shipCollider = Self.GetComponent<Collider>();

            while (!token.IsCancellationRequested)
            {
                toTarget = Target - Self.transform.position;
                distanceToTarget = toTarget.magnitude;
                toTarget = toTarget.UNormalized();
                heading = Pathing.CollisionAvoidance.AvoidObstacles(toTarget, shipCollider, Self.MaxSpeed);

                Quaternion targetRotation = Quaternion.LookRotation(heading);
                Self.transform.rotation = Quaternion.RotateTowards(Self.transform.rotation, targetRotation, Self.RotationSpeed * Time.deltaTime);
                float speedMultiplier = Vector3.Project(heading, Self.transform.forward).magnitude;
                targetSpeed = Mathf.Clamp(Mathf.Sqrt(distanceToTarget * (Self.SpeedDelta * 0.9f) * 2) * Mathf.Clamp01(speedMultiplier), 0, Self.MaxSpeed);

                Debug.DrawRay(Self.transform.position, toTarget * 3f, Color.red);
                Debug.DrawRay(Self.transform.position, heading * 3f, Color.green);
                Self.CurrentSpeed += Mathf.Clamp(targetSpeed - Self.CurrentSpeed, -Self.SpeedDelta * Time.deltaTime, Self.SpeedDelta * Time.deltaTime);

                if (distanceToTarget < 0.25f && Self.CurrentSpeed <= 0.1f)
                {
                    Self.CurrentSpeed = 0;
                    break;
                }

                await Await.NextUpdate();
            }
            Complete();
        }

        public MoveTo(Ship self, Vector3 target)
        {
            Self = self;
            Target = target;
        }
    }
}
