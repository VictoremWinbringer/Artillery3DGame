Texture2D textureMap : register(t0);
Texture2D specularMap : register(t1);
Texture2D disMap : register(t2);
SamplerState textureSampler : register(s0);

const float TessellationFactor = 3;

cbuffer data : register(b0)
{
    float4x4 WorldViewProjection;
    float4x4 World;
};

cbuffer data1 : register(b1)
{
    float Ns_SpecularPower;
    float Ni_OpticalDensity;
    float d_Transparency;
    float Tr_Transparency;
    float3 Tf_TransmissionFilter;
    float4 Ka_AmbientColor;
    float4 Kd_DiffuseColor;
    float4 Ks_SpecularColor;
    float4 Ke_EmissiveColor;
};

cbuffer data2 : register(b2)
{
    float4 Color;
    float3 Direction;
    float3 CameraPosition;
};  

struct VS_IN
{
    float4 position : SV_Position;
    float3 normal : NORMAL;
    float3 textureUV : TEXCOORD;
};

struct PS_IN
{
    float4 Position : SV_Position;
    float3 TextureUV : TEXCOORD;
    float3 WorldNormal : TEXCOORD1;
    float3 WorldPosition : WORLDPOS;
};



PS_IN VS(VS_IN input)
{
    PS_IN output = (PS_IN) 0;
    output.Position = mul(input.position, WorldViewProjection);
    output.WorldNormal = mul(input.normal, (float3x3) World);
    output.WorldPosition = mul(input.position, World).xyz;
    output.TextureUV = input.textureUV;
    return output;
}

float4 PS(PS_IN input) : SV_Target
{
    float4 sample = textureMap.Sample(textureSampler, input.TextureUV);
    float4 specularColorMap = specularMap.Sample(textureSampler, input.TextureUV);

    float3 normal = normalize(input.WorldNormal);
    float3 toLight = normalize(-Direction);
    float3 toEye = normalize(CameraPosition - input.WorldPosition);
    float3 halfway = normalize(toLight + toEye);
    float3 emissive = Ke_EmissiveColor.rgb;
    float3 ambient = sample.rgb * Ka_AmbientColor.rgb;
    float3 diffuse = sample.rgb * Kd_DiffuseColor.rgb * max(0, dot(normal, toLight));
    float3 specular = Ks_SpecularColor.rgb * specularColorMap.rgb * pow(max(0, dot(normal, halfway)), max(Ns_SpecularPower, 0.00001f));
    float3 color = saturate(ambient) + saturate(diffuse) + saturate(specular) + saturate(emissive);
    float alpha = sample.a;

    return saturate(float4(color, alpha));
}
