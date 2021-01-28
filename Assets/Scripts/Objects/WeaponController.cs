using Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityAsync;
using UnityEngine.VFX;
using AI;
using AI.Turrets;
using Entities.Parts.Weapons;
using Management;
using static Util;

namespace Entities.Parts
{
    public class WeaponController : MonoBehaviour
    {
        [HideInInspector]
        public Ship Owner;

        public StateMachine stateMachine = new StateMachine();

        [Header("Gimbal/Turret Constraints")]
        public Vector3 Meridian;
        public Vector3 Horizon;
        public Bounds1D AzimuthBounds;
        public Bounds1D ElevationBounds;
        public float AzimuthSpeed;
        public float ElevationSpeed;

        public float MaxRange = 30;

        [Header("Parts")]
        [SerializeField]
        Transform AzimuthTransform;
        [SerializeField]
        Transform ElevationTransform;
        [SerializeField] [Tooltip("Targets are aligned with this transform's forward")]
        Transform TargetSight;

        public Weapon[] Weapons;
        [SerializeField]
        public string[] WeaponLoadout;

        public float CurrentAzimuth;
        public float CurrentElevation;
        //float OffsetAngle;

        public bool ISDEBUG;
        public Entity DEBUGTARGET;

        private void Awake()
        {
            Owner = GetComponentInParent<Ship>();
            Weapons = new Weapon[0];
            DataManager.DataLoaded += OnDataLoaded;
            //OffsetAngle = Vector3.SignedAngle(Meridian, )
        }

        protected void OnDataLoaded()
        {
            Weapons = new Weapon[WeaponLoadout.Length];
            List<Transform> firingPieces = transform.FindChildren("FiringPiece", true);
            for(int i = 0; i < WeaponLoadout.Length; i++)
            {
                if(DataManager.Weapons.TryGetValue(WeaponLoadout[i], out JsonConstructors.WeaponConstructor wepData))
                {
                    Weapons[i] = wepData.CreateWeapon(DataManager.Materials["Blit_Additive"], firingPieces[i]);
                }
                else
                {
                    throw new ArgumentException($"No weapon '{WeaponLoadout[i]}' exists within loaded data.");
                }
            }
            if (ISDEBUG)
            {
                stateMachine.SetBehaviour(new WeaponFireAt(this, DEBUGTARGET));
            }
        }



        private struct TargetingData
        {
            public Vector2 ForwardTargets, BackwardTargets;
            public bool IsForwardValid, IsBackwardsValid;

            public TargetingData(Vector2 forward, Vector2 backward, bool fValid, bool bValid)
            {
                ForwardTargets = forward;
                BackwardTargets = backward;
                IsForwardValid = fValid;
                IsBackwardsValid = bValid;
            }
        }

