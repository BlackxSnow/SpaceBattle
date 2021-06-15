using AI;
using AI.Actions;
using AI.Turrets;
using Entities.Parts;
using System;
using UnityEngine;
using Management;
using System.Collections.Generic;
using System.Linq;
using Utility;

namespace Entities
{
    public class Ship : Entity
    {
        public float CurrentSpeed;
        public float MaxSpeed;
        public float SpeedDelta;

        public float RotationSpeed;
        //public Vector3 CurrentAngular;
        //public Vector3 MaxAngular;
        //public Vector3 AngularDelta;

        public Vector3 OrientationToEnemy = Vector3.up;
        public float PreferredRange = 20; //Arbitrary placeholder range
        public float MaxRange = 30;

        protected StarSystem m_CurrentSystem;
        public StarSystem CurrentSystem
        {
            get => m_CurrentSystem;
            set
            {
                m_CurrentSystem?.DeregisterShip(this);
                m_CurrentSystem = value;
                m_CurrentSystem?.RegisterShip(this);
            }
        }

        private Fleet m_CurrentFleet;
        public Fleet CurrentFleet
        {
            get => m_CurrentFleet;
            set
            {
                m_CurrentFleet?.RemoveMember(this);
                m_CurrentFleet = value;
                m_CurrentFleet?.AddMember(this);
            }
        }


        //public StateMachine StrategicAI   = new StateMachine();
        public StateMachine TacticalAI      = new StateMachine();
        public StateMachine AIStateMachine  = new StateMachine();

        [Range(0, 199)]
        [Tooltip("Threshold below which to retreat. 0-100 = hull, 100-200 = shield")]
        public float RetreatThreshold = 50;

        [HideInInspector]
        public WeaponController[] Weapons;

        protected override void Awake()
        {
            Weapons = GetComponentsInChildren<WeaponController>();
            base.Awake();
        }

        protected override void Update()
        {
            base.Update();
            transform.Translate(Vector3.forward * CurrentSpeed * Time.deltaTime);
        }

        public bool ShouldRetreat()
        {
            if (RetreatThreshold <= 100)
            {
                return Health.Hull / Health.MaxHull < RetreatThreshold / 100;
            }
            else
            {
                return Health.Shield / Health.MaxShield < (RetreatThreshold - 100) / 100;
            }
        }

        protected override void Die()
        {
            AIStateMachine.Clear();
            TacticalAI.Clear();
            m_CurrentSystem.DeregisterShip(this);
            base.Die();
        }

        public bool GetTargetValidity(Ship target, bool inSystem = false)
        {
            //TODO Check if ship is docked
            if (target == null || (target.CurrentSystem != m_CurrentSystem && inSystem) || target.CurrentFaction == CurrentFaction)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public void CoordinateWeapons(Ship primaryTarget)
        {
            List<WeaponController> unassignedWeapons = Weapons.ToList();
            List<WeaponController> assignedWeapons = new List<WeaponController>();
            float maxRange = 0;

            //Try firing at primary target first
            bool isInRange;
            bool isSameTarget;
            foreach (WeaponController weapon in unassignedWeapons)
            {
                maxRange = weapon.MaxRange > maxRange ? weapon.MaxRange : maxRange;
                isInRange = weapon.MaxRange >= Vector3.Distance(weapon.transform.position, primaryTarget.transform.position);
                if (isInRange && weapon.CanAim(primaryTarget))
                {
                    isSameTarget = weapon.stateMachine.CurrentState is WeaponFireAt fireAt && fireAt.Target == primaryTarget;
                    if (isSameTarget)
                    {
                        assignedWeapons.Add(weapon);
                        continue;
                    }
                    weapon.stateMachine.SetBehaviour(new WeaponFireAt(weapon, primaryTarget));
                    assignedWeapons.Add(weapon);
                }
            }

            unassignedWeapons = unassignedWeapons.Except(assignedWeapons).ToList();

            //Group guns and target the ship with most numerous guns on target until no guns, or no ships are left
            List<(Ship target, List<WeaponController> weapons)> gunsOnTarget = new List<(Ship, List<WeaponController>)>();
            List<Ship> validShipsInSystem = new List<Ship>(CurrentSystem.Ships.Where(s => s.CurrentFaction != CurrentFaction));
            List<Ship> invalidTargets = new List<Ship>();
            int iterations = 0;
            while (validShipsInSystem.Count > 0 && unassignedWeapons.Count > 0)
            {
                assignedWeapons.Clear();
                foreach (Ship ship in validShipsInSystem)
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

                foreach (WeaponController weapon in bestTarget.weapons)
                {
                    isSameTarget = weapon.stateMachine.CurrentState is WeaponFireAt fireAt && fireAt.Target == primaryTarget;
                    if (isSameTarget)
                    {
                        assignedWeapons.Add(weapon);
                        continue;
                    }
                    weapon.stateMachine.SetBehaviour(new WeaponFireAt(weapon, bestTarget.target));
                    assignedWeapons.Add(weapon);
                }

                invalidTargets = gunsOnTarget.Where(t => t.weapons.Count == 0).Select(t => t.target).ToList();
                invalidTargets.Add(bestTarget.target);
                validShipsInSystem = validShipsInSystem.Except(invalidTargets).ToList();
                unassignedWeapons = unassignedWeapons.Except(assignedWeapons).ToList();
                if (iterations > 1000)
                {
                    throw new Exception("Reached 1000 iterations while coordinating weapons");
                }
                iterations++;
            }

            //foreach (WeaponController weapon in unassignedWeapons)
            //{
            //    weapon.stateMachine.Clear();
            //}
        }
    }
}
