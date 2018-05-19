cbuffer Params : register(b0) // матрицы вида и проекции
{
    float4x4 World;
    float4x4 View;
    float4x4 Projection;
    float Size;
};


struct PixelInput // описывает вертекс на выходе из Vertex Shader
{
    float4 Position : SV_POSITION;
    float2 UV : TEXCOORD;
};
// функция изменения вертекса и последующая проекция его в Projection Space
PixelInput _offsetNprojected(PixelInput data, float2 offset, float2 uv)
{
    data.Position.xy += offset;
    data.Position = mul(data.Position, Projection);
    data.UV = uv;

    return data;
}

[maxvertexcount(4)] // результат работы GS – 4 вертекса, которые образуют TriangleStrip
void GS(point PixelInput input[1], inout TriangleStream<PixelInput> stream)
{
    PixelInput pointOut = input[0];
  
	// описание квадрата
    stream.Append(_offsetNprojected(pointOut, float2(-1, -1) * Size, float2(0, 0)));
    stream.Append(_offsetNprojected(pointOut, float2(-1, 1) * Size, float2(0, 1)));
    stream.Append(_offsetNprojected(pointOut, float2(1, -1) * Size, float2(1, 0)));
    stream.Append(_offsetNprojected(pointOut, float2(1, 1) * Size, float2(1, 1)));

	// создать TriangleStrip
    stream.RestartStrip();
}