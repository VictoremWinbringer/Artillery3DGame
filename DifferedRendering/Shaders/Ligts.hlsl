cbuffer data : register(b0)
{
    float4x4 InverseProjection;
    float4x4 WorldViewProjection;
    float3 LightColor;
};

Texture2D Texture0 : register(t0);
Texture2D<uint> Texture1 : register(t1);
Texture2D Texture2 : register(t2);
Texture2D<float> TextureDepth : register(t3);

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



struct LightStruct
{
    float3 Direction;
    uint Type;
    
    float3 Position;
    float Range;
    
    float3 Color;
};

cbuffer PerLight : register(b4)
{
    LightStruct LightParams;
};

struct VertexIn
{
    float4 Position : SV_Position;
};

struct PixelIn
{
    float4 Position : SV_Position;
    float2 UV : TEXCOORD0;
};

struct VertexShaderInput
{
    float4 Position : POSITION;
    float3 Normal : NORMAL;
    float2 TextureUV : TEXCOORD0;
    float4 Tangent : TANGENT;
    float4 BiTangent : BINORMAL;
};
PixelIn VSLight(VertexShaderInput vertex)
{
    PixelIn result = (PixelIn) 0;

    vertex.Position.w = 1.0f;
    
    result.Position = mul(vertex.Position, WorldViewProjection);

    // Determine UV from device coords
    result.UV.xy = result.Position.xy / result.Position.w;
    // The UV coordinates are top-left 0,0 bottom-right 1,1
    result.UV.x = result.UV.x * 0.5 + 0.5;
    result.UV.y = result.UV.y * -0.5 + 0.5;

    return result;
}

struct GBufferAttributes
{
    float3 Position;
    float3 Normal;
    float3 Diffuse;
    float SpecularInt; // specular intensity
    float3 Emissive;
    float SpecularPower;
};

void ExtractGBufferAttributes(in PixelIn pixel,
                            in Texture2D<float4> t0,
                            in Texture2D<uint> t1,
                            in Texture2D<float4> t2,
                            in Texture2D<float> t3,
                           out GBufferAttributes attrs)
{
    int3 screenPos = int3(pixel.Position.xy, 0);

    float depth = t3.Load(screenPos).x;
    attrs.Diffuse = t0.Load(screenPos).xyz;
    attrs.SpecularInt = t0.Load(screenPos).w;
    attrs.Normal = UnpackNormal(t1.Load(screenPos));
    attrs.Emissive = t2.Load(screenPos).xyz;
    attrs.SpecularPower = t2.Load(screenPos).w * 50;

    float x = pixel.UV.x * 2 - 1;
    float y = (1 - pixel.UV.y) * 2 - 1;
    float4 posVS = mul(float4(x, y, depth, 1.0f), InverseProjection);
    attrs.Position = posVS.xyz / posVS.w;
}

static const float pi = 3.14159265f;

float DiffuseConservation()
{
    return 1.0 / pi;
}
float SpecularConservation(float power)
{
   
    return 0.0397436 * power + 0.0856832;
}

float3 LightContribution(GBufferAttributes attrs, float3 V, float3 L, float3 H, float3 D, float attenuation)
{
    float NdotL = saturate(dot(attrs.Normal, L));
    if (NdotL <= 0)
    {
        discard;
        return 0;
    }
    float NdotH = saturate(dot(attrs.Normal, H));
 
    float3 diffuse = NdotL * LightParams.Color * attrs.Diffuse; // * DiffuseConservation();
  
    float specPower = max(attrs.SpecularPower, 0.00001f);
    float3 specular = pow(NdotH, specPower) * attrs.SpecularInt * LightParams.Color; // * SpecularConservation(specPower);

    return (diffuse + specular) * attenuation + attrs.Emissive;
}

