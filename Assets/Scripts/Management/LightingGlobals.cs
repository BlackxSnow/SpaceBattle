using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

[ExecuteInEditMode]
public class LightingGlobals : MonoBehaviour
{
    public Light MainLight;
    // Start is called before the first frame update
    void Start()
    {
        Shader.SetGlobalVector("_MainLightDirection", MainLight.transform.forward);
        Shader.SetGlobalFloat("_MainLightAttenuation", MainLight.range);
        Shader.SetGlobalColor("_MainLightColor", MainLight.color);
    }
}
