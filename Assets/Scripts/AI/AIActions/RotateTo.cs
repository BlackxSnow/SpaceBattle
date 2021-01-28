using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Entities;
using UnityAsync;

namespace AI.Actions
{
    public class RotateTo : AIAction
    {
        Ship Self;
        Quaternion StartingRotation;
        Quaternion TargetRotation;
        protected override async void Behaviour(CancellationToken token)
        {
            //Quaternion toTarget = Util.FromTo(StartingRotation, TargetRotation);
            //Matrix4x4 rotMatrix = Matrix4x4.Rotate(toTarget);
            float traversedAngle = 0;
            float totalAngle = Quaternion.Angle(StartingRotation, TargetRotation);
            float deltaAngle, t = 0;
            while(!token.IsCancellationRequested && t < 1)
            {
                deltaAngle = Self.RotationSpeed * Time.deltaTime;
                traversedAngle += deltaAngle;
                t = Mathf.Clamp01(traversedAngle / totalAngle);
                Self.transform.rotation = Quaternion.Slerp(StartingRotation, TargetRotation, t);
                
                if(t >= 1)
                {
                    break;
                }
                else
                {
                    await Await.NextUpdate();
                }
            }
            Complete();
        }

        public RotateTo(Ship self, Quaternion start, Quaternion target)
        {
            Self = self;
            StartingRotation = start;
            TargetRotation = target;
        }
        public RotateTo(Ship self, Vector3 start, Vector3 end)
        {
            Self = self;
            StartingRotation = Quaternion.LookRotation(start, self.transform.up);
            TargetRotation = Quaternion.LookRotation(end);
        }
    }
}
