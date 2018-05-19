Texture2D<float4> input : register(t0);
RWTexture2D<float4> output : register(u0);
cbuffer ComputeConstants : register(b0)
{
    float Intensity;
};
#define FILTERTAP 5// Must be ODD
 // Note: at a thread group size of 1024, the maximum 
// FILTERTAP possible is 33
#define FILTERRADIUS ((FILTERTAP-1)/2) 

 // The total group size (DX11 max 1024)
#define GROUPSIZE (THREADSX * THREADSY) 
// Shared memory for storing thread group data for filters 
// with enough room for 
// GROUPSIZE + (THREADSY * FILTERRADIUS * 2) 
// Max size of groupshared is 32KB
groupshared float4 FilterGroupMemX[GROUPSIZE + (THREADSY * FILTERRADIUS * 2)];
groupshared float4 FilterGroupMemY[GROUPSIZE + (THREADSX * FILTERRADIUS * 2)];

static const float BlurKernel[FILTERTAP] = (float[FILTERTAP]) (1.0 / (FILTERTAP));

[numthreads(THREADSX, THREADSY, 1)]
void CS(uint groupIndex : SV_GroupIndex, uint3 groupId : SV_GroupID, uint3 groupThreadId : SV_GroupThreadID, uint3 dispatchThreadId : SV_DispatchThreadID)
{
    uint yOffset = FILTERRADIUS * THREADSX;
    uint offsetGroupIndex = groupIndex + yOffset;
    FilterGroupMemY[offsetGroupIndex] = (float4) 0;
    FilterGroupMemY[offsetGroupIndex] = input[min(dispatchThreadId.xy, input.Length.xy - 1)];
       
    if (groupThreadId.y < FILTERRADIUS)
    {
        int y = dispatchThreadId.y - FILTERRADIUS;
        FilterGroupMemY[offsetGroupIndex - yOffset] = input[int2(dispatchThreadId.x, max(y, 0))];
    }
    if (groupThreadId.y >= THREADSY - FILTERRADIUS)
    {
        int y = dispatchThreadId.y + FILTERRADIUS;
        FilterGroupMemY[offsetGroupIndex + yOffset] = input[int2(dispatchThreadId.x, min(y, input.Length.y - 1))];
    }

    GroupMemoryBarrierWithGroupSync();
    float4 result = float4(0, 0, 0, 0);
    //int index = offsetGroupIndex - yOffset;
    int centerPixel = offsetGroupIndex - yOffset;
    [unroll]
    for (int i = -FILTERRADIUS; i <= FILTERRADIUS; ++i)
    {
        int j = centerPixel + i;
        result += BlurKernel[i + FILTERRADIUS] * FilterGroupMemY[j];
      //  index += THREADSX;
    }
    // Write the result to the output
    output[dispatchThreadId.xy] = lerp(FilterGroupMemY[offsetGroupIndex], result, Intensity);
}