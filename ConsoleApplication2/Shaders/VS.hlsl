struct Particle // описание структуры на GPU
{
    float3 Position;
    float3 Velocity;
};
// т.к. вертексов у нас нет, мы можем получить текущий ID вертекса при рисовании без использования Vertex Buffer
struct VertexInput
{
    uint VertexID : SV_VertexID;
};
struct PixelInput // описывает вертекс на выходе из Vertex Shader
{
    float4 Position : SV_POSITION;
    float2 UV : TEXCOORD;
};

cbuffer Params : register(b0) // матрицы вида и проекции
{
    float4x4 World;
    float4x4 View;
    float4x4 Projection;
    float Size;
};

StructuredBuffer<Particle> Particles : register(t0); // буфер частиц

PixelInput VS(VertexInput input)
{
    PixelInput output = (PixelInput) 0;

    Particle particle = Particles[input.VertexID];

    float4 worldPosition = mul(float4(particle.Position, 1), World);
    float4 viewPosition = mul(worldPosition, View);
    output.Position = viewPosition;
    output.UV = 0;

    return output;
}