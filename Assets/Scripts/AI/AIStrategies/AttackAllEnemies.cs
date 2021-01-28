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

namespace AI.Strategies
{
    public class AttackAllEnemies : AIStrategy
    {
        Ship Self;
        StarSystem System;

        protected override async void Behaviour(CancellationToken token)
        {
            Ship target;
            while(!token.IsCancellationRequested)
            {
                target = GetTarget();

                if (target == null)
                {
                    break;
                }

                AttackTarget attackState = new AttackTarget(Self, target, false);
                Self.TacticalAI.SetBehaviour(attackState);
                await attackState.CompletionSource.Task;
            }
            //TODO Add 'patrol system' task to StrategicAI?
            Complete();
        }

        private Ship GetTarget()
        {
            Ship[] validTargets = System.Ships.Where(s => Self.GetTargetValidity(s, true)).ToArray();
            if (validTargets.Length == 0)
            {
                Debug.Log($"No targets found for {Self.name} on team {Self.Team} in system {Self.CurrentSystem.SystemName}");
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
                distance = Vector3.Distance(validTargets[i].transform.position, Self.transform.position);
                distanceScore = distance < Self.MaxRange ? (Self.MaxRange - distance) / Self.MaxRange : -(distance - Self.MaxRange) / Self.MaxSpeed;
                healthScore = ((health.MaxHull + health.MaxShield) - (health.Hull + health.Shield) * 2) / 60; //60 is arbitrary scaling. This value scales by the maximum health
                scores[i] = distanceScore + healthScore;
            }
            Debug.Log($"{validTargets.Length} targets found for {Self.name} on team {Self.Team} in system {Self.CurrentSystem.SystemName}, highest score was {scores.Max()} for {validTargets[Array.IndexOf(scores, scores.Max())].name}");
            return validTargets[Array.IndexOf(scores, scores.Max())];
        }

        public AttackAllEnemies(Ship self, StarSystem system)
        {
            Self = self;
            System = system;
        }
    }
}
