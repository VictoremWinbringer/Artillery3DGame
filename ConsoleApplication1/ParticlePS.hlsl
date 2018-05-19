#include "Particle.hlsl"
Texture2D ParticleTexture : register(t0);
SamplerState linearSampler : register(s0);
float4 PSMain(PS_Input pixel) : SV_Target
{
    float4 result = ParticleTexture.Sample(linearSampler, pixel.UV);
     // Fade-out as approaching the near clip plane  
  // and as a particle loses energy between 1->0  
    return float4(result.xyz, saturate(pixel.Energy) * result.w * pixel.Position.z * pixel.Position.z);
}