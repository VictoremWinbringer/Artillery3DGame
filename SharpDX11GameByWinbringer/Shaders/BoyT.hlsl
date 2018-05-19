Texture2D textureMap : register(t0);
Texture2D NormalMap : register(t1);
SamplerState textureSampler : register(s0);


cbuffer PerObject : register(b0)
{
    float4x4 WorldViewProjection;
    float4x4 World;
    float4x4 WorldInverseTranspose;
    float4x4 ViewProjection;
    float TessellationFactor;
};

struct HS_TrianglePatchConstant
{
    float EdgeTessFactor[3] : SV_TessFactor;
    float InsideTessFactor : SV_InsideTessFactor;    
    float2 TextureUV[3] : TEXCOORD0;
    float3 WorldNormal[3] : NORMAL3;
};

struct DS_ControlPointInput
{
    float3 Position : BEZIERPOS;
};

struct VS_IN
{
    float4 Position : POSITION;
    float3 Normal : NORMAL;
    float2 TextureUV : TEXCOORD;
    float4 Tangent : TANGENT;
  
};

struct PixelShaderInput
{
    float4 Position : SV_POSITION;
    float3 WorldNormal : NORMAL;
    float2 TextureUV : TEXCOORD;
    float4 WorldTangent : TANGENT;
};

struct HullShaderInput
{
    float3 WorldPosition : POSITION;
    float2 TextureUV : TEXCOORD0;
    float3 WorldNormal : NORMAL;
    float4 WorldTangent : TANGENT;
};

