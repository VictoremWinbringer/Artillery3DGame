struct PixelInput // описывает вертекс на выходе из Vertex Shader
{
    float4 Position : SV_POSITION;
    float2 UV : TEXCOORD;
};

Texture2D ParticleTexture : register(t0);
SamplerState ParticleSampler : register(s0);

float4 PS(PixelInput input) : SV_Target0
{
    
    float4 Color = ParticleTexture.Sample(ParticleSampler, input.UV);
   // Color.b = 0;
    return Color;
}