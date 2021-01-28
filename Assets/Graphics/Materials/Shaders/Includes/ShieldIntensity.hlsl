int PointCount;
float4 Points[128];
float Strengths[128];

void GetIntensity_float(float3 worldPos, out float intensity)
{
    intensity = 0;
    for (int i = 0; i < PointCount; i++)
    {
        float dist = distance(Points[i].xyz, worldPos);
        intensity += Strengths[i] / pow(dist, 2);
    }
}