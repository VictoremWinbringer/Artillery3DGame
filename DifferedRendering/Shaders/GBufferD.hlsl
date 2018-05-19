cbuffer data : register(b0)
{
    float4x4 InverseProjection;

}
struct GBufferAttributes
{
    float3 Position;
    float3 Normal;
    float3 Diffuse;
    float SpecularInt;
    float3 Emissive;
    float SpecularPower;
};

struct PixelIn
{
    float4 Position : SV_Position;
    float2 UV : TEXCOORD0;
};

float3 DecodeAzimuthal(in float2 enc)
{
    float2 fenc = enc * 4 - 2;
    float f = dot(fenc, fenc);
    float g = sqrt(1 - f / 4);
    float3 n;
    n.xy = fenc * g;
    n.z = 1 - f / 2;
    return n;
}
float3 UnpackNormal(in uint packedN)
{
    float2 unpack;
    unpack.x = f16tof32(packedN);
    unpack.y = f16tof32(packedN >> 16);
    return DecodeAzimuthal(unpack);
}
void ExtractGBufferAttributes(in PixelIn pixel, in Texture2D<float4> t0, in Texture2D<uint> t1, in Texture2D<float4> t2, in Texture2D<float> t3, out GBufferAttributes attrs)
{
    int3 screenPos = int3(pixel.Position.xy, 0);
    attrs.Diffuse = t0.Load(screenPos).xyz;
    attrs.SpecularInt = t0.Load(screenPos).w;
    attrs.Normal = UnpackNormal(t1.Load(screenPos));
    attrs.Emissive = t2.Load(screenPos).xyz;
    attrs.SpecularPower = t2.Load(screenPos).w * 50;
    float depth = t3.Load(screenPos);
    float x = pixel.UV.x * 2 - 1;
    float y = (1 - pixel.UV.y) * 2 - 1;
    float4 posVS = mul(float4(x, y, depth, 1.0f), InverseProjection);
    attrs.Position = posVS.xyz / posVS.w;
}

Texture2D<float4> Texture0 : register(t0);
Texture2D<uint> Texture1 : register(t1);
Texture2D<float4> Texture2 : register(t2);
Texture2D<float> TextureDepth : register(t3);

float4 PS_GBufferNormal(PixelIn pixel) : SV_Target
{
    GBufferAttributes attrs;
    ExtractGBufferAttributes(pixel, Texture0, Texture1, Texture2, TextureDepth, attrs);
    return float4(attrs.Normal, 1);
}