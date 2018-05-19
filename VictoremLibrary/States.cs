using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.Mathematics.Interop;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace VictoremLibrary
{
    class States : IDisposable
    {
        #region Поля 

        private Device _device;
        RasterizerState _rasterizerState = null;
        BlendState _blendState = null;
        DepthStencilState _depthState = null;
        SamplerState _samler;
        #endregion

        #region Свойства

        public RasterizerState RasterizerStat { get { return _rasterizerState; } }
        public BlendState BlendState { get { return _blendState; } }
        public DepthStencilState DepthState { get { return _depthState; } }
        public SamplerState Sampler { get { return _samler; } }
        public RawColor4? BlendFactor { get; set; } = null;
        public DepthStencilStateDescription DepthStencilDescripshion { set { Utilities.Dispose(ref _depthState); _depthState = new DepthStencilState(_device, value); } }
        public RasterizerStateDescription RasterizerDescription { set { Utilities.Dispose(ref _rasterizerState); _rasterizerState = new RasterizerState(_device, value); } }
        public BlendStateDescription BlendDescription { set { Utilities.Dispose(ref _blendState); _blendState = new BlendState(_device, value); } }
        public SamplerStateDescription SamplerDescription { set { Utilities.Dispose(ref _samler); _samler = new SamplerState(_device, value); } }
        #endregion

        public States(Device device)
        {
            _device = device;
            var d = DepthStencilStateDescription.Default();
            d.IsDepthEnabled = true;
            d.IsStencilEnabled = false;
            DepthStencilDescripshion = d;
            var r = RasterizerStateDescription.Default();
            r.CullMode = CullMode.None;
            r.FillMode = SharpDX.Direct3D11.FillMode.Solid;
            RasterizerDescription = r;
            var b = BlendStateDescription.Default();
            b.AlphaToCoverageEnable = new RawBool(true);
            BlendDescription = b;
            var s = new SamplerStateDescription()
            {
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                BorderColor = new Color4(0, 0, 0, 0),
                ComparisonFunction = Comparison.Never,
                Filter = Filter.Anisotropic,
                MaximumAnisotropy = 16,
                MaximumLod = float.MaxValue,
                MinimumLod = 0,
                MipLodBias = 0.0f
            };
        }

        #region Методы

        public void ApplyAll(DeviceContext context, int sampleSlot = 0)
        {
            ApplyBlend(context);
            ApplyDepth(context);
            ApplyRasterizer(context);
            ApplySampler(context, sampleSlot);
        }

        public void ApplyBlend(DeviceContext context)
        {
            context.OutputMerger.SetBlendState(_blendState, BlendFactor);
        }

        public void ApplyDepth(DeviceContext context)
        {
            context.OutputMerger.DepthStencilState = _depthState;
        }

        public void ApplyRasterizer(DeviceContext context)
        {
            context.Rasterizer.State = _rasterizerState;
        }

        public void ApplySampler(DeviceContext context, int slot = 0)
        {
            context.VertexShader.SetSampler(slot, _samler);
            context.PixelShader.SetSampler(slot, _samler);
            context.GeometryShader.SetSampler(slot, _samler);
            context.HullShader.SetSampler(slot, _samler);
            context.DomainShader.SetSampler(slot, _samler);
            context.ComputeShader.SetSampler(slot, _samler);
        }

        public Buffer CreateIndexBuffer(uint[] index)
        {
            return Buffer.Create(_device, BindFlags.IndexBuffer, index);
        }

        public Buffer CreateVertexBuffer<V>(V[] vertex) where V : struct
        {
            return Buffer.Create<V>(_device, BindFlags.VertexBuffer, vertex);
        }

        public Buffer CreateConstantBuffer(int size)
        {
            return new Buffer(_device, size, ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
        }

        public void SetVIBuffers(DeviceContext context,
            VertexBufferBinding vertexBinging,
            Buffer indexBuffer = null,
            PrimitiveTopology primitiveTopology = PrimitiveTopology.TriangleList)
        {
            context.InputAssembler.PrimitiveTopology = primitiveTopology;
            context.InputAssembler.SetVertexBuffers(0, vertexBinging);
            if (indexBuffer != null) context.InputAssembler.SetIndexBuffer(indexBuffer, SharpDX.DXGI.Format.R32_UInt, 0);
        }

        public void Dispose()
        {
            Utilities.Dispose(ref _rasterizerState);
            Utilities.Dispose(ref _blendState);
            Utilities.Dispose(ref _depthState);
            Utilities.Dispose(ref _samler);
        }
        #endregion
    }
}
