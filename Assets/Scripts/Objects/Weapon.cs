using Entities.Parts.Weapons;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Entities.Parts.Weapons
{
    [Serializable]
    public abstract class Weapon
    {
        [Header("Weapon Data")]
        [Tooltip("Damage per shot or tick")]
        public float Damage;
        [Tooltip("Damage ticks or shots per second")]
        public float RateOfFire;

        [Header("Projectile Data")]
        [SerializeField]
        protected GameObject Projectile;
        [SerializeField]
        protected GameObject ImpactVFX;
        [SerializeField]
        protected float ImpactStrength;
        [SerializeField]
        protected Texture2D ImpactTexture;
        [SerializeField]
        protected float ImpactSize = 1.0f;
        [SerializeField]
        protected Material BlitMaterial;
        [SerializeField]
        protected Color ProjectileColour;

        [SerializeField] [Tooltip("Transform to spawn projectiles from")]
        protected Transform FiringPiece;

        protected Utility.Async.Timer Cooldown;
        protected CancellationTokenSource FireToken;
        protected bool IsFiring;
        protected bool IsReadyToFire;
        protected WeaponController Controller;

        public virtual void Init()
        {
            Cooldown = new Utility.Async.Timer(1f / RateOfFire, (_) => { IsReadyToFire = true; }, false);
            Cooldown.Start();
        }

        public void StartFiring()
        {
            if (!IsFiring)
            {
                IsFiring = true;
                FireToken = new CancellationTokenSource();
                FireBehaviour(FireToken.Token);
            }
        }
        public void StopFiring()
        {
            IsFiring = false;
            FireToken?.Cancel();
        }

        protected abstract void FireBehaviour(CancellationToken token);

        public Weapon(float damage, float rateOfFire, string projectileName, string impactVFXName, float impactStrength, string impactTextureName, float impactSize, Material blitMaterial, Transform firingPiece)
        {
            Damage = damage;
            RateOfFire = rateOfFire;
            if (!Management.DataManager.Prefabs.TryGetValue(projectileName, out Projectile))
            {
                throw new ArgumentException($"'{projectileName}' does not match a loaded GameObject");
            }
            if (!Management.DataManager.Prefabs.TryGetValue(impactVFXName, out ImpactVFX))
            {
                throw new ArgumentException($"'{impactVFXName}' does not match a loaded GameObject");
            }
            ImpactStrength = impactStrength;
            if (!Management.DataManager.Textures.TryGetValue(impactTextureName, out ImpactTexture))
            {
                throw new ArgumentException($"'{impactTextureName}' does not match a loaded Texture2D");
            }
            ImpactSize = impactSize;
            BlitMaterial = blitMaterial;
            FiringPiece = firingPiece;
        }

        public Weapon(JsonConstructors.WeaponConstructor data, Material blitMaterial, Transform firingPiece)
        {
            Damage = data.Damage;
            RateOfFire = data.RateOfFire;
            ImpactStrength = data.ImpactStrength;
            ImpactSize = data.ImpactSize;
            if (!Management.DataManager.Prefabs.TryGetValue(data.Projectile, out Projectile))
            {
                throw new ArgumentException($"'{data.Projectile}' does not match a loaded GameObject");
            }
            if (!Management.DataManager.Prefabs.TryGetValue(data.ImpactVFX, out ImpactVFX))
            {
                throw new ArgumentException($"'{data.ImpactVFX}' does not match a loaded GameObject");
            }
            if (!Management.DataManager.Textures.TryGetValue(data.ImpactTexture, out ImpactTexture))
            {
                throw new ArgumentException($"'{data.ImpactTexture}' does not match a loaded Texture2D");
            }

            BlitMaterial = blitMaterial;
            FiringPiece = firingPiece;
        }
    }
}

namespace JsonConstructors
{
    [Serializable]
    public class WeaponConstructor
    {
        public float Damage, RateOfFire, ImpactStrength, ImpactSize;
        public string Projectile, ImpactVFX, ImpactTexture, Name;
        [JsonProperty]
        string SWeaponType;
        public Type WeaponType;

        [OnDeserialized()]
        void OnDeserialized(StreamingContext context)
        {
            WeaponType = Type.GetType("Entities.Parts.Weapons." + SWeaponType);
        }

        public Weapon CreateWeapon(Material blitMaterial, Transform firingPiece)
        {
            Weapon weapon =  (Weapon)Activator.CreateInstance(WeaponType, this, blitMaterial, firingPiece);
            weapon.Init();
            return weapon;
        }
    }
}
