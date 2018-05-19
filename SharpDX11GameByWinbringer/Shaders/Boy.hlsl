

Texture2D textureMap : register(t0);
Texture2D NormalMap : register(t1);
SamplerState textureSampler : register(s0);

cbuffer data : register(b0)
{
    float4x4 WVP;
    float4x4 World;
    float4x4 WorldIT;
    float4x4 ViewProjection;
};

struct VS_IN
{
    float4 position : POSITION;
    float3 normal : NORMAL;
    float2 TextureUV : TEXCOORD;
   // float4 Tangent : TANGENT;
  
};

struct PS_IN
{
    float4 position : SV_Position;
    float2 TextureUV : TEXCOORD;
    float3 WorldNormal : NORMAL;
    float4 WorldTangent : TANGENT;
  
};
struct GS_IN
{
    float4 position : SV_Position;
    float2 TextureUV : TEXCOORD;
    float3 WorldNormal : NORMAL;
};

float3 ApplyNormalMap(float3 normal, float4 tangent, float3 normalSample)
{
    // Remap normalSample to the range (-1,1)  
    normalSample = (2.0 * normalSample) - 1.0;
    // Ensure tangent is orthogonal to normal vector   
     // Gram-Schmidt orthogonalize   
    float3 T = normalize(tangent - normal * dot(normal, tangent));
     // Create the Bitangent   
    float3 bitangent = cross(normal, T) * tangent.w;
    // Create TBN matrix to transform from tangent space  
    float3x3 TBN = float3x3(T, bitangent, normal);
    return normalize(mul(normalSample, TBN));
}


GS_IN VS(VS_IN input)
{
    GS_IN output = (GS_IN) 0;
    output.position = mul(input.position, World);
    output.WorldNormal = normalize(mul(input.normal, (float3x3) WorldIT));
    //output.WorldPosition = mul(input.position, World).xyz;
    output.TextureUV = input.TextureUV;
   // output.WorldTangent = float4(mul(input.Tangent.xyz, (float3x3) WorldIT), input.Tangent.w);
    return output;
}

static const float3 ToL = normalize(float3(-1, 1, -1));

float4 PS(PS_IN input) : SV_Target
{
    
    float3 normal = normalize(input.WorldNormal);
    float3 tangent = normalize(input.WorldTangent.xyz);
    normal = ApplyNormalMap(normal, float4(tangent, input.WorldTangent.w), NormalMap.Sample(textureSampler, input.TextureUV).rgb);
    float4 color = textureMap.Sample(textureSampler, input.TextureUV);
    float4 amb = color * 0.2f;
    float4 diff = color * saturate(dot(normalize(normal), ToL)) * 0.8f;
    return amb + diff;
    //float3 normal = normalize(input.WorldNormal);
    //float3 toLight = normalize(LightPosition - input.WorldPosition);

    //float3 Abm = color.rgb * 0.2f;
    //float3 Dif = color.rgb * 0.8f * saturate(dot(normal, toLight));
    //float3 c = Abm + Dif;

    //return float4(c,1.0f);
}

[maxvertexcount(72)]
void GS(triangle GS_IN input[3], inout TriangleStream<PS_IN> TriStream)
{
    float3 P1 = input[0].position.xyz;
    float3 P2 = input[1].position.xyz;
    float3 P3 = input[2].position.xyz;
	
    float3 P = P2 - P1;
    float3 Q = P3 - P1;

    float s1 = input[1].TextureUV.x - input[0].TextureUV.x;
    float t1 = input[1].TextureUV.y - input[0].TextureUV.y;
    float s2 = input[2].TextureUV.x - input[0].TextureUV.x;
    float t2 = input[2].TextureUV.y - input[0].TextureUV.y;
    float tmp = 0.0f;
    if (abs(s1 * t2 - s2 * t1) <= 0.0001f)
    {
        tmp = 1.0f;
    }
    else
    {
        tmp = 1.0f / (s1 * t2 - s2 * t1);
    }

    float3 tangent = float3(0, 0, 0);
    tangent.x = (t2 * P.x - t1 * Q.x);
    tangent.y = (t2 * P.y - t1 * Q.y);
    tangent.z = (t2 * P.z - t1 * Q.z);

    tangent = tangent * tmp;
    float4 t = float4(tangent, 1);
    float3 Nrm = normalize(cross(P2 - P1, P3 - P1));
	
    PS_IN A = (PS_IN) 0;
    A.position = mul(input[0].position, ViewProjection);
    A.WorldNormal = Nrm;
    //input[0].WorldNormal;
    //Nrm;
    A.TextureUV = input[0].TextureUV;
    A.WorldTangent = t;

    PS_IN B = (PS_IN) 0;
    B.position = mul(input[1].position, ViewProjection);
    B.WorldNormal = Nrm;
    //input[1].WorldNormal; //Nrm;
    B.TextureUV = input[1].TextureUV;
    B.WorldTangent = t;
	
    PS_IN C = (PS_IN) 0;
    C.position = mul(input[2].position, ViewProjection);
    C.WorldNormal = Nrm; //input[2].WorldNormal;
    //Nrm;
    C.TextureUV = input[2].TextureUV;
    C.WorldTangent = t;
	
    TriStream.Append(A);
    TriStream.Append(B);
    TriStream.Append(C);
    TriStream.RestartStrip();
}