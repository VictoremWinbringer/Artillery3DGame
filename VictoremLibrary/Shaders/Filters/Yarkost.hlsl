Texture2D<float4> input : register(t0);
RWTexture2D<float4> output : register(u0);
cbuffer ComputeConstants : register(b0)
{
    float Intensity;
};
// Lerp helper functions 
float4 lerpKeepAlpha(float4 source, float3 target, float T)
{
    return float4(lerp(source.rgb, target, T), source.a);
}
float4 lerpKeepAlpha(float3 source, float4 target, float T)
{
    return float4(lerp(source, target.rgb, T), target.a);
}

[numthreads(THREADSX, THREADSY, 1)]
void CS(uint groupIndex : SV_GroupIndex,
 uint3 groupId : SV_GroupID,
 uint3 groupThreadId : SV_GroupThreadID,
 uint3 dispatchThreadId : SV_DispatchThreadID)
{

    //.............Меняет яркость.
    // Adjust brightness by adding or removing Black
     // LerpT == 1.0 original image 
    // LerpT > 1.0 brightens 
    // LerpT < 1.0 darkens
    float4 sample = input[dispatchThreadId.xy];
    float3 target = float3(0, 0, 0);
    output[dispatchThreadId.xy] = lerpKeepAlpha(target, sample, Intensity);

}