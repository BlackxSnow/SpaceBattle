using AI.Tactics;
using Data;
using Entities;
using Management;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityAsync;

namespace AI.Strategies
{
    //TODO consider role based target aquisition, fleet members should act together according to their abilities
    //For now, this behaviour will use the first fleet member
    public class AttackAllEnemies : AIStrategy
    {
        Fleet Self;
        StarSystem System;

        protected override async void Behaviour(CancellationToken token)
        {
            Ship target;
            while(!token.IsCancellationRequested)
            {
                target = GetTarget(Self.Members[0].Member);

                if (target == null)
                {
                    break;
                }

                AttackTarget attackState = new AttackTarget(Self.Members[0].Member, target, false);
                Self.Members[0].Member.TacticalAI.SetBehaviour(attackState);

                for(int i = 1; i < Self.Members.Count; i++)
                {
                    if (Self.Members[i].Member.TacticalAI.CurrentState?.GetType() != typeof(Retreat))
                    {
                        Self.Members[i].Member.TacticalAI.SetBehaviour(new AttackTarget(Self.Members[i].Member, target, false)); 
                    }
                }
                while (!attackState.CompletionSource.Task.IsCompleted)
                {
                    SetRetreats();
                    await Await.NextUpdate();
                }
                await attackState.CompletionSource.Task;
            }
            //TODO Add 'patrol system' task to StrategicAI?
            Complete();
        }

        //Could be parallel if necessary
        private void SetRetreats()
        {
            bool isRetreating;
            for (int i = 0; i < Self.Members.Count; i++)
            {
                isRetreating = Self.Members[i].Member.TacticalAI.CurrentState.GetType() == typeof(Retreat);
                if (!isRetreating && Self.Members[i].Member.ShouldRetreat())
                {
                    Self.Members[i].Member.TacticalAI.SetBehaviour(new Retreat());
                }
            }
        }

        private Ship GetTarget(Ship origin)
        {
            Ship[] validTargets = System.Ships.Where(s => origin.GetTargetValidity(s, true)).ToArray();
            if (validTargets.Length == 0)
            {
                Debug.Log($"No targets found for {origin.name} in faction {origin.CurrentFaction.Name} in system {origin.CurrentSystem.SystemName}");
                return null;
            }
            float[] scores = new float[validTargets.Length];
            float distance, distanceScore, healthScore;
            HealthData health;
            for (int i = 0; i < validTargets.Length; i++)
            {
                //TODO calculate meaningful metrics
                //Ideas:
                //  Relative threat of target to ship, prefer easier targets
                //  Absolute threat of target, prefer to eliminate high threat targets
                //  Score penalty for distance outside of maximum weapon range according to ship maneouverability, including time to rotate
                //  weighting based on individual combat preferences
                //  Multiply by constant if currently engaging that enemy
                health = validTargets[i].Health;
                distance = Vector3.Distance(validTargets[i].transform.position, origin.transform.position);
                distanceScore = distance < origin.MaxRange ? (origin.MaxRange - distance) / origin.MaxRange : -(distance - origin.MaxRange) / origin.MaxSpeed;
                healthScore = ((health.MaxHull + health.MaxShield) - (health.Hull + health.Shield) * 2) / 60; //60 is arbitrary scaling. This value scales by the maximum health
                scores[i] = distanceScore + healthScore;
            }
            Debug.Log($"{validTargets.Length} targets found for {origin.name} in faction {origin.CurrentFaction.Name} in system {origin.CurrentSystem.SystemName}, highest score was {scores.Max()} for {validTargets[Array.IndexOf(scores, scores.Max())].name}");
            return validTargets[Array.IndexOf(scores, scores.Max())];
        }

        public AttackAllEnemies(Fleet self, StarSystem system)
        {
            Self = self;
            System = system;
        }
    }
}
