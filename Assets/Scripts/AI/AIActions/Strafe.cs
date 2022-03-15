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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
        //Could be changed to bounding box for better performance
        protected float GetDistanceToTarget(Vector3 direction)
        {
            Ray ray = new Ray(Self.transform.position, direction);
            RaycastHit[] hits = Physics.RaycastAll(ray, Vector3.Distance(Self.transform.position, Target.position));
            if (hits.Length == 0)
            {
                return 0;
            }
            else
            {
                return hits.Where(h => h.transform.IsChildOf(Target) || h.transform == Target).Aggregate((min, next) => min.distance < next.distance ? min : next).distance;
            }
        }

        private enum StrafeState
        {
            Approaching,
            BreakingOff
        }

        protected override async void Behaviour(CancellationToken token)
        {
            Vector3 toTarget = Target.position - Self.transform.position;
            Quaternion targetRotation;
            float targetDistance;
            Vector3 breakoffPointObjectSpace;

            StrafeState currentState = StrafeState.Approaching;


            while (!token.IsCancellationRequested && Self != null && Target != null && Self != Target)
            {
                Vector3 targetHeading;
                toTarget = Target.position - Self.transform.position;

                //Distance from target surface
                targetDistance = GetDistanceToTarget(toTarget);
                
                if (currentState == StrafeState.Approaching)
                {
                    if (targetDistance < BreakoffDistance)
                    {
                        currentState = StrafeState.BreakingOff;
                    }
                }
                else
                {
                    if (targetDistance > EngageDistance)
                    {
                        currentState = StrafeState.Approaching;
                    }
                }

                if (currentState == StrafeState.Approaching)
                {
                    targetHeading = toTarget;
                }
                else
                {
                    targetHeading = -toTarget;
                }

                targetRotation = Quaternion.LookRotation(targetHeading);

                bool facingTarget = Self.RotateTowards(targetHeading, Vector3.up);
                Self.TargetSpeed = facingTarget ? Self.MaxSpeed : Self.GetIdealTurnSpeed();

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
