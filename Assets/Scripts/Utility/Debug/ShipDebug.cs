#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Entities;
using AI.Actions;
using Entities.Parts;
using AI.Turrets;
using AI;
using AI.Tactics;
using Management;
using AI.Strategies;

namespace EditorDebug
{
    [ExecuteAlways]
    [RequireComponent(typeof(Ship))]
    public class ShipDebug : MonoBehaviour
    {
        Ship Self;
        
        public bool DebugMode;
        public bool IsInvincible;
        public string FactionName;
        public string FleetName;
        public bool AttackAllEnemies;
        public Transform OrbitTarget;
        public float OrbitRadius;
        public Vector3 MoveTarget;
        public Entity ShootTarget;


        [Header("Debug Data")]
        public string CurrentAction;

        void Start()
        {
            Self = gameObject.GetComponent<Ship>();

            if (Application.isPlaying)
            {
                GameManager.GameInitialized += OnGameInitialized; 
            }
        }

        private void OnGameInitialized()
        {
            Self.CurrentSystem = GameManager.Systems["Astraeus"];
            if (Application.isPlaying && DebugMode)
            {
                if (FactionName == "")
                {
                    FactionName = "DefaultFaction";
                }
                GameManager.RegisterFaction(FactionName);
                Self.CurrentFaction = GameManager.Factions[FactionName];

                if (FleetName == "")
                {
                    FleetName = Self.name + Self.GetInstanceID();
                }
                Self.CurrentFaction.RegisterFleet(FleetName);
                Self.CurrentFleet = Self.CurrentFaction.Fleets[FleetName];

                if (Self.CurrentFleet.StrategicAI.CurrentState != null)
                {
                    return;
                }

                if (AttackAllEnemies)
                {
                    Self.CurrentFleet.StrategicAI.SetBehaviour(new AttackAllEnemies(Self.CurrentFleet, Self.CurrentSystem), 1);
                }
                else
                {
                    if (OrbitTarget != null)
                    {
                        Self.AIStateMachine.SetBehaviour(new OrbitTarget(Self, OrbitTarget, Vector3.right, OrbitRadius));
                    }
                    else
                    {
                        //Self.AIStateMachine.SetBehaviour(new MoveTo(Self, MoveTarget));
                    }
                    if (ShootTarget != null)
                    {
                        foreach (WeaponController weapon in Self.Weapons)
                        {
                            weapon.stateMachine.SetBehaviour(new WeaponFireAt(weapon, ShootTarget));
                        }
                    }
                }
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (Self.AIStateMachine.CurrentState != null)
            {
                if (!Self.AIStateMachine.CurrentState.IsStopped)
                {
                    CurrentAction = Self.AIStateMachine.CurrentState.ToString(); 
                }
                else
                {
                    CurrentAction = "None";
                }
            }
            if(IsInvincible)
            {
                Self.Health.Shield = Self.Health.MaxShield;
                Self.Health.Hull = Self.Health.MaxHull;
            }
        }
    } 
}

#endif