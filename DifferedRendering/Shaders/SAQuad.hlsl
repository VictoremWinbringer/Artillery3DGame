struct VertexIn
{
    float4 Position : SV_Position;
};

struct PixelIn
{
    float4 Position : SV_Position;
    float2 UV : TEXCOORD0;
};

PixelIn VSMain(VertexIn vertex)
{
    PixelIn result = (PixelIn) 0;
    result.Position = vertex.Position;
    result.Position.w = 1.0f;
    result.UV.x = result.Position.x * 0.5 + 0.5;
    result.UV.y = result.Position.y * -0.5 + 0.5;
    return result;
}

Texture2DMS<float4> TextureMS0 : register(t0);
Texture2D<float4> Texture0 : register(t0);
SamplerState Sampler : register(s0);


float4 PSMain(PixelIn input) : SV_Target
{
    return Texture0.Sample(Sampler, input.UV);
}

float4 PSMainMultisample(PixelIn input, uint sampleIndex : SV_SampleIndex) : SV_Target
{
    int2 screenPos = int2(input.Position.xy);
    return TextureMS0.Load(screenPos, sampleIndex);
}