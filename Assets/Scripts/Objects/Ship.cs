using AI;
using AI.Actions;
using AI.Turrets;
using Entities.Parts;
using System;
using UnityEngine;
using Management;

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

        private StarSystem currentSystem;
        public StarSystem CurrentSystem
        {
            get => currentSystem;
            set
            {
                currentSystem?.DeregisterShip(this);
                currentSystem = value;
                currentSystem.RegisterShip(this);
            }
        }

        public StateMachine StrategicAI     = new StateMachine();
        public StateMachine TacticalAI      = new StateMachine();
        public StateMachine AIStateMachine  = new StateMachine();


        [HideInInspector]
        public WeaponController[] Weapons;

        private void Start()
        {
            Weapons = GetComponentsInChildren<WeaponController>();
        }

        protected override void Update()
        {
            base.Update();
            transform.Translate(Vector3.forward * CurrentSpeed * Time.deltaTime);
        }

        protected override void Die()
        {
            AIStateMachine.Clear();
            TacticalAI.Clear();
            currentSystem.DeregisterShip(this);
            base.Die();
        }

        public bool GetTargetValidity(Ship target, bool inSystem = false)
        {
            //TODO Check if ship is docked
            if (target == null || (target.CurrentSystem != currentSystem && inSystem) || target.Team == Team)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
