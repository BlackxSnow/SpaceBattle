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

                Self.AIStateMachine.SetBehaviour(new OrbitTarget(Self, Target.transform, Self.OrientationToEnemy, Self.PreferredRange));
                CoordinateWeapons(Target);
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

        private void CoordinateWeapons(Ship primaryTarget)
        {
            List<WeaponController> unassignedWeapons = Self.Weapons.ToList();
            List<WeaponController> assignedWeapons = new List<WeaponController>();
            float maxRange = 0;

            foreach(WeaponController weapon in unassignedWeapons)
            {
                maxRange = weapon.MaxRange > maxRange ? weapon.MaxRange : maxRange;
                if (weapon.MaxRange >= Vector3.Distance(weapon.transform.position, primaryTarget.transform.position) && weapon.CanAim(primaryTarget))
                {
                    weapon.stateMachine.SetBehaviour(new WeaponFireAt(weapon, primaryTarget));
                    assignedWeapons.Add(weapon);
                }
            }

            unassignedWeapons = unassignedWeapons.Except(assignedWeapons).ToList();

            List<(Ship target, List<WeaponController> weapons)> gunsOnTarget = new List<(Ship, List<WeaponController>)>();
            List<Ship> shipsInSystem = new List<Ship>(Self.CurrentSystem.Ships);
            List<Ship> invalidTargets = new List<Ship>();
            int iterations = 0;
            while (shipsInSystem.Count > 0 && unassignedWeapons.Count > 0)
            {
                assignedWeapons.Clear();
                foreach (Ship ship in shipsInSystem)
                {
                    (Ship target, List<WeaponController> weapons) entry;
                    entry.target = ship;
                    entry.weapons = new List<WeaponController>();

                    foreach (WeaponController weapon in unassignedWeapons)
                    {
                        if (Vector3.Distance(ship.transform.position, weapon.transform.position) <= maxRange && weapon.CanAim(ship))
                        {
                            entry.weapons.Add(weapon);
                        }
                    }
                    gunsOnTarget.Add(entry);
                }
                (Ship target, List<WeaponController> weapons) bestTarget = gunsOnTarget.Aggregate((item1, item2) => item1.weapons.Count > item2.weapons.Count ? item1 : item2);

                foreach(WeaponController weapon in bestTarget.weapons)
                {
                    weapon.stateMachine.SetBehaviour(new WeaponFireAt(weapon, bestTarget.target));
                    assignedWeapons.Add(weapon);
                }

                invalidTargets = gunsOnTarget.Where(t => t.weapons.Count == 0).Select(t => t.target).ToList();
                invalidTargets.Add(bestTarget.target);
                shipsInSystem = shipsInSystem.Except(invalidTargets).ToList();
                unassignedWeapons = unassignedWeapons.Except(assignedWeapons).ToList();
                if (iterations > 1000)
                {
                    throw new Exception("Reached 1000 iterations while coordinating weapons");
                }
                iterations++;
            }

            foreach(WeaponController weapon in unassignedWeapons)
            {
                weapon.stateMachine.Clear();
            }
        }

        public AttackTarget(Ship self, Ship target, bool followTarget)
        {
            Self = self;
            Target = target;
            FollowTarget = followTarget;
        }
    }
}
