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

namespace AI.Actions
{
    public class OrbitTarget : AIAction
    {
        Ship Self;
        public Transform Target;
        Vector3 LocalOrientationToTarget;
        public float Radius;

        protected override async void Behaviour(CancellationToken token)
        {
            Vector3 toTarget, /*orientedToTarget,*/ targetPerpendicular, modifiedTrajectory;
            Quaternion /*rotationToOrientation,*/ targetRotation, result, targetRoll;
            Collider shipCollider = Self.GetComponent<Collider>();
            Target.TryGetComponent<Collider>(out Collider targetCollider);
            float targetMod, rotationDeltaFactor, targetSpeed, distanceToTarget;
            while(!token.IsCancellationRequested && Self != null && Target != null)
            {
                toTarget = (Target.position - Self.transform.position);
                distanceToTarget = toTarget.magnitude;
                toTarget = toTarget.UNormalized();
                //TODO: Fix this (targetRoll up = orientedToTarget)
                //rotationToOrientation = Quaternion.FromToRotation(Self.transform.localToWorldMatrix.MultiplyVector(LocalOrientationToTarget), toTarget);
                //orientedToTarget = rotationToOrientation * toTarget;
                
                targetPerpendicular = new Vector2(toTarget.x, toTarget.y).Perpendicular();
                targetMod = Util.Map(Vector3.Distance(Self.transform.position, Target.position), Radius * 0.8f, Radius * 1.2f, -1, 1f);
                modifiedTrajectory = (toTarget * targetMod + targetPerpendicular).UNormalized();
                modifiedTrajectory = CollisionAvoidance.AvoidObstacles(modifiedTrajectory, shipCollider, distanceToTarget > Mathf.Max(Radius * 1.25f, Self.MaxSpeed) ? Self.MaxSpeed : Radius * 0.75f, targetCollider);

                //Calculate rotation and roll seperately, as they're governed by different speeds
                targetRotation = Quaternion.LookRotation(modifiedTrajectory, Self.transform.up);
                result = Quaternion.RotateTowards(Self.transform.rotation, targetRotation, Self.RotationSpeed * Time.deltaTime);
                targetRoll = Quaternion.LookRotation(result * Vector3.forward, toTarget);

                //The fraction of the targetRotation achieved according to the rotation speed;
                rotationDeltaFactor = Self.RotationSpeed * Time.deltaTime / Mathf.Max(Quaternion.Angle(Self.transform.rotation, targetRotation), 1);
                Self.transform.rotation = Quaternion.RotateTowards(result, targetRoll, Self.RotationSpeed * Time.deltaTime);

                //TargetSpeed adjusts for low rotationDeltaFactors - hopefully fixes the orbit at the cost of speed
                targetSpeed = Mathf.Clamp(Self.MaxSpeed * rotationDeltaFactor, 0, Self.MaxSpeed);
                Self.CurrentSpeed += Mathf.Clamp(targetSpeed - Self.CurrentSpeed, -Self.SpeedDelta * Time.deltaTime, Self.SpeedDelta * Time.deltaTime);
                Self.transform.Translate(Vector3.forward * Self.CurrentSpeed * Time.deltaTime);

                //Debug.DrawRay(Self.transform.position, toTarget * 3f, Color.green);
                //Debug.DrawRay(Self.transform.position, targetPerpendicular * 3f, Color.red);
                //Debug.DrawRay(Self.transform.position, modifiedTrajectory * 3f, Color.blue);
                //Debug.DrawRay(Self.transform.position, orientedToTarget * 3f, Color.green);
                await Await.NextUpdate();
            }
            Complete();
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
