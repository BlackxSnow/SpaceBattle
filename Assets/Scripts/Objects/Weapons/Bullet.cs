using Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.VFX;

namespace Entities.Parts.Weapons
{
    public class Bullet : MonoBehaviour
    {
        public Transform Owner;
        //Stats
        public float Damage;
        public float Speed;
        public float MaxRange;
        public string DamageType;

        //Graphical
        public Material BlitMaterial;
        public Texture ImpactTexture;
        public GameObject ImpactVFX;
        public VisualEffect SourceVFX;
        public float ImpactSize;
        public float ImpactStrength;

        protected void Update()
        {
            transform.Translate(Vector3.forward * Speed * Time.deltaTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.transform.IsChildOf(Owner) || other.transform == Owner)
            {
                return;
            }

            SourceVFX.SetVector3("KillSphere_Pos", transform.position + Vector3.forward * Speed * Time.deltaTime);
            SourceVFX.SetFloat("KillSphere_Radius", 1f);
            Ray ray = new Ray(transform.position - transform.forward * Speed * Time.deltaTime, transform.forward);
            Physics.Raycast(ray, out RaycastHit hit);
            GameObject vfx = Instantiate(ImpactVFX, hit.point, Quaternion.LookRotation(hit.normal));
            Destroy(vfx, 5);
            if (other.TryGetComponentInParent(out IDamageable damageable))
            {
                damageable.Damage(Damage, DamageType);

                if (damageable is Entity entity && entity.AdditionalTags.HasFlag(Entity.TagTypes.RecieveDecals) && BlitMaterial != null)
                {
                    BlitMaterial.SetFloat("Scale", ImpactSize);
                    BlitMaterial.SetVector("UVHit", hit.textureCoord);
                    //DecalBlitInstance.SetColor("_DecalColour")
                    entity.RegisterHit(hit.point, ImpactStrength, ImpactTexture, BlitMaterial);
                }
            }
            //SourceVFX.SetVector3("KillSphere_Pos", new Vector3(0,0,0));
            //SourceVFX.SetFloat("KillSphere_Radius", 0);
            Destroy(gameObject);
        }

        //private void OnCollisionEnter(Collision collision)
        //{
        //    if (collision.collider.TryGetComponent(out IDamageable damageable))
        //    {
        //        ContactPoint contact = collision.GetContact(0);
        //        damageable.Damage(Damage, DamageType);

        //        GameObject vfx = Instantiate(ImpactVFX, contact.point, Quaternion.LookRotation(contact.normal));
        //        //vfx.GetComponent<UnityEngine.VFX.VisualEffect>()
        //        Destroy(vfx, 5);
        //        //TODO figure out how to extract the duration of a visual effect for destruction.

        //        if (damageable is Entity entity && entity.AdditionalTags.HasFlag(Entity.TagTypes.RecieveDecals) && BlitMaterial != null)
        //        {

        //            Ray ray = new Ray(contact.point - contact.normal * 0.1f * 0.5f, contact.normal);
        //            Physics.Raycast(ray, out RaycastHit hit, 0.1f);
        //            BlitMaterial.SetFloat("Scale", ImpactSize);
        //            BlitMaterial.SetVector("UVHit", hit.textureCoord);
        //            //DecalBlitInstance.SetColor("_DecalColour")
        //            entity.RegisterHit(hit.point, ImpactStrength, ImpactTexture, BlitMaterial);
        //        }
        //    }
        //    Destroy(gameObject);
        //}
        private void OnCollisionEnter(Collision collision)
        {
            if (collision.collider.transform.IsChildOf(Owner) || collision.collider.transform == Owner)
            {
                return;
            }

            SourceVFX.SetVector3("KillSphere_Pos", transform.position + transform.forward * Speed * Time.deltaTime);
            SourceVFX.SetFloat("KillSphere_Radius", 1f);
            ContactPoint hit = collision.contacts[0];
            Ray ray = new Ray(hit.point + hit.normal * Speed * Time.deltaTime, -hit.normal);
            Physics.Raycast(ray, out RaycastHit rayHit);
            Debug.DrawRay(hit.point + hit.normal * Speed * Time.deltaTime, -hit.normal, Color.red, 5f);
            GameObject vfx = Instantiate(ImpactVFX, hit.point, Quaternion.LookRotation(collision.contacts[0].normal));
            Destroy(vfx, 5);
            if (collision.collider.TryGetComponentInParent(out IDamageable damageable))
            {
                damageable.Damage(Damage, DamageType);

                if (damageable is Entity entity && entity.AdditionalTags.HasFlag(Entity.TagTypes.RecieveDecals) && BlitMaterial != null)
                {
                    BlitMaterial.SetFloat("Scale", ImpactSize);
                    BlitMaterial.SetVector("UVHit", rayHit.textureCoord);
                    //DecalBlitInstance.SetColor("_DecalColour")
                    entity.RegisterHit(hit.point, ImpactStrength, ImpactTexture, BlitMaterial);
                }
            }
            //SourceVFX.SetVector3("KillSphere_Pos", new Vector3(0,0,0));
            //SourceVFX.SetFloat("KillSphere_Radius", 0);
            Destroy(gameObject);
        }
    }
}