struct DS_PNControlPointInput
{
    float3 Position : POSITION;
    float3 WorldNormal : NORMAL;
    float2 TextureUV : TEXCOORD;
    float4 WorldTangent : TANGENT;
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

float2 BarycentricInterpolate(float2 v0, float2 v1, float2 v2, float3 barycentric)
{
    return barycentric.z * v0 + barycentric.x * v1 + barycentric.y * v2;
}

float2 BarycentricInterpolate(float2 v[3], float3 barycentric)
{
    return BarycentricInterpolate(v[0], v[1], v[2], barycentric);
}

float3 BarycentricInterpolate(float3 v0, float3 v1, float3 v2, float3 barycentric)
{
    return barycentric.z * v0 + barycentric.x * v1 + barycentric.y * v2;
}

float3 BarycentricInterpolate(float3 v[3], float3 barycentric)
{
    return BarycentricInterpolate(v[0], v[1], v[2], barycentric);
}

float4 BarycentricInterpolate(float4 v0, float4 v1, float4 v2, float3 barycentric)
{
    return barycentric.z * v0 + barycentric.x * v1 + barycentric.y * v2;
}

float4 BarycentricInterpolate(float4 v[3], float3 barycentric)
{
    return BarycentricInterpolate(v[0], v[1], v[2], barycentric);
}

float3 ProjectOntoPlane(float3 planeNormal, float3 planePoint, float3 pointToProject)
{
    float3 n = normalize(planeNormal);
    return pointToProject - dot(pointToProject - planePoint, n) * n;
}

HullShaderInput VS(VS_IN vertex)
{
    HullShaderInput result = (HullShaderInput) 0;

    result.TextureUV = vertex.TextureUV;
    result.WorldNormal = mul(vertex.Normal, (float3x3) WorldInverseTranspose);
    result.WorldPosition = mul(vertex.Position, World).xyz;
  //  result.WorldTangent = float4(mul(vertex.Tangent.xyz, (float3x3) WorldInverseTranspose), vertex.Tangent.w);
    return result;
}

float4 PS(PixelShaderInput input) : SV_Target
{
    float3 normal = normalize(input.WorldNormal);
    float3 tangent = normalize(input.WorldTangent.xyz);
    normal = ApplyNormalMap(normal, float4(tangent, input.WorldTangent.w), NormalMap.Sample(Sampler, input.TextureUV).rgb);
    float4 color = textureMap.Sample(textureSampler, input.TextureUV);
    float4 amb = color * 0.2f;
    float4 diff = color * saturate(dot(normalize(normal), normalize(float3(-1, 1, -1)))) * 0.8f;
    return amb + diff;
  //  return textureMap.Sample(textureSampler, input.TextureUV) * saturate(dot(normalize(input.Nrm), normalize(float3(-1,1,-1)))); //* dot(input.Nrm, normalize(float3(1, 1, 0)));
}



[domain("tri")] // Triangle domain for our shader
[partitioning("integer")] // Partitioning type
[outputtopology("triangle_cw")] // The vertex winding order of the generated triangles
[outputcontrolpoints(3)] // Number of times this part of the hull shader will be called for each patch
[patchconstantfunc("HS_TrianglesConstant")] // The constant hull shader function
DS_PNControlPointInput HS(InputPatch<HullShaderInput, 3> patch, uint id : SV_OutputControlPointID, uint patchID : SV_PrimitiveID)
{
    DS_PNControlPointInput result = (DS_PNControlPointInput) 0;
    result.Position = patch[id].WorldPosition;
    result.WorldNormal = patch[id].WorldNormal;
    result.TextureUV = patch[id].TextureUV;
    result.WorldTangent = patch[id].WorldTangent;
    return result;
}
// Triangle patch constant func (executes once for each patch)
HS_TrianglePatchConstant HS_TrianglesConstant(InputPatch<HullShaderInput, 3> patch)
{
    HS_TrianglePatchConstant result = (HS_TrianglePatchConstant) 0;

    float3 roundedEdgeTessFactor;
    float roundedInsideTessFactor, insideTessFactor;
    ProcessTriTessFactorsMax((float3) TessellationFactor, 1.0, roundedEdgeTessFactor, roundedInsideTessFactor, insideTessFactor);

    // Apply the edge and inside tessellation factors
    result.EdgeTessFactor[0] = roundedEdgeTessFactor.x;
    result.EdgeTessFactor[1] = roundedEdgeTessFactor.y;
    result.EdgeTessFactor[2] = roundedEdgeTessFactor.z;
    result.InsideTessFactor = roundedInsideTessFactor;
    //result.InsideTessFactor = roundedInsideTessFactor * insideMultiplier;
    
    // Apply constant information
    [unroll]
    for (uint i = 0; i < 3; i++)
    {
        result.TextureUV[i] = patch[i].TextureUV;
        result.WorldNormal[i] = patch[i].WorldNormal;
    }

    return result;
}

// This domain shader applies control point weighting to the barycentric coords produced by the fixed function tessellator stage
[domain("tri")]
PixelShaderInput DS(HS_TrianglePatchConstant constantData, const OutputPatch<DS_ControlPointInput, 3> patch, float3 barycentricCoords : SV_DomainLocation)
{
    PixelShaderInput result = (PixelShaderInput) 0;

    // Interpolate using barycentric coordinates
    float3 position = BarycentricInterpolate(patch[0].Position, patch[1].Position, patch[2].Position, barycentricCoords);
    // Interpolate array of UV coordinates
    float2 UV = BarycentricInterpolate(constantData.TextureUV, barycentricCoords);
   
    // Interpolate array of normals
    float3 normal = BarycentricInterpolate(constantData.WorldNormal, barycentricCoords);
    float3 tangent = BarycentricInterpolate(patch[0].WorldTangent, patch[1].WorldTangent, patch[2].WorldTangent, barycentricCoords);
     // BEGIN Phong Tessellation
    // Orthogonal projection in the tangent planes
    float3 posProjectedU = ProjectOntoPlane(constantData.WorldNormal[0], patch[0].Position, position);
    float3 posProjectedV = ProjectOntoPlane(constantData.WorldNormal[1], patch[1].Position, position);
    float3 posProjectedW = ProjectOntoPlane(constantData.WorldNormal[2], patch[2].Position, position);

    // Interpolate the projected points
    position = BarycentricInterpolate(posProjectedU, posProjectedV, posProjectedW, barycentricCoords);
    
    // Example of applying only half of the Phong displaced position
    //position = lerp(position, BarycentricInterpolate(posProjectedU, posProjectedV, posProjectedW, barycentricCoords), 0.5);
    // END Phong Tessellation

    // Prepare pixel shader input:
    // Transform world position to view-projection
    result.Position = mul(float4(position, 1), ViewProjection);
    
    
    result.TextureUV = UV;
    result.WorldNormal = normal;
    result.WorldTangent = tangent;
    return result;
}

//HS_PNTrianglePatchConstant HS_PNTrianglesConstant(InputPatch<HullShaderInput, 3> patch)
//{
//    HS_PNTrianglePatchConstant result = (HS_PNTrianglePatchConstant) 0;

//    float3 roundedEdgeTessFactor;
//    float roundedInsideTessFactor, insideTessFactor;
//    ProcessTriTessFactorsMax((float3) TessellationFactor, 1.0, roundedEdgeTessFactor, roundedInsideTessFactor, insideTessFactor);

//    // Apply the edge and inside tessellation factors
//    result.EdgeTessFactor[0] = roundedEdgeTessFactor.x;
//    result.EdgeTessFactor[1] = roundedEdgeTessFactor.y;
//    result.EdgeTessFactor[2] = roundedEdgeTessFactor.z;
//    result.InsideTessFactor = roundedInsideTessFactor;
//    //result.InsideTessFactor = roundedInsideTessFactor * insideMultiplier;

//    //************************************************************
//    // Calculate PN-Triangle coefficients
//    // Refer to Vlachos 2001 for the original formula
//    float3 p1 = patch[0].WorldPosition;
//    float3 p2 = patch[1].WorldPosition;
//    float3 p3 = patch[2].WorldPosition;

//    //B300 = p1;
//    //B030 = p2;
//    //float3 b003 = p3;
    
//    float3 n1 = patch[0].WorldNormal;
//    float3 n2 = patch[1].WorldNormal;
//    float3 n3 = patch[2].WorldNormal;
    
//    //N200 = n1;
//    //N020 = n2;
//    //N002 = n3;

//    // Calculate control points
//    float w12 = dot((p2 - p1), n1);
//    result.B210 = (2.0f * p1 + p2 - w12 * n1) / 3.0f;

//    float w21 = dot((p1 - p2), n2);
//    result.B120 = (2.0f * p2 + p1 - w21 * n2) / 3.0f;

//    float w23 = dot((p3 - p2), n2);
//    result.B021 = (2.0f * p2 + p3 - w23 * n2) / 3.0f;
    
//    float w32 = dot((p2 - p3), n3);
//    result.B012 = (2.0f * p3 + p2 - w32 * n3) / 3.0f;

//    float w31 = dot((p1 - p3), n3);
//    result.B102 = (2.0f * p3 + p1 - w31 * n3) / 3.0f;
    
//    float w13 = dot((p3 - p1), n1);
//    result.B201 = (2.0f * p1 + p3 - w13 * n1) / 3.0f;
    
//    float3 e = (result.B210 + result.B120 + result.B021 +
//                result.B012 + result.B102 + result.B201) / 6.0f;
//    float3 v = (p1 + p2 + p3) / 3.0f;
//    result.B111 = e + ((e - v) / 2.0f);
    
//    // Calculate normals
//    float v12 = 2.0f * dot((p2 - p1), (n1 + n2)) /
//                          dot((p2 - p1), (p2 - p1));
//    result.N110 = normalize((n1 + n2 - v12 * (p2 - p1)));

//    float v23 = 2.0f * dot((p3 - p2), (n2 + n3)) /
//                          dot((p3 - p2), (p3 - p2));
//    result.N011 = normalize((n2 + n3 - v23 * (p3 - p2)));

//    float v31 = 2.0f * dot((p1 - p3), (n3 + n1)) /
//                          dot((p1 - p3), (p1 - p3));
//    result.N101 = normalize((n3 + n1 - v31 * (p1 - p3)));

//    return result;
//}

//// This domain shader applies contol point weighting to the barycentric coords produced by the fixed function tessellator stage
//[domain("tri")]
//PS_INPUT DS(HS_PNTrianglePatchConstant constantData, const OutputPatch<DS_PNControlPointInput, 3> patch, float3 barycentricCoords : SV_DomainLocation)
//{
//    PS_INPUT result = (PS_INPUT) 0;

//    // Prepare barycentric ops (xyz=uvw,   w=1-u-v,   u,v,w>=0)
//    float u = barycentricCoords.x;
//    float v = barycentricCoords.y;
//    float w = barycentricCoords.z;
//    float uu = u * u;
//    float vv = v * v;
//    float ww = w * w;
//    float uu3 = 3.0f * uu;
//    float vv3 = 3.0f * vv;
//    float ww3 = 3.0f * ww;

//    // Interpolate using barycentric coordinates and PN Triangle control points
//    float3 position =
//        patch[0].Position * w * ww + //B300
//        patch[1].Position * u * uu + //B030
//        patch[2].Position * v * vv + //B003
//        constantData.B210 * ww3 * u +
//        constantData.B120 * uu3 * w +
//        constantData.B201 * ww3 * v +
//        constantData.B021 * uu3 * v +
//        constantData.B102 * vv3 * w +
//        constantData.B012 * vv3 * u +
//        constantData.B111 * 6.0f * w * u * v;
//    float3 normal =
//        patch[0].WorldNormal * ww + //N200
//        patch[1].WorldNormal * uu + //N020
//        patch[2].WorldNormal * vv + //N002
//        constantData.N110 * w * u +
//        constantData.N011 * u * v +
//        constantData.N101 * w * v;

//    // Interpolate using barycentric coordinates as per Tri
//    float2 UV = BarycentricInterpolate(patch[0].TextureUV, patch[1].TextureUV, patch[2].TextureUV, barycentricCoords);    
    

//    // Transform world position to view-projection
//    result.Position = mul(float4(position, 1), ViewProjection);
       
//    result.TextureUV = UV;
//    result.WorldNormal = normal;
    
//    return result;
//}