        public bool CanAim(Vector3 pos)
        {
            TargetingData data = CalculateTargets(pos);
            return data.IsBackwardsValid || data.IsForwardValid;
        }
        public bool CanAim(Entity target)
        {
            TargetingData centre = CalculateTargets(target.transform.position);
            if (centre.IsBackwardsValid || centre.IsForwardValid)
            {
                return true;
            }
            else
            {
                bool[] canAim = new bool[target.AimBounds.Length];
                Parallel.For(0, target.AimBounds.Length, (i) =>
                {
                    TargetingData data = CalculateTargets(target.transform.TransformPoint(target.AimBounds[i]));
                    canAim[i] = data.IsBackwardsValid || data.IsForwardValid;
                });
                //Minimum two bound verts visible
                if (canAim.Count(b => b) >= 2)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private TargetingData CalculateTargets(Vector3 pos)
        {
            TargetingData result = new TargetingData();
            //Vector3 meridianWorldSpace = transform.localToWorldMatrix.MultiplyVector(Meridian).UNormalized();
            //Vector3 horizonWorldSpace = transform.localToWorldMatrix.MultiplyVector(Horizon).UNormalized();
            //Vector3 meridianUp = Vector3.Cross(meridianWorldSpace, horizonWorldSpace);
            Vector3 targetDir = (pos - TargetSight.position).UNormalized();
            Vector3 targetDirExcludeUp = Vector3.ProjectOnPlane(targetDir, transform.up);

            Vector3 elevationAxis = Vector3.Cross(targetDirExcludeUp, transform.up);

            result.ForwardTargets.x = Vector3.SignedAngle(transform.forward, targetDirExcludeUp, transform.up);
            result.ForwardTargets.y = -Vector3.SignedAngle(targetDirExcludeUp, targetDir, elevationAxis);

            if (Mathf.Abs(Vector3.SignedAngle(elevationAxis, TargetSight.forward, transform.up)) > 180)
            {
                result.ForwardTargets.y *= -1;
            }

            result.BackwardTargets.x = (result.ForwardTargets.x + 360) % 360 - 180;
            result.BackwardTargets.y = (90 + (90 - Mathf.Abs(result.ForwardTargets.y))) * Mathf.Sign(result.ForwardTargets.y);

            result.IsForwardValid = WithinBounds(result.ForwardTargets.x, AzimuthBounds) && WithinBounds(result.ForwardTargets.y, ElevationBounds);
            result.IsBackwardsValid = WithinBounds(result.BackwardTargets.x, AzimuthBounds) && WithinBounds(result.BackwardTargets.y, ElevationBounds);
            return result;
        }

        /// <summary>
        /// Attempt to aim at pos
        /// </summary>
        /// <param name="pos"></param>
        /// <returns>(isAimable, isFinished)</returns>
        public (bool, bool) AimAt(Vector3 pos)
        {
            TargetingData data = CalculateTargets(pos);

            float totalAzimuth, totalElevation;
            bool wrapAzimuth = false;

            bool isFullCircle = Mathf.Abs(AzimuthBounds.Min) + Mathf.Abs(AzimuthBounds.Max) >= 360;
            if (data.IsForwardValid && data.IsBackwardsValid)
            {
                float forwardAziDist, forwardEleDist, backwardAziDist, backwardEleDist;
                bool forwardAziWrap = false, backwardAziWrap = false;
                forwardAziDist = isFullCircle ? DistanceBetween(CurrentAzimuth, data.ForwardTargets.x, AzimuthBounds, out forwardAziWrap) : Mathf.Abs(CurrentAzimuth - data.ForwardTargets.x);
                forwardEleDist = Mathf.Abs(CurrentElevation - data.ForwardTargets.y);
                backwardAziDist = isFullCircle ? DistanceBetween(CurrentAzimuth, data.BackwardTargets.x, AzimuthBounds, out backwardAziWrap) : Mathf.Abs(CurrentAzimuth - data.BackwardTargets.x);
                backwardEleDist = Mathf.Abs(CurrentElevation - data.BackwardTargets.y);

                float totalForwardTime = Mathf.Max(forwardAziDist / AzimuthSpeed, forwardEleDist / ElevationSpeed);
                float totalBackwardTime = Mathf.Max(backwardAziDist / AzimuthSpeed, backwardEleDist / ElevationSpeed);

                if (totalForwardTime <= totalBackwardTime)
                {
                    totalAzimuth = data.ForwardTargets.x;
                    totalElevation = data.ForwardTargets.y;
                    wrapAzimuth = forwardAziWrap;
                }
                else
                {
                    totalAzimuth = data.BackwardTargets.x;
                    totalElevation = data.BackwardTargets.y;
                    wrapAzimuth = backwardAziWrap;
                }
            }
            else if (data.IsForwardValid)
            {
                totalAzimuth = data.ForwardTargets.x;
                totalElevation = data.ForwardTargets.y;

            }
            else if (data.IsBackwardsValid)
            {
                totalAzimuth = data.BackwardTargets.x;
                totalElevation = data.BackwardTargets.y;
            }
            else
            {
                return (false, false);
            }

            if (isFullCircle && (!data.IsForwardValid || !data.IsBackwardsValid))
            {
                DistanceBetween(CurrentAzimuth, totalAzimuth, AzimuthBounds, out wrapAzimuth);
            }

            float deltaAziSpeed = AzimuthSpeed * Time.deltaTime;
            float deltaEleSpeed = ElevationSpeed * Time.deltaTime;
                
            float deltaAzimuth = totalAzimuth - CurrentAzimuth;
            float deltaElevation = totalElevation - CurrentElevation;

            if(wrapAzimuth)
            {
                deltaAzimuth = 180 - Mathf.Abs(CurrentAzimuth) + 180 - Mathf.Abs(totalAzimuth) * Mathf.Sign(CurrentAzimuth);
            }
            const float aimTolerance = 5;

            //bool withinTolerances = Mathf.Sqrt(Mathf.Pow(Mathf.Max(Mathf.Abs(deltaAzimuth) - deltaAziSpeed, 0), 2) + Mathf.Pow(Mathf.Max(Mathf.Abs(deltaElevation) - deltaEleSpeed, 0), 2)) < aimTolerance;
            bool withinTolerances = Vector3.Angle(TargetSight.forward, pos - transform.position) < aimTolerance;
            deltaAzimuth = Mathf.Clamp(deltaAzimuth, -deltaAziSpeed, deltaAziSpeed);
            deltaElevation = Mathf.Clamp(deltaElevation, -deltaEleSpeed, deltaEleSpeed);
            CurrentAzimuth += deltaAzimuth;
            CurrentElevation += deltaElevation;

            if (Mathf.Abs(CurrentAzimuth) > 180)
            {
                CurrentAzimuth = (180 - Mathf.Abs(CurrentAzimuth % 180)) * Mathf.Sign(-CurrentAzimuth);
            }

            AzimuthTransform.Rotate(0, deltaAzimuth, 0, Space.Self);
            ElevationTransform.Rotate(deltaElevation, 0, 0, Space.Self);

            if (withinTolerances)
            {
                return (true, true);
            }
            else
            {
                return (true, false);
            }
        }

        /// <summary>
        /// Provides the distance from one number to another with number wrapping
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="wrap">Lower and upper bounds of rotational wrapping</param>
        /// <param name="wrapped">true if the shorter distance is by wrapping</param>
        /// <returns></returns>
        private float DistanceBetween(float a, float b, Bounds1D wrap, out bool wrapped)
        {
            float noWrapDist = Mathf.Abs(a - b);
            float wrappedDist = wrap.Max - Mathf.Abs(a) + wrap.Max - Mathf.Abs(b);

            wrapped = wrappedDist < noWrapDist ? true : false;
            return Mathf.Min(noWrapDist, wrappedDist);
        }

        public void StartFiring()
        {
            foreach(Weapon weapon in Weapons)
            {
                weapon.StartFiring();
            }
        }

        public void StopFiring()
        {
            foreach(Weapon weapon in Weapons)
            {
                weapon.StopFiring();
            }
        }

        private void OnDestroy()
        {
            StopFiring();
        }
    }
}
