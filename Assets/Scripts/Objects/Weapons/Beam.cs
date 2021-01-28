using Interfaces;
using JsonConstructors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityAsync;
using UnityEngine;
using UnityEngine.VFX;
using static UnityEngine.Object;

namespace Entities.Parts.Weapons
{
    public class Beam : Weapon
    {
        public class LaserProjectile
        {
            public LineRenderer Beam;
            public VisualEffect ImpactVFX;
            public RaycastHit HitInfo;
            public Collider oldCollider = null;
            public IDamageable Damageable = null;
            public bool IsDisabled;
        }


        protected override async void FireBehaviour(CancellationToken token)
        {
            if (Projectile == null) throw new ArgumentNullException($"No projectile for weapon on controller {Controller.gameObject.name}");
            LaserProjectile laser = new LaserProjectile();

            laser.Beam = Instantiate(Projectile).GetComponent<LineRenderer>();
            laser.ImpactVFX = Instantiate(ImpactVFX).GetComponent<VisualEffect>();


            while (!token.IsCancellationRequested)
            {
                if (!Physics.Raycast(FiringPiece.position, FiringPiece.forward, out laser.HitInfo))
                {
                    //laser.Beam.gameObject.SetActive(false);
                    laser.Beam.SetPositions(new Vector3[] { FiringPiece.position, FiringPiece.position + FiringPiece.forward * 100f });
                    laser.Beam.endColor = new Color(laser.Beam.endColor.r, laser.Beam.endColor.g, laser.Beam.endColor.b, 0.0f);
                    laser.ImpactVFX.gameObject.SetActive(false);
                    laser.IsDisabled = true;
                    await Await.NextUpdate();
                    continue;
                }

                if (laser.IsDisabled)
                {
                    //laser.Beam.gameObject.SetActive(true);
                    laser.ImpactVFX.gameObject.SetActive(true);
                    laser.Beam.endColor = new Color(laser.Beam.endColor.r, laser.Beam.endColor.g, laser.Beam.endColor.b, 1.0f);
                }

                if (laser.oldCollider == null || laser.HitInfo.collider != laser.oldCollider)
                {
                    laser.HitInfo.collider.TryGetComponentInParent(out laser.Damageable);
                    laser.oldCollider = laser.HitInfo.collider;
                }

                laser.Beam.SetPositions(new Vector3[] { FiringPiece.position, laser.HitInfo.point });
                laser.ImpactVFX.transform.position = laser.HitInfo.point;
                laser.ImpactVFX.transform.rotation = Quaternion.LookRotation(-FiringPiece.forward);

                if (laser.Damageable is Entity entity && entity.AdditionalTags.HasFlag(Entity.TagTypes.RecieveDecals) && BlitMaterial != null)
                {
                    BlitMaterial.SetFloat("Scale", ImpactSize);
                    BlitMaterial.SetVector("UVHit", laser.HitInfo.textureCoord);
                    //DecalBlitInstance.SetColor("_DecalColour")
                    entity.RegisterHit(laser.HitInfo.point, ImpactStrength, ImpactTexture, BlitMaterial);
                }

                if (IsReadyToFire)
                {
                    if (laser.Damageable != null)
                    {
                        laser.Damageable.Damage(Damage, "");
                    }
                    IsReadyToFire = false;
                    Cooldown.Start();
                }
                await Await.NextUpdate();
            }
            Destroy(laser.Beam.gameObject);
            Destroy(laser.ImpactVFX.gameObject);
        }

        public Beam(float damage, float rateOfFire, string projectileName, string impactVFXName, float impactStrength, string impactTextureName, float impactSize, Material blitMaterial, Transform firingPiece) : base(damage, rateOfFire, projectileName, impactVFXName, impactStrength, impactTextureName, impactSize, blitMaterial, firingPiece)
        {

        }

        public Beam(WeaponConstructor data, Material blitMaterial, Transform firingPiece) : base(data, blitMaterial, firingPiece)
        {

        }
    }
}
