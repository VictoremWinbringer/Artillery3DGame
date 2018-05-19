cbuffer data : register(b0)
{
    float Time;
    float4x4 WVP;
};

static const float3 ToL = normalize(float3(1.0f, 1.0f, 1.0f));

struct VS_IN
{
    float4 position : POSITION;
    float2 TextureUV : TEXCOORD0;
};

struct PS_IN
{
    float4 position : SV_Position;
    float2 TextureUV : TEXCOORD0;
    float3 Normal : NORMAL;
    float3 Tangent : TANGENT;
};


void GerstnerWaveTessendorf(float waveLength, float speed, float amplitude, float steepness, float2 direction, in float3 position, inout float3 result, inout float3 normal, inout float3 tangent)
{
    float L = waveLength; // wave crest to crest float
    float A = amplitude; // wave height 
    float k = 2.0 * 3.1416 / L; // wave length
    float kA = k * A;
    float2 D = normalize(direction); // normalized direction
    float2 K = D * k; // wave vector and magnitude (direction)
    float Q = steepness;
    float S = speed * 0.5;
    
    float w = S * k; // Phase/frequency
    float wT = w * Time;
    float KPwT = dot(K, position.xz) - wT;
    float S0 = sin(KPwT);
    float C0 = cos(KPwT);
    float2 xz = position.xz - D * Q * A * S0;
    float y = A * C0;
    // Bitangent
    float3 B = float3(1 - (Q * D.x * D.x * kA * C0), D.x * kA * S0, -(Q * D.x * D.y * kA * C0));
     // Tangent
    float3 T = float3(-(Q * D.x * D.y * kA * C0), D.y * kA * S0, 1 - (Q * D.y * D.y * kA * C0));
    B = normalize(B);
    T = normalize(T);
    float3 N = cross(T, B);
    result.y += y;
    normal += N;
    tangent += T;
}

PS_IN VS(VS_IN input)
{
    PS_IN vertex = (PS_IN) 0;
    vertex.position = input.position;
    vertex.TextureUV = input.TextureUV;

    float3 N = (float3) 0;
    float3 T = (float3) 0;
    float3 waveOffset = (float3) 0;
    float2 direction = float2(1, 0);

    //// Choppy ocean waves
    // GerstnerWaveTessendorf(10, 2, 2.5, 0.5, direction,    vertex.position, waveOffset, N, T);
    // GerstnerWaveTessendorf(5, 1.2, 2, 1, direction,    vertex.position, waveOffset, N, T);
    // GerstnerWaveTessendorf(4, 2, 2, 1, direction + float2(0,    1), vertex.position, waveOffset, N, T);
    //GerstnerWaveTessendorf(4, 1, 0.5, 1, direction + float2(0, 1), vertex.position, waveOffset, N, T);
    //GerstnerWaveTessendorf(2.5, 2, 0.5, 1, direction + float2(0, 0.5), vertex.position, waveOffset, N, T);
    //GerstnerWaveTessendorf(2, 2, 0.5, 1, direction, vertex.position, waveOffset, N, T);

   // Gentle ocean waves 
    GerstnerWaveTessendorf(80, 0.01, 3, 1, direction, vertex.position, waveOffset, N, T);
    GerstnerWaveTessendorf(40, 0.01, 4, 1, direction + float2(0, 0.5), vertex.position, waveOffset, N, T);
    GerstnerWaveTessendorf(30, 0.01, 3, 1, direction + float2(0, 1), vertex.position, waveOffset, N, T);
    GerstnerWaveTessendorf(25, 0.01, 2, 1, direction, vertex.position, waveOffset, N, T);

    vertex.position.xyz += waveOffset;
    vertex.Normal = normalize(N);
    vertex.Tangent.xyz = normalize(T);
    vertex.position = mul(vertex.position, WVP);
    return vertex;
}

Texture2D textureMap : register(t0);
SamplerState textureSampler : register(s0);

float4 PS(PS_IN input) : SV_Target0
{
    float4 color = textureMap.Sample(textureSampler, input.TextureUV);
    float4 amb = color * 0.2f;
    float4 diff = color * saturate(dot(normalize(input.Normal), ToL)) * 0.8f;
    return amb + diff;
}
