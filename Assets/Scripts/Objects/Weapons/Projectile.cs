using JsonConstructors;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityAsync;
using UnityEngine;
using static UnityEngine.Object;

namespace Entities.Parts.Weapons
{
    public class Projectile : Weapon
    {


        protected override async void FireBehaviour(CancellationToken token)
        {
            while(!token.IsCancellationRequested)
            {
                if (IsReadyToFire)
                {
                    Quaternion spreadRot = FiringPiece.rotation * 
                        Quaternion.AngleAxis(Random.Range(-ProjectileSpread/2, ProjectileSpread/2), FiringPiece.up) * 
                        Quaternion.AngleAxis(Random.Range(-ProjectileSpread / 2, ProjectileSpread / 2), FiringPiece.right);
                    GameObject projectile = Instantiate(Projectile, FiringPiece.position, spreadRot);
                    Bullet bullet = projectile.AddComponent<Bullet>();
                    if (ProjectileVFX)
                    {
                        ProjectileVFX.SetVector3("Scale", ProjectileScale.ToVector3());
                        ProjectileVFX.SetVector3("InitialPosition", FiringPiece.position);
                        ProjectileVFX.SetVector3("InitialVelocity", FiringPiece.forward * ProjectileSpeed);
                        ProjectileVFX.SendEvent("SpawnSingle");
                    }
                    SetBulletStats(bullet);


                    IsReadyToFire = false;
                    Cooldown.Start();
                    await Cooldown.CompletionSource.Task;
                    continue;
                }
                await Await.NextUpdate();
            }
        }

        protected void SetBulletStats(Bullet bullet)
        {
            bullet.Damage = Damage;
            bullet.Speed = ProjectileSpeed;
            bullet.MaxRange = Controller.MaxRange;
            bullet.DamageType = DamageType;
            bullet.BlitMaterial = BlitMaterial;
            bullet.ImpactSize = ImpactSize;
            bullet.ImpactStrength = ImpactStrength;
            bullet.ImpactTexture = ImpactTexture;
            bullet.Owner = Controller.Owner.transform;
            bullet.ImpactVFX = ImpactVFX;
            bullet.transform.localScale = new Vector3(ProjectileScale, ProjectileScale, ProjectileScale);
            bullet.SourceVFX = ProjectileVFX;
        }

        public Projectile(WeaponConstructor data, Transform firingPiece, WeaponController controller) : base(data, firingPiece, controller)
        {

        }
    } 
}
