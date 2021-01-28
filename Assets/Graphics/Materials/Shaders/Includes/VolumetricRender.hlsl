//#ifndef GETLIGHT_INCLUDED
//#define GETLIGHT_INCLUDED
//#include "../../../../Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
//#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightDefinition.cs.hlsl"
//#include "../../../../Library/PackageCache/com.unity.render-pipelines.high-definition@7.1.5/Runtime/Lighting/LightDefinition.cs.hlsl"

float3 Centre;
float3 Radius;

void ReturnColourTest_float(out float4 Colour)
{
    Colour = float4(1, 0, 0, 1);
}

float GetDistance(float3 Position)
{
    return distance(Position, Centre) - Radius;
}

void SimpleLambert_float(float3 normal, float3 BaseColour, float3 LightColour, float3 lightDirection, float Alpha, float3 ViewDirection, float Specular, float Gloss, out float3 Colour, out float AlphaOut)
{
    if (Alpha == 0)
    {
        Colour = (0, 0, 0, 0);
        return;
    }
    //float3 LightDirection = normalize(WorldLightPos.xyz);
    
    float3 h = (-lightDirection - ViewDirection) / 2;
    float SpecularLight = pow(clamp(dot(normal, h), 0, 1), Specular) * Gloss;
    
    float NdotL = max(dot(normal, -lightDirection), 0);
    Colour.rgb = BaseColour * LightColour * NdotL + SpecularLight;
    AlphaOut = Alpha;
    return;
}

void GetNormal_float(float3 Position, float Alpha, out float3 normal)
{
    const float NormalDistance = 0.01;
    if (Alpha == 0)
    {
        normal = (0, 0, 0);
        return;
    }
    normal = normalize(float3
	(
		GetDistance(Position + float3(NormalDistance, 0, 0)) - GetDistance(Position - float3(NormalDistance, 0, 0)),
		GetDistance(Position + float3(0, NormalDistance, 0)) - GetDistance(Position - float3(0, NormalDistance, 0)),
		GetDistance(Position + float3(0, 0, NormalDistance)) - GetDistance(Position - float3(0, 0, NormalDistance))
	));
    return;
}

void RenderSurface(float3 Position, float3 BaseColour)
{
    
}

#define STEPS 128
#define MIN_DISTANCE 0.01
void RayMarch_float(float3 hitPosition, float3 viewDirection, float3 centre, float radius, out float3 surfacePosition, out float ambientOcclusion, out float Alpha)
{
    Centre = centre;
    Radius = radius;
    surfacePosition = float3(0, 0, 0);
    Alpha = 0;
    ambientOcclusion = 0;
    for (int i = 0; i < STEPS; i++)
    {
        float distance = GetDistance(hitPosition);
        if (distance < MIN_DISTANCE)
        {
            Alpha = 1;
            surfacePosition = hitPosition;
            ambientOcclusion = 1 - float(i) / float(STEPS - 1);
            return;
        }
        hitPosition += distance * normalize(-viewDirection);
    }
    return;
    
}

