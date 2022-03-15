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

        [Header("Movement")]
        public Vector3 CurrentVelocity;
        public float TargetSpeed;
        public float MaxSpeed;
        public float Acceleration;

        [Header("Rotation")]
        /// <summary>
        /// Pitch, yaw, and roll acceleration values at ideal turning speed respectively
        /// </summary>
        public Vector3 PeakRotationAcceleration;
        /// <summary>
        /// Minimum pitch, yaw, and roll acceleration values
        /// </summary>
        public Vector3 MinRotationAcceleration;
        /// <summary>
        /// Percent of max speed where angular acceleration is highest
        /// </summary>
        [Tooltip("Percent of max speed where angular acceleration is highest")]
        public float PeakManoeuvreVelocity;

        /// <summary>
        /// The current angular velocity in world space
        /// </summary>
        public Vector3 RotationDelta;
        /// <summary>
        /// The target RotationDelta in local space (Pitch, Yaw, Roll). Positive values rotate as follows: Nose down, Nose right, anti-clockwise
        /// </summary>
        public Vector3 TargetRotationSpeed;
        public float MaxPitchYawDelta;
        public float MaxRollDelta;


        public Vector3 OrientationToEnemy = Vector3.up;
        public float PreferredRange = 20; //Arbitrary placeholder range
        public float MaxRange = 30;

        public PIDController PitchPID = new PIDController(1, 0, 0, 0);
        public PIDController YawPID = new PIDController(1, 0, 0, 0);
        public PIDController RollPID = new PIDController(1, 0, 0, 0);

        public float ProportionalGain = 1;
        public float IntegralGain = 1;
        public float DerivativeGain = 1;

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

            ReduceNonForwardVelocity();
            ApproachTargetSpeed();
            ApproachTargetRotationSpeed();

            transform.Rotate(RotationDelta * Time.deltaTime, Space.World);
            transform.Translate(CurrentVelocity * Time.deltaTime, Space.World);
        }

        const float rollThreshold = 15;

        public bool RotateTowards(Vector3 heading, Vector3 up)
        {
            return RotateTowards(heading, up, out _);
        }

        public bool RotateTowards(Vector3 heading, Vector3 up, out Vector3 targetAngles)
        {
            TargetRotationSpeed = GetRotateTowardsTargets(heading, up, out targetAngles);
            bool isFinished = Mathf.Approximately(TargetRotationSpeed.magnitude, 0);
            return isFinished;
        }

        public Vector3 GetRotateTowardsTargets(Vector3 heading, Vector3 up, out Vector3 targetAngles)
        {
            Vector3 result;
            Vector3 currentRotationAcceleration = GetCurrentRotationAcceleration();

            Vector3 headingOnRollPlane = Vector3.ProjectOnPlane(heading, transform.forward);

            float targetRollAngle;
            float angleFromHeading = Vector3.Angle(transform.forward, heading);
            if (angleFromHeading > rollThreshold)
            {
                float upRollAngle = Vector3.SignedAngle(transform.up, headingOnRollPlane, transform.forward);
                float downRollAngle = Vector3.SignedAngle(-transform.up, headingOnRollPlane, transform.forward);
                targetRollAngle = Mathf.Abs(upRollAngle) < Mathf.Abs(downRollAngle) ? upRollAngle : downRollAngle;
            }
            else
            {
                targetRollAngle = Vector3.SignedAngle(transform.up, Vector3.ProjectOnPlane(up, transform.forward), transform.forward);
            }

            result.z = Mathf.Sign(targetRollAngle) * Mathf.Sqrt(Mathf.Abs(targetRollAngle) * currentRotationAcceleration.z * 2);

            float targetPitchAngle = Vector3.SignedAngle(transform.forward, Vector3.ProjectOnPlane(heading, transform.right), transform.right);
            result.x = Mathf.Sign(targetPitchAngle) * Mathf.Sqrt(Mathf.Abs(targetPitchAngle) * currentRotationAcceleration.x * 2);

            float targetYawAngle = Vector3.SignedAngle(transform.forward, Vector3.ProjectOnPlane(heading, transform.up), transform.up);
            result.y = Mathf.Sign(targetYawAngle) * Mathf.Sqrt(Mathf.Abs(targetYawAngle) * currentRotationAcceleration.y * 2);

            Debug.Assert(result.x != float.NaN && result.y != float.NaN & result.z != float.NaN);
            targetAngles = new Vector3(targetPitchAngle, targetYawAngle, targetRollAngle);
            return result;
        }

        /// <summary>
        /// Rotate towards a direction with a static angular velocity target of 'speed'
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="speed"></param>
        /// <param name="useRoll">Whether or not to use roll to orient the pitch axis towards the target</param>
        public void RotateInDirection(Vector3 direction, float speed, bool useRoll = true)
        {
            Vector3 currentRotationAcceleration = GetCurrentRotationAcceleration();
            Vector3 directionOnForwardPlane = Vector3.ProjectOnPlane(direction, transform.forward);
            //Orient pitch towards direction
            float angleFromHeading = Vector3.Angle(transform.forward, direction * Mathf.Sign(speed));
            if (angleFromHeading > rollThreshold && useRoll)
            {
                float upRollAngle = Vector3.SignedAngle(transform.up, directionOnForwardPlane, transform.forward);
                float downRollAngle = -(180 - Mathf.Abs(upRollAngle));
                float targetRollAngle = Mathf.Abs(upRollAngle) < Mathf.Abs(downRollAngle) ? upRollAngle : downRollAngle;
                TargetRotationSpeed.z = Mathf.Sign(targetRollAngle) * Mathf.Sqrt(Mathf.Abs(targetRollAngle) * currentRotationAcceleration.z * 2);
            }

            float rightProjection = Util.ScalarProjection(transform.right, directionOnForwardPlane);
            float leftProjection = -rightProjection;
            float yawModifier = Mathf.Max(rightProjection, leftProjection);
            TargetRotationSpeed.y = (rightProjection > leftProjection ? 1 : -1) * speed * yawModifier;

            float upProjection = Util.ScalarProjection(transform.up, directionOnForwardPlane);
            float downProjection = -upProjection;
            float pitchModifier = Mathf.Max(upProjection, downProjection);
            TargetRotationSpeed.x = (downProjection > upProjection ? 1 : -1) * speed * pitchModifier;

            if (float.IsNaN(TargetRotationSpeed.x) || float.IsNaN(TargetRotationSpeed.y) || float.IsNaN(TargetRotationSpeed.z))
            {
                TargetRotationSpeed = new Vector3(0, 0, 0);
                throw new ArgumentException($"At least one component of TargetRotationSpeed was set to NaN.");
            }
        }

        /// <summary>
        /// Calculates the current rotation acceleration values based on current forward speed
        /// </summary>
        /// <returns></returns>
        public Vector3 GetCurrentRotationAcceleration()
        {
            float speedPercentage = Mathf.Abs(Util.ScalarProjection(CurrentVelocity, transform.forward)) / MaxSpeed;
            float distanceForMin = Mathf.Max(1f - PeakManoeuvreVelocity, PeakManoeuvreVelocity);

            float velocityDistance = Mathf.Abs(speedPercentage - PeakManoeuvreVelocity);
            Vector3 result = Vector3.Lerp(PeakRotationAcceleration, MinRotationAcceleration, velocityDistance / distanceForMin);
            return result;
        }

        public float GetIdealTurnSpeed()
        {
            return PeakManoeuvreVelocity * MaxSpeed;
        }

        /// <summary>
        /// Move velocity towards the target forward speed by up to acceleration
        /// </summary>
        protected void ApproachTargetSpeed()
        {
            CurrentVelocity += transform.forward * Mathf.Clamp(TargetSpeed - Util.ScalarProjection(CurrentVelocity, transform.forward), -Acceleration * Time.deltaTime, Acceleration * Time.deltaTime);

            float forwardSpeed = GetForwardSpeed();
            if (forwardSpeed > MaxSpeed)
            {
                CurrentVelocity /= forwardSpeed / MaxSpeed;
            }
        }

        public float GetForwardSpeed()
        {
            return Util.ScalarProjection(CurrentVelocity, transform.forward);
        }

        protected void ApproachTargetRotationSpeed()
        {
            Vector3 currentRoll = Vector3.Project(RotationDelta, transform.forward);
            Vector3 targetRoll = transform.forward * TargetRotationSpeed.z;

            Vector3 currentPitch = Vector3.Project(RotationDelta, transform.right);
            Vector3 targetPitch = transform.right * TargetRotationSpeed.x;

            Vector3 currentYaw = Vector3.Project(RotationDelta, transform.up);
            Vector3 targetYaw = transform.up * TargetRotationSpeed.y;

            //TODO: update to use modified rot speed based on current velocity
            RotationDelta = Vector3.MoveTowards(currentRoll, targetRoll, PeakRotationAcceleration.z * Time.deltaTime);
            RotationDelta += Vector3.MoveTowards(currentPitch, targetPitch, PeakRotationAcceleration.x * Time.deltaTime);
            RotationDelta += Vector3.MoveTowards(currentYaw, targetYaw, PeakRotationAcceleration.y * Time.deltaTime);
        }

        /// <summary>
        /// Reduce all non-forward velocity by up to acceleration value
        /// </summary>
        protected void ReduceNonForwardVelocity()
        {
            Vector3 nonForwardVelocity = (CurrentVelocity - Vector3.Project(CurrentVelocity, transform.forward)) * 0.5f;
            Vector3 reductionValue = Vector3.MoveTowards(Vector3.zero, nonForwardVelocity, Acceleration);
            CurrentVelocity -= reductionValue + nonForwardVelocity;
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
