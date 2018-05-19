Texture2D textureMap : register(t0);
Texture2D specularMap : register(t1);
Texture2D disMap : register(t2);
SamplerState textureSampler : register(s0);

cbuffer data : register(b0)
{
    float4x4 WorldViewProjection;
    float4x4 World;
    float4x4 WorldIT;
    float4x4 ViewProjection;
    float DisplaceScale;
    float TessellationFactor;
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
    float2 textureUV : TEXCOORD;
};

struct PS_IN
{
    float4 Position : SV_Position;
    float2 TextureUV : TEXCOORD;
    float3 WorldNormal : NORMAL;
    float3 WorldPosition : WORLDPOS;
};

struct TS_IN
{
    float3 Position : POSITION;
    float2 TextureUV : TEXCOORD;
    float3 WorldNormal : NORMAL;
    float3 WorldPosition : WORLDPOS;
};

struct HS_TrianglePatchConstant
{
    float EdgeTessFactor[3] : SV_TessFactor;
    float InsideTessFactor : SV_InsideTessFactor;
    float2 TextureUV[3] : TEXCOORD0;
    float3 WorldNormal[3] : NORMAL3;
};



float3 CalculateDisplacement(float2 UV, float3 normal)
{
    normal = normalize(normal);
    if (DisplaceScale == 0)
        return 0;
    const float mipLevel = 1.0f;
    float height = disMap.SampleLevel(textureSampler, UV, mipLevel).x;
    height = (2 * height) - 1;
    return height * DisplaceScale * normal;
}



float3 ApplyNormalMap(float3 normal, float3 tangent, float3 bitangent, float3 normalSample)
{
    normalSample = (2.0 * normalSample) - 1.0;
    float3x3 TBN = float3x3(tangent, bitangent, normal);
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


TS_IN VS(VS_IN input)
{
    TS_IN output = (TS_IN) 0;
    output.Position = mul(input.position, WorldViewProjection).xyz;
    output.WorldNormal = mul(input.normal, (float3x3) WorldIT);
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

[domain("tri")] // Triangle domain for our shader
[partitioning("integer")] // Partitioning type
[outputtopology("triangle_cw")] // The vertex winding order of the generated triangles
[outputcontrolpoints(3)] // Number of times this part of the hull shader will be called for each patch
[patchconstantfunc("HS_TrianglesConstant")] // The constant hull shader function
TS_IN HS(InputPatch<TS_IN, 3> patch, uint id : SV_OutputControlPointID, uint patchID : SV_PrimitiveID)
{
    TS_IN result = (TS_IN) 0;
    result.Position = patch[id].WorldPosition;
    result.WorldNormal = patch[id].WorldNormal;
    result.TextureUV = patch[id].TextureUV;    
    return result;
}
// Triangle patch constant func (executes once for each patch)
HS_TrianglePatchConstant HS_TrianglesConstant(InputPatch<TS_IN, 3> patch)
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
PS_IN DS(HS_TrianglePatchConstant constantData, const OutputPatch<TS_IN, 3> patch, float3 barycentricCoords : SV_DomainLocation)
{
    PS_IN result = (PS_IN) 0;

    // Interpolate using barycentric coordinates
    float3 position = BarycentricInterpolate(patch[0].Position, patch[1].Position, patch[2].Position, barycentricCoords);
    // Interpolate array of UV coordinates
    float2 UV = BarycentricInterpolate(constantData.TextureUV, barycentricCoords);
   
    // Interpolate array of normals
    float3 normal = BarycentricInterpolate(constantData.WorldNormal, barycentricCoords);
  
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
    position += CalculateDisplacement(UV, normal);
    // Prepare pixel shader input:
    // Transform world position to view-projection
    result.Position = mul(float4(position, 1), ViewProjection);    
    result.WorldPosition = position;
    result.TextureUV = UV;
    result.WorldNormal = normal;
  
    return result;
}

//[maxvertexcount(30)]
//void GSMain(triangle PixelShaderInput input[3], inout LineStream<PixelShaderInput> OutputStream)
//{
//    PixelShaderInput output = (PixelShaderInput) 0;

//    // now create three new normal rectangles, one for each vertex
//    for (uint j = 0; j < 3; j++)
//    {
//        float3 pos = input[j].WorldPosition;
//        float3 normal = normalize(input[j].WorldNormal);

//        // calculate tangent
//        float3 tangent;
//        float3 bitangent;

//        float3 c1 = cross(normal, float3(0.0, 0.0, 1.0));
//        float3 c2 = cross(normal, float3(0.0, 1.0, 0.0));

//        if (length(c1) > length(c2))
//        {
//            tangent = c1;
//        }
//        else
//        {
//            tangent = c2;
//        }

//        tangent = normalize(tangent);
//        bitangent = normalize(cross(tangent, normal));
        
//        // Set the new geometry width and height
//        float3 nl = normal * 0.02; // full height
//        float3 tf = tangent * 0.02; // 1/2 width (tangent direction)
//        float3 btf = bitangent * 0.02; // 1/2 width (bitangent direction)

//        float3 p[6];
//        p[0] = pos + tf;
//        p[1] = pos;
//        p[2] = pos + btf;
//        p[3] = pos;
//        p[4] = pos + nl;
//        p[5] = pos;

//        output = (PixelShaderInput) 0;
//        output.Diffuse = float4(1, 0, 0, 1);

//        output.Position = mul(float4(p[0], 1), ViewProjection);
//        OutputStream.Append(output);
//        output.Position = mul(float4(p[1], 1), ViewProjection);
//        OutputStream.Append(output);
//        OutputStream.RestartStrip();

//        output.Diffuse = float4(0, 1, 0, 1);
//        output.Position = mul(float4(p[2], 1), ViewProjection);
//        OutputStream.Append(output);
//        output.Position = mul(float4(p[3], 1), ViewProjection);
//        OutputStream.Append(output);
//        OutputStream.RestartStrip();

//        output.Diffuse = float4(0, 0, 1, 1);
//        output.Position = mul(float4(p[4], 1), ViewProjection);
//        OutputStream.Append(output);
//        output.Position = mul(float4(p[5], 1), ViewProjection);
//        OutputStream.Append(output);
//        OutputStream.RestartStrip();
//    }
//}
