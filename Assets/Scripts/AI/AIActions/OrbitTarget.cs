using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Entities;
using UnityAsync;
using AI.Pathing;
using Utility;

namespace AI.Actions
{
    public class OrbitTarget : AIAction
    {
        Ship Self;
        public Transform Target;
        Vector3 LocalOrientationToTarget;
        public float Radius;

        /// <summary>
        /// The distance from the orbit where orbiting stops and pathing takes over
        /// </summary>
        const float OrbitTolerance = 100;
        /// <summary>
        /// The distance from the orbit where pathing stops and orbiting takes over
        /// </summary>
        const float InnerTolerance = OrbitTolerance / 2;

        protected override async void Behaviour(CancellationToken token)
        {
            Vector3 toTarget;
            Collider shipCollider = Self.GetComponent<Collider>();
            Target.TryGetComponent<Collider>(out Collider targetCollider);
            float distanceToTarget;
            bool isLastErrorValid = false; //Was the last PID call in the last frame?

            UILineRenderer.UILine line = GameObject.Find("Canvas").GetComponentInChildren<UILineRenderer.UILine>();
            line.BezierControlPoints = new UILineRenderer.BezierPoint[600];
            for(int i = 0; i < line.BezierControlPoints.Length; i++)
            {
                line.BezierControlPoints[i] = new UILineRenderer.BezierPoint();
                line.BezierControlPoints[i].Position = new Vector2(i, 0);
            }
            PIDController pidController = new PIDController(1, 0.1f, 10, 0, line);
            while(!token.IsCancellationRequested && Self != null && Target != null)
            {
                toTarget = (Target.position - Self.transform.position);
                distanceToTarget = toTarget.magnitude;
                toTarget = toTarget.UNormalized();

                //The centripetal acceleration formula is a_c = v^2 / r, we rearrange in terms of v. The maximum a_c is equal to our ship's maximum acceleration value
                float maxOrbitVelocity = Mathf.Clamp(Mathf.Sqrt(Self.Acceleration * Radius), 0, Self.MaxSpeed);

                //TODO: Fix this (targetRoll up = orientedToTarget)

                if (distanceToTarget > Radius + OrbitTolerance)
                {
                    await MoveIntoOrbit(token, maxOrbitVelocity);
                    pidController.Reset();
                    isLastErrorValid = false;
                    continue;
                }
                else
                {

                    float pidError;
                    float currentError = Radius - distanceToTarget;

                    if (isLastErrorValid)
                    {
                        pidController.SetGain(Self.ProportionalGain, Self.IntegralGain, Self.DerivativeGain);
                        pidError = pidController.PID(currentError);
                    }
                    else
                    {
                        pidController.Reset();
                        pidError = pidController.PI(currentError);
                        isLastErrorValid = true;
                    }

                    Vector3 tangentDirection = Vector3.ProjectOnPlane(Self.transform.forward, toTarget).normalized;

                    Self.RotateInDirection(toTarget, -pidError);

                    Self.TargetSpeed = maxOrbitVelocity;

                    if (Self.RotationDelta.sqrMagnitude > 0)
                    {
                        Vector3 rotatedForwardDirection = Matrix4x4.Rotate(Quaternion.Euler(Self.RotationDelta.UNormalized())).MultiplyVector(Self.transform.forward);
                        float rotatedProjection = Util.ScalarProjection(rotatedForwardDirection, toTarget);
                        float currentProjection = Util.ScalarProjection(Self.transform.forward, toTarget);

                        bool isNewRotationAwayFromTarget = rotatedProjection < currentProjection;

                        if (isNewRotationAwayFromTarget)
                        {
                            pidController.Reset();
                            Debug.Log("PID was reset");
                        } 
                    }
                }
                
                await Await.NextUpdate();
            }
            Complete();
        }
        
        private async Task MoveIntoOrbit(CancellationToken token, float maxOrbitVelocity)
        {
            Vector3 toTarget = (Target.position - Self.transform.position);
            float distanceToTarget = toTarget.magnitude;

            MoveTo interceptOrbit = new MoveTo(Self, Vector3.zero, maxOrbitVelocity);
            bool hasMoveStarted = false;

            while (!token.IsCancellationRequested && distanceToTarget > Radius + InnerTolerance)
            {
                toTarget = (Target.position - Self.transform.position);
                distanceToTarget = toTarget.magnitude;
                toTarget = toTarget.UNormalized();

                Vector3 nonParallelUp = Vector3.Cross(Vector3.up, toTarget);
                if (nonParallelUp.sqrMagnitude == 0)
                {
                    nonParallelUp = Vector3.Cross(Vector3.right, toTarget);
                }
                nonParallelUp = nonParallelUp.UNormalized();

                float tangentAngle = Mathf.Asin(Radius / distanceToTarget);
                float tangentMagnitude = distanceToTarget * Mathf.Cos(tangentAngle);

                Vector3 radiusOrthogonal = Vector3.Cross(nonParallelUp, toTarget);

                Vector3 tangentDirection = Quaternion.AngleAxis(Util.Rad2Deg(tangentAngle), radiusOrthogonal) * toTarget;
                Vector3 intercept = Self.transform.position + (tangentDirection * tangentMagnitude);

                interceptOrbit.Target = intercept;

                Debug.DrawRay(Self.transform.position, tangentDirection * tangentMagnitude, Color.red);

                if (!hasMoveStarted)
                {
                    interceptOrbit.Start();
                }

                await Await.NextUpdate();
            }

            interceptOrbit.Stop();
        }

        public OrbitTarget(Ship self, Transform target, Vector3 localOrientationToTarget, float radius)
        {
            if (radius <= 0)
            {
                throw new ArgumentException($"Radius cannot be 0 or negative.");
            }
            Self = self;
            Target = target;
            Radius = radius;
            LocalOrientationToTarget = localOrientationToTarget;
        }
    }
}
