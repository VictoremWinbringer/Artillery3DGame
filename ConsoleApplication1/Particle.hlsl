cbuffer ParticleConstants : register(b0)
{
    float3 DomainBoundsMin;
    float ForceStrength;
    float3 DomainBoundsMax;
    float MaxLifetime;
    float3 ForceDirection;
    uint MaxParticles;
    float3 Attractor;
    float Radius;
};
 // Particles per frame constant buffer
cbuffer ParticleFrame : register(b1)
{
    float Time;
    float FrameTime;
    uint RandomSeed;
    uint ParticleCount; // consume buffer count 
}
 // Represents a single particle 
struct Particle
{
    float3 Position;
    float Radius;
    float3 OldPosition;
    float Energy;
};
// Pixel shader input
struct PS_Input
{
    float4 Position : SV_Position;
    float2 UV : TEXCOORD0;
    float Energy : ENERGY;
};