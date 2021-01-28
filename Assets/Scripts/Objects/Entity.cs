﻿using Data;
using Interfaces;
using System;
using System.Collections.Generic;
using UnityAsync;
using UnityEngine;
using System.Linq;

public abstract class Entity : MonoBehaviour, IDamageable
{
    [Flags]
    public enum TagTypes
    {
        RecieveDecals = 1 << 0,
        RecieveHitEffects = 1 << 1
    }

    public HealthData Health { get; set; } = new HealthData(100, 100);
    public int Team;

    [SerializeField]
    protected bool UseTextureGradient;
    [SerializeField]
    protected bool UseBaseGradient;
    [GradientUsage(true)]
    public Gradient ShieldTextureGradient;
    public Gradient ShieldBaseGradient;


    protected Renderer Renderer;
    protected bool HasDecalChanged = false;
    [HideInInspector]
    public float UVPerUnit;
    protected RenderTexture _DecalMap;
    protected (List<Vector4> points, List<float> strengths) ShieldHits;
    public RenderTexture DecalMap { 
        get
        {
            HasDecalChanged = true;
            return _DecalMap;
        } 
        set {
            _DecalMap = value;
            HasDecalChanged = true;
        }
    }

    public Vector3[] AimBounds;
    public Transform[] AimPoints;
    public GameObject[] Shields;
    protected Material[] ShieldMaterials;
    public TagTypes AdditionalTags;

    protected virtual void Awake()
    {
        Renderer = GetComponent<Renderer>();
        DecalMap = new RenderTexture(2048, 2048, 0);
        ShieldHits.points = new List<Vector4>();
        ShieldHits.strengths = new List<float>();
        ShieldMaterials = new Material[Shields.Length];
        for (int i = 0; i < Shields.Length; i++)
        {
            ShieldMaterials[i] = Shields[i].GetComponent<Renderer>().material;
        }
        foreach (Material shieldMat in ShieldMaterials)
        {
            shieldMat.SetVectorArray("Points", new Vector4[128]);
            shieldMat.SetFloatArray("Strengths", new float[128]);
        }

        GameObject aimBoundsObject = transform.Find("AimBounds")?.gameObject;
        if (aimBoundsObject)
        {
            AimBounds = aimBoundsObject.GetComponent<MeshFilter>().mesh.vertices;
        }
        Transform aimPointsContainer = transform.Find("AimPoints");
        if (aimPointsContainer)
        {
            AimPoints = Enumerable.Concat(aimPointsContainer.FindChildren("point"), new Transform[] { transform }).ToArray(); 
        }

        GetUVSize();
    }

