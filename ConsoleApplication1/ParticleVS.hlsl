#include "Particle.hlsl" 
// Access to the particle buffer 
StructuredBuffer<Particle> particles : register(t0);

cbuffer PerObject : register(b2)
{
    // WorldViewProjection matrix
    float4x4 WorldViewProjection;
    float4 CameraPosition;
};

// Computes the vertex position
float4 ComputePosition(in float3 pos, in float size, in float2 vPos)
{
     // Create billboard (quad always facing the camera)   
    float3 toEye = normalize(CameraPosition.xyz - pos);
    float3 up = float3(0.0f, 1.0f, 0.0f);
    float3 right = cross(toEye, up);
    up = cross(toEye, right);
    pos += (right * size * vPos.x) + (up * size * vPos.y);
    return mul(float4(pos, 1), WorldViewProjection);
}

PS_Input VSMain(in uint vertexID : SV_VertexID, in uint instanceID : SV_InstanceID)
{
    PS_Input result = (PS_Input) 0;
    // Load particle using instance Id 
    Particle p = particles[instanceID];
     // 0-1 Vertex strip layout   
 //  /    
  // 2-3  
    result.UV = float2(vertexID & 1, (vertexID & 2) >> 1);
    result.Position = ComputePosition(p.Position, p.Radius, result.UV * float2(2, -2) + float2(-1, 1));
    result.Energy = p.Energy;
    return result;
}
