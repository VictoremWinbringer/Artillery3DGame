#define THREAD_GROUP_X 32
#define THREAD_GROUP_Y 24
#define THREAD_GROUP_TOTAL 768

struct Particle
{
    float3 Position;
    float3 Velocity;
};

cbuffer constants : register(b0)
{
    int GroupDim;
    uint MaxParticles;
    float DeltaTime;
    float3 Attractor;
};

RWStructuredBuffer<Particle> Particles : register(u0);

float3 _calculate(float3 anchor, float3 position)
{
    float3 direction = anchor - position;
    float distance = length(direction);
    direction /= distance;

    return direction * max(0.01, (1 / (distance * distance)));
}

[numthreads(THREAD_GROUP_X, THREAD_GROUP_Y, 1)]
void CS(uint3 groupID : SV_GroupID, uint groupIndex : SV_GroupIndex)
{
    uint index = groupID.x * THREAD_GROUP_TOTAL + groupID.y * GroupDim * THREAD_GROUP_TOTAL + groupIndex;
	
	[flatten]
    if (index >= MaxParticles)
        return;

    Particle particle = Particles[index];

    float3 position = particle.Position;
    float3 velocity = particle.Velocity;
    velocity += _calculate(Attractor, position) ;
    velocity += _calculate(-Attractor, position);
    particle.Position = position + velocity * DeltaTime;
    particle.Velocity = velocity;
   
    Particles[index] = particle;
}