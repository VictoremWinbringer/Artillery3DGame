Texture2D<float4> input : register(t0);
RWTexture2D<float4> output : register(u0);
cbuffer ComputeConstants : register(b0)
{
    float LerpT;
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
// used for RGB/sRGB color models
 #define LUMINANCE_RGB float3(0.2125, 0.7154, 0.0721)
 #define LUMINANCE(_V) dot(_V.rgb, LUMINANCE_RGB)


float SobelEdge(float2 coord, float threshold, float thickness)
{ // Sobel 3x3 tap filter: approximate magnitude  
      // Cheaper than the full Sobel kernel evaluation  
      // http://homepages.inf.ed.ac.uk/rbf/HIPR2/sobel.html
    float p1 = LUMINANCE(input[coord + float2(-thickness, -thickness)]);
    float p2 = LUMINANCE(input[coord + float2(0, -thickness)]);
    float p3 = LUMINANCE(input[coord + float2(thickness, -thickness)]);
    float p4 = LUMINANCE(input[coord + float2(-thickness, 0)]);
    float p6 = LUMINANCE(input[coord + float2(thickness, 0)]);
    float p7 = LUMINANCE(input[coord + float2(-thickness, thickness)]);
    float p8 = LUMINANCE(input[coord + float2(0, thickness)]);
    float p9 = LUMINANCE(input[coord + float2(thickness, thickness)]);
    float sobelX = mad(2, p2, p1 + p3) - mad(2, p8, p7 + p9);
    float sobelY = mad(2, p6, p3 + p9) - mad(2, p4, p1 + p7);
    float edgeSqr = (sobelX * sobelX + sobelY * sobelY);
    float result = 1.0 - (edgeSqr > threshold * threshold);
    // if (edgeSqr > threshold * threshold) { is edge }  
    return result; // black (0) = edge, otherwise white (1) 
} // End SobelEdge

[numthreads(THREADSX, THREADSY, 1)]
void CS(uint groupIndex : SV_GroupIndex, uint3 groupId : SV_GroupID, uint3 groupThreadId : SV_GroupThreadID, uint3 dispatchThreadId : SV_DispatchThreadID)
{
    float4 sample = input[dispatchThreadId.xy];
    float threshold = 0.4f;
    float thickness = 1;

    float3 target = sample.rgb * SobelEdge(dispatchThreadId.xy, threshold, thickness);
    output[dispatchThreadId.xy] = lerpKeepAlpha(sample, target, LerpT);
}