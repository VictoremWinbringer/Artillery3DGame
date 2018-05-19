cbuffer data : register(b0)
{
    float4x4 WVP;
};
struct VS_IN
{
    float4 position : POSITION;
    float4 color : COLOR;
};

struct PS_IN
{
    float4 position : SV_Position;
    float4 color : COLOR;
};

PS_IN VS(VS_IN input)
{
    PS_IN output = (PS_IN) 0;    
    output.position = mul(input.position, WVP);
    output.color = input.color;
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
    float4 color = input.color;
    return color;
}
