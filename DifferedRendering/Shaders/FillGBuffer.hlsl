
cbuffer PerObject : register(b0)
{
    float4x4 WorldViewProjection;
    float4x4 World;
    float4x4 View;
    bool HasTexture;
    bool HasNormalMap;
    bool HasSpecMap;
};

static const float4 MaterialSpecular = float4(1, 1, 1, 1);
static const float MaterialSpecularPower = 24;
static const float4 MaterialEmissive = float4(0, 0, 0, 1);


struct VertexShaderInput
{
    float4 Position : POSITION;
    float3 Normal : NORMAL;
    float2 TextureUV : TEXCOORD0;
    float4 Tangent : TANGENT; 
    float4 BiTangent : BINORMAL;
};


float3 ApplyNormalMap(float3 normal, float4 tangent, float3 normalSample)
{
    normalSample = (2.0 * normalSample) - 1.0;
    float3 T = normalize(tangent.xyz);
    float3 bitangent = normalize(cross(normal, T));
    float3x3 TBN = float3x3(T, bitangent, normal);
    return normalize(mul(normalSample, TBN));
}

Texture2D Texture0 : register(t0);
Texture2D NormalMap : register(t1);
Texture2D SpecularMap : register(t2);
SamplerState Sampler : register(s0);

struct GBufferPixelIn
{
    float4 Position : SV_Position;
    float4 Diffuse : COLOR;
    float2 TextureUV : TEXCOORD0;
    float3 ViewNormal : TEXCOORD1;
    float4 ViewTangent : TANGENT;
    float4 ViewBiTangent : BINORMAL;
};

struct GBufferOutput
{
    float4 Target0 : SV_Target0;
    uint Target1 : SV_Target1;
    float4 Target2 : SV_Target2;
      // | -----------32 bits-----------|   
 // | Diffuse (RGB)  | SpecInt (A) | RT0   
 // | Packed Normal--------------->| RT1 
   // | Emissive (RGB) | SpecPwr (A) | RT2 
};

GBufferPixelIn VSFillGBuffer(VertexShaderInput vertex)
{
    GBufferPixelIn result = (GBufferPixelIn) 0;
    vertex.Position.w = 1.0;
    result.Position = mul(vertex.Position, WorldViewProjection);
    result.Diffuse = float4(1, 1, 1, 1);
    result.TextureUV = vertex.TextureUV;
    result.ViewNormal = mul(vertex.Normal, (float3x3) World);
    result.ViewNormal = mul(result.ViewNormal, (float3x3) View);
    result.ViewTangent = float4(mul(vertex.Tangent.xyz, (float3x3) World), vertex.Tangent.w);
    result.ViewTangent.xyz = mul(result.ViewTangent.xyz, (float3x3) View);
    return result;
}

float2 EncodeAzimuthal(in float3 N)
{
    float f = sqrt(8 * N.z + 8);
    return N.xy / f + 0.5;
}

uint PackNormal(in float3 N)
{
    float2 encN = EncodeAzimuthal(N);
    uint result = 0;
    result = f32tof16(encN.x);
    result |= f32tof16(encN.y) << 16;
    return result;
}
GBufferOutput PSFillGBuffer(GBufferPixelIn pixel)
{
    float3 normal = normalize(pixel.ViewNormal);
    if (HasNormalMap)
        normal = ApplyNormalMap(normal, pixel.ViewTangent, NormalMap.Sample(Sampler, pixel.TextureUV).rgb);
    float4 sample = (float4) 1.0;
    if (HasTexture)
        sample = Texture0.Sample(Sampler, pixel.TextureUV);

    float specIntensity = 1.0f;
    if (HasSpecMap)
        specIntensity = SpecularMap.Sample(Sampler, pixel.TextureUV).r;
    else
        specIntensity = dot(MaterialSpecular.rgb, float3(0.2125, 0.7154, 0.0721));

    float3 diffuse = (pixel.Diffuse.rgb) * sample.rgb;

    GBufferOutput result = (GBufferOutput) 0;
    result.Target0.xyz = diffuse;
    result.Target0.w = specIntensity;
    result.Target1 = PackNormal(normal);
    result.Target2.xyz = MaterialEmissive.rgb;
    result.Target2.w = MaterialSpecularPower / 50;
    return result;
}