void PrepareLightInputs(in float3 camera, in float3 position, in float3 N, in LightStruct light,
                        out float3 V, out float3 L, out float3 H, out float D, out float attenuation)
{
    V = camera - position;
    L = light.Position - position;
    D = length(L);
    
    L /= D;
    H = normalize(L + V);
   
    attenuation = max(1 - D / light.Range, 0);
    attenuation *= attenuation;
}

float3 ComputeSpotLight(GBufferAttributes attrs, float3 camPos)
{
    float3 result = (float3) 0;
    
    float3 V, L, H;
    float D, attenuation, NdotL, NdotH;
    PrepareLightInputs(camPos, attrs.Position, attrs.Normal, LightParams,
        V, L, H, D, attenuation);

    
    return LightContribution(attrs, V, L, H, D, attenuation);
}

float3 ComputePointLight(GBufferAttributes attrs, float3 camPos)
{
    float3 result = (float3) 0;
    
    float3 V, L, H;
    float D, attenuation, NdotL, NdotH;
    PrepareLightInputs(camPos, attrs.Position, attrs.Normal, LightParams,
        V, L, H, D, attenuation);

    return LightContribution(attrs, V, L, H, D, attenuation);
}

float3 ComputeDirectionLight(GBufferAttributes attrs, float3 camPos)
{
    float3 result = (float3) 0;
    
    float3 V, L, H;
    float D, attenuation;
    PrepareLightInputs(camPos, attrs.Position, attrs.Normal, LightParams,
        V, L, H, D, attenuation);

    L = normalize(-LightParams.Direction);
    H = normalize(L + V);
    attenuation = 1.0f;
    //return attrs.Position;
    return LightContribution(attrs, V, L, H, D, attenuation);
}

float4 PSSpotLight(in PixelIn pixel) : SV_Target
{
    float4 result = (float4) 0;
    result.a = 1.0f;

    GBufferAttributes attrs;
    ExtractGBufferAttributes(pixel,
        Texture0, Texture1,
        Texture2, TextureDepth,
        attrs);

    result.xyz = ComputeSpotLight(attrs, (float3) 0);

    return result;
}

float4 PSPointLight(in PixelIn pixel) : SV_Target
{
    float4 result = (float4) 0;
    result.a = 1.0f;

    GBufferAttributes attrs;
    ExtractGBufferAttributes(pixel,
        Texture0, Texture1,
        Texture2, TextureDepth,
        attrs);

    float3 V, L, H;
    float D, attenuation;
    PrepareLightInputs((float3) 0, attrs.Position, attrs.Normal, LightParams,
        V, L, H, D, attenuation);

    result.xyz = LightContribution(attrs, V, L, H, D, attenuation);

    return result;
}

float4 PSDirectionalLight(in PixelIn pixel) : SV_Target
{
    float4 result = (float4) 0;
    result.a = 1.0f;

    GBufferAttributes attrs;
    ExtractGBufferAttributes(pixel,
        Texture0, Texture1,
        Texture2, TextureDepth,
        attrs);

    float3 V, L, H;
    float D, attenuation;
    PrepareLightInputs((float3) 0, attrs.Position, attrs.Normal, LightParams,
        V, L, H, D, attenuation);

    L = normalize(-LightParams.Direction);
    H = normalize(L + V);
    attenuation = 1.0f;
    result.xyz = LightContribution(attrs, V, L, H, D, attenuation);
    return result;
}

float4 PSAmbientLight(in PixelIn pixel) : SV_Target
{
    float4 result = (float4) 0;
    result.a = 1.0f;

    GBufferAttributes attrs;
    ExtractGBufferAttributes(pixel,
        Texture0, Texture1,
        Texture2, TextureDepth,
        attrs);

    result.xyz = attrs.Diffuse * LightParams.Color;
    return result;
}

float4 PSDebugLight(in PixelIn pixel) : SV_Target
{
    float4 result = (float4) 0;
    result.xyz = LightColor;
    result.w = 1.0f;
    return result;
}