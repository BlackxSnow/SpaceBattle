using Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityAsync;
using UnityEngine;

namespace AI.Actions
{
    public class Strafe : AIAction
    {
        Ship Self;
        public Transform Target;
        float BreakoffDistance, EngageDistance;

        protected override async void Behaviour(CancellationToken token)
        {
            Vector3 toTarget;
            Quaternion targetRotation;
            float targetDistance, targetSpeed;
            bool isApproaching = Vector3.Distance(Self.transform.position, Target.position) > BreakoffDistance;
            while (!token.IsCancellationRequested && Self != null && Target != null)
            {
                Vector3 targetHeading;
                toTarget = Target.position - Self.transform.position;

                //Distance from target surface
                //Could be changed to bounding box for better performance
                Ray ray = new Ray(Self.transform.position, toTarget);
                RaycastHit[] hits = Physics.RaycastAll(ray, Vector3.Distance(Self.transform.position, Target.position));
                if (hits.Length == 0)
                {
                    Debug.DrawRay(Self.transform.position, toTarget.UNormalized() * Vector3.Distance(Self.transform.position, Target.position), Color.green, 5);
                }
                targetDistance = hits.Where(h => h.transform.IsChildOf(Target) || h.transform == Target).Aggregate((min, next) => min.distance < next.distance ? min : next).distance;

                if (isApproaching && targetDistance < BreakoffDistance)
                {
                    isApproaching = false;
                    //Debug.Log($"{Self.name} is beginning breakoff at {targetDistance}");
                }
                else if (!isApproaching && targetDistance > EngageDistance)
                {
                    isApproaching = true;
                    //Debug.Log($"{Self.name} is beginning engage at {targetDistance}");
                }

                if (isApproaching)
                {
                    targetHeading = toTarget;
                }
                else
                {
                    targetHeading = -toTarget;
                }

                targetRotation = Quaternion.LookRotation(targetHeading);
                if (Quaternion.Angle(Self.transform.rotation, targetRotation) > Self.RotationSpeed)
                {
                    targetSpeed = Self.MaxSpeed / 2;
                }
                else
                {
                    targetSpeed = Self.MaxSpeed;
                }
                Self.transform.rotation = Quaternion.RotateTowards(Self.transform.rotation, targetRotation, Self.RotationSpeed * Time.deltaTime);
                Self.CurrentSpeed += Mathf.Clamp(targetSpeed - Self.CurrentSpeed, -Self.SpeedDelta * Time.deltaTime, Self.SpeedDelta * Time.deltaTime);
                await Await.NextUpdate();
            }

            Complete();
        }

        protected override void Complete()
        {
            base.Complete();
        }

        public Strafe(Ship self, Transform target, float breakoff, float engage)
        {
            Self = self;
            Target = target;
            BreakoffDistance = breakoff;
            EngageDistance = engage;
        }
    }
}
