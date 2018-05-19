using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.DXGI;

namespace DifferedRendering
{
    public class ScreenAlignedQuadRenderer : IDisposable
    {
        VertexShader vertexShader;
        InputLayout vertexLayout;
        SharpDX.Direct3D11.Buffer vertexBuffer;
        VertexBufferBinding vertexBinding;
        SharpDX.Direct3D11.Device device;
        public PixelShader Shader { get; set; } = null;
        public ShaderResourceView[] ShaderResources { get; set; } = null;
        public ScreenAlignedQuadRenderer(SharpDX.Direct3D11.Device device)
        {
            Utilities.Dispose(ref vertexShader);
            Utilities.Dispose(ref vertexLayout);
            Utilities.Dispose(ref vertexBuffer);
            this.device = device;
            ShaderFlags shaderFlags = ShaderFlags.None;
#if DEBUG
            shaderFlags = ShaderFlags.Debug | ShaderFlags.SkipOptimization;
#endif
            using (var vertexShaderBytecode = ShaderBytecode.CompileFromFile(@"Shaders\SAQuad.hlsl", "VSMain", "vs_5_0", shaderFlags))
            {
                vertexShader = new VertexShader(device, vertexShaderBytecode);
                vertexLayout = new InputLayout(device, ShaderSignature.GetInputSignature(vertexShaderBytecode), new[]    {
                   new InputElement("SV_Position", 0, Format.R32G32B32_Float, 0, 0),
               });
                vertexBuffer = SharpDX.Direct3D11.Buffer.Create(device,
                    BindFlags.VertexBuffer,
                    new Vector3[]
                    {
                      new Vector3(-1.0f, -1.0f, 1.0f),
                      new Vector3(-1.0f, 1.0f, 1.0f),
                      new Vector3(1.0f, -1.0f, 0.99f),
                      new Vector3(1.0f, 1.0f,  0.99f),
                  });
                vertexBinding = new VertexBufferBinding(vertexBuffer, Utilities.SizeOf<Vector3>(), 0);
            }

            using (var bytecode = ShaderBytecode.CompileFromFile(@"Shaders\SAQuad.hlsl", "PSMain", "ps_5_0", shaderFlags))
                pixelShader = new PixelShader(device, bytecode);

            using (var bytecode = ShaderBytecode.CompileFromFile(@"Shaders\SAQuad.hlsl", "PSMainMultisample", "ps_5_0", shaderFlags))
                pixelShaderMS = new PixelShader(device, bytecode);

            samplerState = new SamplerState(device, new SamplerStateDescription
            {
                Filter = Filter.MinMagMipLinear,
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                ComparisonFunction = Comparison.Never,
                MinimumLod = 0,
                MaximumLod = float.MaxValue
            });
        }
        public void Render()
        {
            var context = device.ImmediateContext;
            using (var oldVertexLayout = context.InputAssembler.InputLayout)
            using (var oldPixelShader = context.PixelShader.Get())
            using (var oldVertexShader = context.VertexShader.Get())
            {
                bool isMultisampledSRV = false;
                if (ShaderResources != null && ShaderResources.Length > 0 && !ShaderResources[0].IsDisposed)
                {
                    for (int i = 0; i < ShaderResources.Length; i++)
                    {
                        context.PixelShader.SetShaderResource(i, ShaderResources[i]);
                    }
                    

                    if (ShaderResources[0].Description.Dimension == SharpDX.Direct3D.ShaderResourceViewDimension.Texture2DMultisampled)
                    {
                        isMultisampledSRV = true;
                    }
                }

                if (Shader == null)
                {
                    if (isMultisampledSRV)
                        context.PixelShader.Set(pixelShaderMS);
                    else
                        context.PixelShader.Set(pixelShader);
                }
                else
                {
                    context.PixelShader.Set(Shader);
                }
                context.PixelShader.SetSampler(0, samplerState);
                context.VertexShader.Set(vertexShader);
                context.InputAssembler.InputLayout = vertexLayout;
                context.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleStrip;
                context.InputAssembler.SetVertexBuffers(0, vertexBinding);
                context.Draw(4, 0);
                if (ShaderResources != null && ShaderResources.Length > 0)
                {
                    context.PixelShader.SetShaderResources(0, new ShaderResourceView[ShaderResources.Length]);
                }
                context.PixelShader.Set(oldPixelShader);
                context.VertexShader.Set(oldVertexShader);
                context.InputAssembler.InputLayout = oldVertexLayout;
            }
        }

        #region IDisposable Support
        private bool disposedValue = false;
        private PixelShader pixelShader;
        private PixelShader pixelShaderMS;
        private SamplerState samplerState;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Utilities.Dispose(ref vertexShader);
                    Utilities.Dispose(ref vertexLayout);
                    Utilities.Dispose(ref vertexBuffer);
                    Utilities.Dispose(ref pixelShader);
                    Utilities.Dispose(ref pixelShaderMS);
                    Utilities.Dispose(ref samplerState);
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {

            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
