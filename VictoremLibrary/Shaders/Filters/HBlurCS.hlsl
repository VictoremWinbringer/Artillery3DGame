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
    // Calculate the correct index into FilterGroupMemX
    uint offsetGroupIndex = groupIndex + (groupThreadId.y * 2 * FILTERRADIUS) + FILTERRADIUS;
    // 1. Sample the current texel (clamp to max input coord)
    FilterGroupMemX[offsetGroupIndex] = input[min(dispatchThreadId.xy, input.Length.xy - 1)];
    // 2. If thread is within FILTERRADIUS of thread group
     // boundary, sample an additional texel.
     // 2a. additional texel @ dispatchThreadId.x – FILTERRADIUS
    if (groupThreadId.x < FILTERRADIUS)
    {
         // Clamp out of bound samples that occur at image  
         // borders (i.e. if x < 0, set to 0).
        int x = dispatchThreadId.x - FILTERRADIUS;
        FilterGroupMemX[offsetGroupIndex - FILTERRADIUS] = input[int2(max(x, 0), dispatchThreadId.y)];
    }
    // 2b. additional texel @ dispatchThreadId.x + FILTERRADIUS
    if (groupThreadId.x >= THREADSX - FILTERRADIUS)
    {
         // Clamp out of bound samples that occur at image 
          // borders (if x > imageWidth-1, set to imageWidth-1)  
        int x = dispatchThreadId.x + FILTERRADIUS;
        FilterGroupMemX[offsetGroupIndex + FILTERRADIUS] = input[int2(min(x, input.Length.x - 1), dispatchThreadId.y)];
    }
    // 3. Wait for all threads in group to complete sampling 
    GroupMemoryBarrierWithGroupSync();
     // 4. Apply blur kernel to the current texel using the 
    //    samples we have already loaded for this thread group
    float4 result = float4(0, 0, 0, 0);
    int centerPixel = offsetGroupIndex;
     [unroll]
    for (int i = -FILTERRADIUS; i <= FILTERRADIUS; ++i)
    {
        int j = centerPixel + i;
        result += BlurKernel[i + FILTERRADIUS] * FilterGroupMemX[j];
    }
    // Write the result to the output 
    output[dispatchThreadId.xy] = lerp(FilterGroupMemX[offsetGroupIndex], result, Intensity);
}

