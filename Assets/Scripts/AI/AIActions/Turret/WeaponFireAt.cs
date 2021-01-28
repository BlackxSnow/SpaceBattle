using Entities.Parts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityAsync;
using UnityEngine;
using Utility;

namespace AI.Turrets
{
    public class WeaponFireAt : AIAction
    {
        WeaponController Self;
        Entity Target;
        Transform AimPoint;

        protected async override void Behaviour(CancellationToken token)
        {
            const float validationTime = 1;
            float t = validationTime;
            while (!token.IsCancellationRequested && Target != null)
            {
                if (t>=validationTime)
                {
                    t = 0;
                    if (AimPoint == null || !ValidateAimPoint(AimPoint))
                    {
                        AimPoint = GetAimPoint();
                        if (AimPoint == null)
                        {
                            Self.StopFiring();
                            t = 1;
                            await Await.Seconds(1);
                            continue;
                        }
                    }
                }

                (bool canAim, bool isFinished) aimResult = Self.AimAt(AimPoint.position);
                if (aimResult.isFinished)
                {
                    Self.StartFiring();
                }
                else
                {
                    Self.StopFiring();
                }
                await Await.NextUpdate();
                t += Time.deltaTime;
            }
            Self.StopFiring();
            Complete();
        }

        Transform GetAimPoint()
        {
            List<Transform> shuffled = new List<Transform>(Target.AimPoints);
            shuffled.Shuffle();

            for(int i = 0; i < shuffled.Count; i++)
            {
                if (ValidateAimPoint(shuffled[i]))
                {
                    return shuffled[i];
                }
            }
            return null;
            //throw new ArgumentException($"Cannot hit any aim points on {Target.name}");
        }

        bool ValidateAimPoint(Vector3 point)
        {
            Ray ray = new Ray(Self.transform.position, point - Self.transform.position);
            if (Physics.Raycast(ray, out RaycastHit hit, Self.MaxRange))
            {
                if (hit.transform == Target.transform || hit.transform.IsChildOf(Target.transform))
                {
                    return true;
                }
            }
            return false;
        }
        bool ValidateAimPoint(Transform transform)
        {
            return ValidateAimPoint(transform.position);
        }

        public WeaponFireAt(WeaponController self, Entity target)
        {
            Self = self;
            Target = target;
        }
    }
}
