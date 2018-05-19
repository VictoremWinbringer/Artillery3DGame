Texture2D textureMap : register(t0);
SamplerState textureSampler : register(s0);
TextureCube Reflection : register(t1);


cbuffer data : register(b0)
{
    float4x4 WVP;
    uint hasBones;
    uint hasDif;
    float4x4 world;
    float4 dif;
    bool IsReflective;
    float ReflectionAmount;
    float3 CameraPosition;
};

cbuffer data2 : register(b1)
{
    float4x4 Bones[1024];
}

static const float3 ToL = float3(-1, 1, -1);

struct VS_IN
{
    float4 position : POSITION;
    float3 normal : NORMAL;
    float3 uv : TEXCOORD;
    float3 tangent : TANGENT;
    float3 biTangent : BINORMAL;
    float4 boneID : BLENDINDICES;
    float4 wheights : BLENDWEIGHT;
    float4 color : COLOR;
};

struct PS_IN
{
    float4 position : SV_Position;
    float3 normal : NORMAL;
    float3 uv : TEXCOORD;
    float3 tangent : TANGENT;
    float3 biTangent : BINORMAL;
    float3 WorldPosition : WORLDPOS;
    float4 color : COLOR;
};

void SkinVertex(float4 weights, float4 bones, inout float4 position, inout float3 normal, inout float3 tangent, inout float3 biTangent)
{
    float4x4 skinTransform = Bones[bones.x] * weights.x + Bones[bones.y] * weights.y + Bones[bones.z] * weights.z + Bones[bones.w] * weights.w;
           
    position = mul(position, skinTransform);
    normal = mul(normal, (float3x3) skinTransform);
    tangent = mul(tangent, (float3x3) skinTransform);
    biTangent = mul(biTangent, (float3x3) skinTransform);
    
}

float3 ApplyNormalMap(float3 normal, float3 tangent, float3 biTangent, float3 normalSample)
{
    // Remap normalSample to the range (-1,1)  
    normalSample = (2.0 * normalSample) - 1.0;
    float3 T = normalize(tangent);   
    float3 B = normalize(biTangent);
    float3 N = normalize(normal);
    // Create TBN matrix to transform from tangent space  
    float3x3 TBN = float3x3(T, B, N);
    return normalize(mul(normalSample, TBN));
};

float3 CalculateDisplacement(float heightRed, float3 normal)
{
    normal = normalize(normal);
    heightRed = (2 * heightRed) - 1;
    return heightRed  * normal;
}

PS_IN VS(VS_IN input)
{
    PS_IN vertex = (PS_IN) 0;
    float4 position = input.position;
    if (hasBones)
        SkinVertex(input.wheights, input.boneID, position, input.normal, input.tangent, input.biTangent);
    vertex.position = mul(position, WVP);
    vertex.normal = mul(input.normal, (float3x3) world);
    vertex.tangent = mul(input.tangent, (float3x3) world);
    vertex.biTangent = mul(input.biTangent, (float3x3) world);
    vertex.uv = input.uv;
    position = mul(position, world);
    vertex.WorldPosition = position.xyz;
    vertex.color = input.color;
    return vertex;
}

float4 PS(PS_IN input) : SV_Target0
{
    
    float3 toEye = normalize(CameraPosition - input.WorldPosition);
    float4 color = input.color;
    if (hasDif)
        color = textureMap.Sample(textureSampler, input.uv);
    float3 amb = color.rgb * 0.3;
    float3 dif = color.rgb * saturate(dot(normalize(input.normal), normalize(ToL))) * 0.7;
    float3 DA = amb + dif;
    if (IsReflective)
    {
        float3 reflection = reflect(-toEye, normalize(input.normal));
        DA = lerp(DA, Reflection.Sample(textureSampler, reflection).rgb, ReflectionAmount);
    }
    return float4(DA.r,DA.g,DA.b, color.w);
}