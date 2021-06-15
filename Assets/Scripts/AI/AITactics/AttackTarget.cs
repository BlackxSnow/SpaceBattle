using AI.Actions;
using AI.Turrets;
using Data;
using Entities;
using Entities.Parts;
using Management;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityAsync;

namespace AI.Tactics
{
    public class AttackTarget : AITactic
    {
        Ship Self;
        Ship Target;
        bool FollowTarget;

        //TODO Target priorities
        protected override async void Behaviour(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (!Self.GetTargetValidity(Target, !FollowTarget))
                {
                    break;
                }
                if (Self.AIStateMachine.CurrentState?.GetType() != typeof(Strafe) || (Self.AIStateMachine.CurrentState is Strafe strafe && strafe.Target != Target.transform))
                {
                    Self.AIStateMachine.SetBehaviour(new Strafe(Self, Target.transform, 10, Self.MaxRange));
                }
                //if(Self.AIStateMachine.CurrentState?.GetType() != typeof(OrbitTarget) || (Self.AIStateMachine.CurrentState is OrbitTarget orbit && orbit.Target != Target.transform))
                //{
                //    Self.AIStateMachine.SetBehaviour(new OrbitTarget(Self, Target.transform, Self.OrientationToEnemy, Self.PreferredRange));
                //}
                Self.CoordinateWeapons(Target);
                await Await.Seconds(1f);
            }

            Complete();
        }

        protected override void Complete()
        {
            foreach (WeaponController weapon in Self.Weapons)
            {
                weapon.stateMachine.Clear();
            }
            base.Complete();
        }

        public AttackTarget(Ship self, Ship target, bool followTarget)
        {
            Self = self;
            Target = target;
            FollowTarget = followTarget;
        }
    }
}
