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
        //................Добавляет sepia tone (Светло коричневый тон)
    float4 sample = input[dispatchThreadId.xy];
    float3 target;
    target.r = saturate(dot(sample.rgb, float3(0.393, 0.769, 0.189)));
    target.g = saturate(dot(sample.rgb, float3(0.349, 0.686, 0.168)));
    target.b = saturate(dot(sample.rgb, float3(0.272, 0.534, 0.131)));
    output[dispatchThreadId.xy] = lerpKeepAlpha(sample, target, Intensity);
}