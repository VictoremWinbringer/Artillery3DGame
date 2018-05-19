#include "Particle.hlsl" 
// Append and consume buffers for particles
AppendStructuredBuffer<Particle> NewState : register(u0);
ConsumeStructuredBuffer<Particle> CurrentState : register(u1);

// Apply ForceDirection with ForceStrength to particle
void ApplyForces(inout Particle particle)
{
     // Forces  
    float3 force = (float3) 0;
     // Directional force 
    force += normalize(ForceDirection) * ForceStrength;
       // Damping 
    float windResist = 0.9;
    force *= windResist;
    particle.OldPosition = particle.Position;
       // Integration step  
    particle.Position += force * FrameTime;
}

// Random Number Generator methods 
uint rand_lcg(inout uint rng_state)
{
     // Linear congruential generator  
    rng_state = 1664525 * rng_state + 1013904223;
    return rng_state;
}

uint wang_hash(uint seed)
{
    // Initialize a random seed   
    seed = (seed ^ 61) ^ (seed >> 16);
    seed *= 9;
    seed = seed ^ (seed >> 4);
    seed *= 0x27d4eb2d;
    seed = seed ^ (seed >> 15);
    return seed;
}

[numthreads(THREADSX, 1, 1)]
void Generator(uint groupIndex : SV_GroupIndex,
 uint3 groupId : SV_GroupID,
uint3 groupThreadId : SV_GroupThreadID,
uint3 threadId : SV_DispatchThreadID)
{
    uint indx = threadId.x + threadId.y * THREADSX;
    Particle p = (Particle) 0;
    // Initialize random seed 
    uint rng_state = wang_hash(RandomSeed + indx);
     // Random float between [0, 1]   
    float f0 = float(rand_lcg(rng_state)) * (1.0 / 4294967296.0);
    float f1 = float(rand_lcg(rng_state)) * (1.0 / 4294967296.0);
    float f2 = float(rand_lcg(rng_state)) * (1.0 / 4294967296.0);
     // Set properties of new particle  
    p.Radius = Radius;
    p.Position.x = DomainBoundsMin.x + f0 * ((DomainBoundsMax.x - DomainBoundsMin.x) + 1);
    p.Position.z = DomainBoundsMin.z + f1 * ((DomainBoundsMax.z - DomainBoundsMin.z) + 1);
    p.Position.y = (DomainBoundsMax.y - 6) + f2 * ((DomainBoundsMax.y - (DomainBoundsMax.y - 6)) + 1);
    p.OldPosition = p.Position;
    p.Energy = MaxLifetime;
  // Append the new particle to the output buffer  
    NewState.Append(p);
}

[numthreads(THREADSX, THREADSY, 1)]
void Snowfall(uint groupIndex : SV_GroupIndex,
 uint3 groupId : SV_GroupID,
 uint3 groupThreadId : SV_GroupThreadID,
 uint3 threadId : SV_DispatchThreadID)
{
    uint indx = threadId.x + threadId.y * THREADSX;
    // Skip out of bounds threads 
    if (indx >= ParticleCount)
        return;
      // Load/Consume particle 
    Particle p = CurrentState.Consume();
    ApplyForces(p);
    // Ensure the particle does not fall endlessly  
    p.Position.y = max(p.Position.y, DomainBoundsMin.y);
    // Count down time to live 
    p.Energy -= FrameTime;
    // If no longer falling only let sit for a second
    if (p.Position.y == p.OldPosition.y && p.Energy > 1.0f)
        p.Energy = 1.0f;
    if (p.Energy > 0)
    { // If particle is alive add back to append buffer  
        NewState.Append(p);
    }
}
