using Entities;
using System.Threading;
using UnityAsync;
using UnityEngine;

namespace AI.Actions
{
    public class MoveTo : AIAction
    {
        Ship Self;
        public Vector3 Target;
        public float TargetFinalSpeed;

        public bool RotateToFinal;
        public Vector3 FinalHeading;
        public Vector3 FinalUpDirection;
        protected override async void Behaviour(CancellationToken token)
        {
            float distanceToTarget;
            Vector3 toTarget, heading;
            Collider shipCollider = Self.GetComponent<Collider>();

            while (!token.IsCancellationRequested)
            {
                toTarget = Target - Self.transform.position;
                distanceToTarget = toTarget.magnitude;
                toTarget = toTarget.UNormalized();
                //heading = Pathing.CollisionAvoidance.AvoidObstacles(toTarget, shipCollider, Self.MaxSpeed);
                heading = toTarget;

                Self.RotateTowards(heading, FinalUpDirection);

                float idealTurnSpeed = Self.GetIdealTurnSpeed();

                const float minAngleForSpeedReduction = 30;
                const float maxAngleForSpeedReduction = 90;

                float angleFromHeading = Vector3.Angle(Self.transform.forward, heading);
                float angularDistanceFactor = Mathf.Clamp01((angleFromHeading - minAngleForSpeedReduction) / maxAngleForSpeedReduction);
                float MaxSpeed = Mathf.Lerp(Self.MaxSpeed, idealTurnSpeed, angularDistanceFactor);
                float orientationFactor = 1;

                if (distanceToTarget < Self.Acceleration)
                {
                    float distanceFactor = 1 - distanceToTarget / Self.Acceleration;
                    float mappedOrientation = Mathf.Clamp01(Util.Map(Vector3.Angle(Self.transform.forward, heading), 5, 45, 1, 0));
                    orientationFactor = Mathf.Pow(mappedOrientation, 1 + (distanceFactor * 2));
                }

                //v^2 = u^2 + 2ax (Constant Acceleration formula)
                Self.TargetSpeed = Mathf.Clamp(Mathf.Sqrt((Mathf.Pow(TargetFinalSpeed, 2) + distanceToTarget * Self.Acceleration * 2) * orientationFactor), 0, MaxSpeed);

                //Debug.DrawRay(Self.transform.position, toTarget * 4f, Color.red);
                //Debug.DrawRay(Self.transform.position, heading * 3f, Color.green);

                if (distanceToTarget < 0.25f && Self.CurrentVelocity.sqrMagnitude <= TargetFinalSpeed + Self.Acceleration / 2f)
                {
                    Self.TargetSpeed = TargetFinalSpeed;
                    Self.TargetRotationSpeed = Vector3.zero;

                    bool isRotating = true;
                    while (isRotating)
                    {
                        if (RotateToFinal)
                        {
                            isRotating = !Self.RotateTowards(FinalHeading, FinalUpDirection);
                        }
                        else
                        {
                            isRotating = !Self.RotateTowards(Self.transform.forward, FinalUpDirection);
                        }
                        if (!isRotating)
                        {
                            break;
                        }
                        await Await.NextUpdate();
                    }

                    break;
                }

                await Await.NextUpdate();
            }
            Complete();
        }

        public MoveTo(Ship self, Vector3 target, float finalSpeed = 0)
        {
            Self = self;
            Target = target;
            RotateToFinal = false;
            FinalUpDirection = Vector3.up;
            TargetFinalSpeed = finalSpeed;
        }

        public MoveTo(Ship self, Vector3 target, Vector3 finalHeading, Vector3 upDirection, float finalSpeed = 0)
        {
            Self = self;
            Target = target;
            RotateToFinal = true;
            FinalHeading = finalHeading;
            FinalUpDirection = upDirection;
            TargetFinalSpeed = finalSpeed;
        }
    }
}
