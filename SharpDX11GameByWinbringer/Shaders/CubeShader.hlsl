cbuffer data : register(b0)
{
    float4x4 WVP;
};
struct VS_IN
{
    float4 position : POSITION;
    float2 TextureUV : TEXCOORD0;
};

struct PS_IN
{
    float4 position : SV_Position;
    float2 TextureUV : TEXCOORD0;   
};

PS_IN VS(VS_IN input)
{
    PS_IN output = (PS_IN) 0;   
    output.position = mul(input.position, WVP);
    output.TextureUV = input.TextureUV;
    return output;
}

Texture2D textureMap : register(t0);
SamplerState textureSampler : register(s1)
{
    Filter = MIN_MAG_MIP_LINEAR;
    AddressU = Wrap;
    AddressV = Wrap;
    AddresW = Wrap;
};
float4 PS(PS_IN input) : SV_Target
{
    float4 color = textureMap.Sample(textureSampler, input.TextureUV);   
    return color;
}
