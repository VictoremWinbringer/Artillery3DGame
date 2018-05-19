Texture2D Texture0 : register(t0);
SamplerState Sampler : register(s0);

cbuffer PerObject : register(b0)
{
    float4x4 WorldViewProjection;
    float4x4 World;
    float4x4 WorldInverseTranspose;
}; 

struct DirectionalLight
{
    float4 Color;
    float3 Direction;
};

cbuffer PerFrame : register(b1)
{
    DirectionalLight Light;
    float3 CameraPosition;
}; 

cbuffer PerMaterial : register(b2)
{
    float4 MaterialAmbient;
    float4 MaterialDiffuse;
    float4 MaterialSpecular;
    float MaterialSpecularPower;
    bool HasTexture;
    float4 MaterialEmissive;
    float4x4 UVTransform;
};

struct VertexShaderInput
{
    float4 Position : SV_Position;
    float3 Normal : NORMAL;
    float4 Color : COLOR0;
    float2 TextureUV : TEXCOORD;
};

struct PixelShaderInput
{
    float4 Position : SV_Position;
    float4 Diffuse : COLOR;
    float2 TextureUV : TEXCOORD;
    float3 WorldNormal : NORMAL;
    float3 WorldPosition : WORLDPOS;
};

float3 Lambert(float4 pixelDiffuse, float3 normal, float3 toLight)
{   
    float3 diffuseAmount = saturate(dot(normal, toLight));
    return pixelDiffuse.rgb * diffuseAmount;
}


float3 SpecularPhong(float3 normal, float3 toLight, float3 toEye)
{ //     R = reflect(i,n) => R = i - 2 * n * dot(i,n) 
    float3 reflection = reflect(-toLight, normal);
    // Calculate the specular amount (smaller specular power =  
       // larger specular highlight) Cannot allow a power of 0     
    // otherwise the model will appear black and white   
    float specularAmount = pow(saturate(dot(reflection, toEye)), max(MaterialSpecularPower, 0.00001f));
    return MaterialSpecular.rgb * specularAmount;
}

float3 SpecularBlinnPhong(float3 normal, float3 toLight, float3 toEye)
{ // Calculate the half vector 
    float3 halfway = normalize(toLight + toEye);
     // Saturate is used to prevent backface light reflection 
     // Calculate specular (smaller power = larger highlight) 
    float specularAmount = pow(saturate(dot(normal, halfway)), max(MaterialSpecularPower, 0.00001f));
    return MaterialSpecular.rgb * specularAmount;
}

PixelShaderInput VS(VertexShaderInput vertex)
{
    PixelShaderInput result = (PixelShaderInput) 0;

    result.Diffuse =  MaterialDiffuse;
    result.TextureUV = mul(float4(vertex.TextureUV.x, vertex.TextureUV.y, 0, 1), (float4x2) UVTransform).xy;
    result.Position = mul(vertex.Position, WorldViewProjection);
    result.TextureUV = vertex.TextureUV;
    result.WorldNormal = mul(vertex.Normal, (float3x3) WorldInverseTranspose);
    result.WorldPosition = mul(vertex.Position, World).xyz;

    return result;
}


////DiffusePS
//float4 PS(PixelShaderInput pixel) : SV_Target
//{
//      // Texture sample (use white if no texture)
//    float4 sample = (float4) 1.0f;
//    if (HasTexture)
//        sample = Texture0.Sample(Sampler, pixel.TextureUV);

//   // After interpolation the values are not necessarily
//     // normalized 
//    float3 normal = normalize(pixel.WorldNormal);
//    float3 toEye = normalize(CameraPosition - pixel.WorldPosition);
//    float3 toLight = normalize(-Light.Direction);

//    float3 emissive = MaterialEmissive.rgb;
//    float3 ambient = MaterialAmbient.rgb;
//    float3 diffuse = Lambert(pixel.Diffuse, normal, toLight);

//// Calculate final color component    
//    // We saturate ambient+diffuse to ensure there is no over
//    // brightness on the texture sample if the sum is greater 
//    // than 1 (we would not do this for HDR rendering)  
//    float3 color = (saturate(ambient + diffuse) * sample.rgb) * Light.Color.rgb + emissive;

//      // Calculate final alpha value
//    float alpha = pixel.Diffuse.a * sample.a;
//    return float4(color, alpha);
//}

//FongPS
float4 PS(PixelShaderInput pixel) : SV_Target
{
   // After interpolation the values are not necessarily
     // normalized 
    float3 normal = normalize(pixel.WorldNormal);
    float3 toEye = normalize(CameraPosition - pixel.WorldPosition);
    float3 toLight = normalize(-Light.Direction);
      // Texture sample (use white if no texture)
    float4 sample = (float4) 1.0f;
    if (HasTexture)
        sample = Texture0.Sample(Sampler, pixel.TextureUV);
    float3 ambient = MaterialAmbient.rgb;
    float3 emissive = MaterialEmissive.rgb;

    float3 diffuse = Lambert(pixel.Diffuse, normal, toLight);
    float3 specular = SpecularPhong(normal, toLight, toEye);

// Calculate final color component 
    float3 color = (saturate(ambient + diffuse) * sample.rgb + specular) * Light.Color.rgb + emissive;
    // We saturate ambient+diffuse to ensure there is no over
    // brightness on the texture sample if the sum is greater 
    // than 1 (we would not do this for HDR rendering)  
      // Calculate final alpha value
    float alpha = pixel.Diffuse.a * sample.a;

    return float4(color, alpha);
}

////BlinFongPS
//float4 PS(PixelShaderInput pixel) : SV_Target
//{
//   // After interpolation the values are not necessarily
//     // normalized 
//    float3 normal = normalize(pixel.WorldNormal);
//    float3 toEye = normalize(CameraPosition - pixel.WorldPosition);
//    float3 toLight = normalize(-Light.Direction);
//      // Texture sample (use white if no texture)
//    float4 sample = (float4) 1.0f;
//    if (HasTexture)
//        sample = Texture0.Sample(Sampler, pixel.TextureUV);
//    float3 ambient = MaterialAmbient.rgb;
//    float3 emissive = MaterialEmissive.rgb;

//    float3 diffuse = Lambert(pixel.Diffuse, normal, toLight);
//    float3 specular = SpecularBlinnPhong(normal, toLight, toEye);

//// Calculate final color component 
//    float3 color = (saturate(ambient + diffuse) * sample.rgb + specular) * Light.Color.rgb + emissive;
//    // We saturate ambient+diffuse to ensure there is no over
//    // brightness on the texture sample if the sum is greater 
//    // than 1 (we would not do this for HDR rendering)  
//      // Calculate final alpha value
//    float alpha = pixel.Diffuse.a * sample.a;

//    return float4(color, alpha);
//}
