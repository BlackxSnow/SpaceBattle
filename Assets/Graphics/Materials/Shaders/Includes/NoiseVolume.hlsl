#include "Noise3D.hlsl"

float3 Centre;
float3 Radius;

float GetDistance(float3 Position)
{
    return distance(Position, Centre) - Radius;
}

#define STEPS 128
#define MIN_DISTANCE 0.01

void RayMarch_float(float3 hitPosition, float3 viewDirection, float3 centre, float radius, float thickness, float scale, float time, int octaves, float persistance, float lunacrity, out float Alpha)
{
    Centre = centre;
    Radius = radius;
    float stepSize = radius * 2 / (STEPS * 0.9);
    Alpha = 0;
    float noiseValue = 0;
    float impact;
    for (int i = 0; i < STEPS; i++)
    {
        float distance = GetDistance(hitPosition);
        if (distance < MIN_DISTANCE)
        {
            octaveNoise_float(hitPosition, octaves, persistance, lunacrity, scale, time, noiseValue);
            impact = clamp(abs(distance / radius) + 0.2, 0, 1);
            Alpha += clamp(noiseValue * impact, 0, 1);
            hitPosition += stepSize * normalize(-viewDirection);
            continue;
        }
        hitPosition += distance * normalize(-viewDirection);
    }
    
    Alpha = Alpha / (STEPS * 0.9) * thickness;
    
    return;
}