    protected virtual void GetUVSize()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] verts = mesh.vertices;
        Vector2[] uvs = mesh.uv;
        float vertDistance = Vector3.Distance(Vector3.Scale(verts[0], transform.localScale), Vector3.Scale(verts[1], transform.localScale));
        float uvDistance = Vector2.Distance(uvs[0], uvs[1]);
        UVPerUnit = uvDistance / vertDistance;
    }

    public virtual void RegisterHit(Vector3 hitPosition, float strength, Texture decalTexture, Material blitMaterial)
    {
        if (AdditionalTags.HasFlag(TagTypes.RecieveHitEffects))
        {
            if (Health.HasShield)
            {
                ShieldHits.points.Add(hitPosition);
                ShieldHits.strengths.Add(strength);
            }
            else if (AdditionalTags.HasFlag(TagTypes.RecieveDecals))
            {
                RenderTexture originalMap = RenderTexture.GetTemporary(DecalMap.descriptor);
                Graphics.Blit(DecalMap, originalMap);
                blitMaterial.SetFloat("UVperUnit", UVPerUnit);
                blitMaterial.SetTexture("_OriginalMap", originalMap);

                Graphics.Blit(decalTexture, DecalMap, blitMaterial);
                originalMap.Release();
            }
        }
    }
    protected virtual void Update()
    {
        if (HasDecalChanged)
        {
            foreach (Material mat in Renderer.materials)
            {
                mat.SetTexture("DecalMap", _DecalMap);
            }
            HasDecalChanged = false;
        }
        if (Health.HasShield)
        {
            DissolveToTarget(); 
        }
    }

    protected virtual void LateUpdate()
    {
        if (ShieldMaterials.Length == 0) return;
        foreach (Material shieldMat in ShieldMaterials)
        {
            shieldMat.SetInt("PointCount", ShieldHits.points.Count);
            //ShieldMaterial.SetInt("StrengthCount", ShieldHits.strengths.Count);

            if (ShieldHits.points.Count > 0)
            {
                shieldMat.SetVectorArray("Points", ShieldHits.points);
                shieldMat.SetFloatArray("Strengths", ShieldHits.strengths);
            } 
        }

        ShieldHits.points.Clear();
        ShieldHits.strengths.Clear();
    }
    float CurrentDissolve = 0;
    float TargetDissolve = 0;
    public void Damage(float amount, string damageType)
    {
        if(Health.HasShield)
        {
            Health.Shield -= amount;

            if (!Health.HasShield)
            {
                DeactivateShields();
            }

            float shieldIntegrity = Health.Shield / Health.MaxShield;

            foreach (Material mat in ShieldMaterials)
            {
                if (UseBaseGradient)
                {
                    mat.SetColor("BaseCol", ShieldBaseGradient.Evaluate(shieldIntegrity));
                }
                if (UseTextureGradient)
                {
                    mat.SetColor("TexCol", ShieldTextureGradient.Evaluate(shieldIntegrity));
                }
                const float dissolveThreshold = 0.5f;
                if(shieldIntegrity <= dissolveThreshold)
                {
                    //mat.SetFloat("DissolveSpeed", Mathf.Lerp(0, 0.4f, (dissolveThreshold - shieldIntegrity) * (1 / dissolveThreshold)));
                    mat.SetFloat("DissolveSpeed", 0.25f);
                    TargetDissolve = Mathf.Lerp(0, 0.2f, (dissolveThreshold - shieldIntegrity) * (1 / dissolveThreshold));
                }
                else
                {
                    mat.SetFloat("DissolveSpeed", 0);
                    mat.SetFloat("DissolveAmount", 0);
                }
            }
        }
        else
        {
            Health.Hull -= amount;
            if (Health.IsDead)
            {
                Die();
            }
        }
    }
    const float DissolveDelta = 0.25f;
    protected virtual void DissolveToTarget()
    {
        float deltaPerSecond = DissolveDelta * Time.deltaTime;
        float delta = Mathf.Clamp(TargetDissolve - CurrentDissolve, -deltaPerSecond, deltaPerSecond);
        CurrentDissolve += delta;
        foreach(Material mat in ShieldMaterials)
        {
            mat.SetFloat("DissolveAmount", CurrentDissolve);
        }
    }

    protected async virtual void DeactivateShields()
    {
        float t = CurrentDissolve;

        bool colliderDisabled = false;
        while (t < 1)
        {
            t += Time.deltaTime * 0.2f;
            foreach(Material mat in ShieldMaterials)
            {
                mat.SetFloat("DissolveAmount", Mathf.Lerp(0, 1, t));
            }
            if (t >= 0.5f && !colliderDisabled)
            {
                foreach (GameObject shield in Shields)
                {
                    shield.GetComponent<Collider>().enabled = false;
                }
                colliderDisabled = true;
            }
            await Await.NextUpdate();
        }
        foreach(GameObject shield in Shields)
        {
            shield.SetActive(false);
        }
    }

    protected async virtual void ReactivateShields()
    {
        float t = 0;
        foreach (GameObject shield in Shields)
        {
            shield.SetActive(true);
        }
        bool colliderEnabled = false;
        while (t > 0)
        {
            t += Time.deltaTime * 0.25f;
            foreach (Material mat in ShieldMaterials)
            {
                mat.SetFloat("DissolveAmount", Mathf.SmoothStep(0, 1, t));
            }
            if (t >= 0.5f && !colliderEnabled)
            {
                foreach (GameObject shield in Shields)
                {
                    shield.GetComponent<Collider>().enabled = true;
                }
                colliderEnabled = true;
            }
            await Await.NextUpdate();
        }
        foreach (GameObject shield in Shields)
        {
            shield.GetComponent<Collider>().enabled = true;
        }
    }

    protected virtual void Die()
    {
        Destroy(Instantiate(Management.DataManager.Prefabs["Explosion_Debris01"], transform.position, Quaternion.identity), 15);
        Destroy(this.gameObject);
    }

}