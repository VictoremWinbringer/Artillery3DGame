// Globals for texture sampling
Texture2D Texture0 : register(t0);
TextureCube Reflection : register(t1);
SamplerState Sampler : register(s0);

cbuffer PerObject : register(b0)
{
    float4x4 WVP;
    float4x4 World;
    float3 CameraPosition;
};

cbuffer PerMaterial : register(b1)
{
    bool HasTexture;
    bool IsReflective;
    float ReflectionAmount;
};

cbuffer PerEnvironmentMap : register(b2)
{
    float4x4 CubeFaceViewProj[6];
};

// Vertex Shader input structure (from Application)
struct VertexShaderInput
{
    float4 Position : SV_Position; // Position - xyzw
    float3 Normal : NORMAL; // Normal - for lighting and mapping operations
    float2 TextureUV : TEXCOORD0; // UV - texture coordinate
};

// Pixel Shader input structure (from Vertex Shader)
struct PixelShaderInput
{
    float4 Position : SV_Position;
    // Interpolation of vertex UV texture coordinate
    float2 TextureUV : TEXCOORD0;

    // We need the World Position and normal for light calculations
    float3 WorldNormal : NORMAL;
    float3 WorldPosition : WORLDPOS;
};

// Pixel Shader input structure (from Geometry Shader) 
struct GS_CubeMapOutput
{
    float4 Position : SV_Position;
    // Interpolation of vertex UV texture coordinate
    float2 TextureUV : TEXCOORD0;

    // We need the World Position and normal for light calculations
    float3 WorldNormal : NORMAL;
    float3 WorldPosition : WORLDPOS;

      // Allows writing to multiple render targets  
    uint RTIndex : SV_RenderTargetArrayIndex;
};

#define GeometryShaderInput PixelShaderInput

GeometryShaderInput VS0(VertexShaderInput vertex)
{
    GeometryShaderInput result = (GeometryShaderInput) 0;

    // Change the position vector to be 4 units for matrix transformation
    vertex.Position.w = 1.0;
    
    // SNIP: vertex skinning here

    // Only world transform
    result.Position = mul(vertex.Position, World);
    // Apply material UV transformation
    result.TextureUV = vertex.TextureUV;
    result.WorldNormal = mul(vertex.Normal, (float3x3) World);
    result.WorldPosition = result.Position.xyz;
    return result;
}

[maxvertexcount(3)] // Outgoing vertex count (1 triangle)
[instance(6)] // Number of times to execute for each input
void GS0(triangle GeometryShaderInput input[3], uint instanceId : SV_GSInstanceID, inout TriangleStream<GS_CubeMapOutput> stream)
{
    // Output the input triangle using the  View/Projection 
    // of the cube face identified by instanceId
    float4x4 viewProj = CubeFaceViewProj[instanceId];
    GS_CubeMapOutput output;

    // Assign the render target instance
    // i.e. 0 = +X face, 1 = -X face and so on
    output.RTIndex = instanceId;
    [unroll]
    for (int v = 0; v < 3; v++)
    {
        // Apply cube face view/projection
        output.Position = mul(input[v].Position, viewProj);
        // Copy other vertex properties as is
        output.WorldPosition = input[v].WorldPosition;
        output.WorldNormal = input[v].WorldNormal;
        output.TextureUV = input[v].TextureUV;

        // Append to the stream
        stream.Append(output);
    }
    stream.RestartStrip();
}


float4 PS0(GS_CubeMapOutput pixel) : SV_Target
{
    float3 normal = normalize(pixel.WorldNormal);
    float3 toEye = normalize(CameraPosition - pixel.WorldPosition);

    // Texture sample here (use white if no texture)
    float4 sample = (float4) 1.0;
    if (HasTexture)
        sample = Texture0.Sample(Sampler, pixel.TextureUV);

    float3 color = sample.rgb;
    
    // Calculate reflection (if any)
    if (IsReflective)
    {
        float3 reflection = reflect(-toEye, normal);
        color = lerp(color, Reflection.Sample(Sampler, reflection).rgb, ReflectionAmount);
    }

    // Return result
    return float4(color, sample.w);
}

PixelShaderInput VS1(VertexShaderInput vertex)
{
    PixelShaderInput result = (PixelShaderInput) 0;
    vertex.Position.w = 1.0;
    result.Position = mul(vertex.Position, WVP);
    result.TextureUV = vertex.TextureUV;
    result.WorldNormal = mul(vertex.Normal, (float3x3) World);
    result.WorldPosition = mul(vertex.Position, World).xyz;
    return result;
}

float4 PS1(PixelShaderInput pixel) : SV_Target
{
    float3 normal = normalize(pixel.WorldNormal);
    float3 toEye = normalize(CameraPosition - pixel.WorldPosition);

    // Texture sample here (use white if no texture)
    float4 sample = (float4) 1.0;
    if (HasTexture)
        sample = Texture0.Sample(Sampler, pixel.TextureUV);

    float3 color = sample.rgb;
    
    // Calculate reflection (if any)
    if (IsReflective)
    {
        float3 reflection = reflect(-toEye, normal);
        color = lerp(color, Reflection.Sample(Sampler, reflection).rgb, ReflectionAmount);
    }

    // Return result
    return float4(color, sample.w);
}

