Texture2D<float4> input : register(t0);
RWByteAddressBuffer outputByteBuffer : register(u0);
// used for RGB/sRGB color models
#define LUMINANCE_RGB float3(0.2125, 0.7154, 0.0721)
#define LUMINANCE(_V) dot(_V.rgb, LUMINANCE_RGB) 
// Calculate the luminance histogram of the input 
// Output to outputByteBuffer

[numthreads(THREADSX, THREADSY, 1)]
void CS(uint groupIndex : SV_GroupIndex, uint3
  groupId : SV_GroupID, uint3 groupThreadId : SV_GroupThreadID, uint3 dispatchThreadId : SV_DispatchThreadID)
{
    float4 sample = input[dispatchThreadId.xy];
    // Calculate the Relative luminance (and map to 0-255)   
    float luminance = LUMINANCE(sample.xyz) * 255.0;
   // Addressable as bytes, x4 to store 32-bit integers  
  // Atomic increment of value at address.
    outputByteBuffer.InterlockedAdd((uint) luminance * 4, 1);
}