using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Device = SharpDX.Direct3D11.Device;
namespace DifferedRendering
{
    class GBuffer : IDisposable
    {
        public List<Texture2D> RTs = new List<Texture2D>();
        public List<ShaderResourceView> SRVs = new List<ShaderResourceView>();
        public List<RenderTargetView> RTVs = new List<RenderTargetView>();
        public Texture2D DS0;
        public ShaderResourceView DSSRV;
        public DepthStencilView DSV;
        int width;
        int height;
        SampleDescription sampleDescription;
        SharpDX.DXGI.Format[] RTFormats;
        Device device;
        public GBuffer(int width,
            int height,
            SampleDescription sampleDesc, Device dv,
            params SharpDX.DXGI.Format[] targetFormats)
        {
            System.Diagnostics.Debug.Assert(targetFormats != null && targetFormats.Length > 0 && targetFormats.Length < 9, "Between 1 and 8 render target formats must be provided");
            this.width = width;
            this.height = height;
            this.sampleDescription = sampleDesc;
            RTFormats = targetFormats;
            device = dv;
            CreateDeviceDependentResources();
        }
        void CreateDeviceDependentResources()
        {
            SharpDX.Utilities.Dispose(ref DSSRV);
            SharpDX.Utilities.Dispose(ref DSV);
            SharpDX.Utilities.Dispose(ref DS0);
            RTs.ForEach(rt => SharpDX.Utilities.Dispose(ref rt));
            SRVs.ForEach(srv => SharpDX.Utilities.Dispose(ref srv));
            RTVs.ForEach(rtv => SharpDX.Utilities.Dispose(ref rtv));
            RTs.Clear();
            SRVs.Clear();
            RTVs.Clear();
            var texDesc = new Texture2DDescription();
            texDesc.BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget;
            texDesc.ArraySize = 1;
            texDesc.CpuAccessFlags = CpuAccessFlags.None;
            texDesc.Usage = ResourceUsage.Default;
            texDesc.Width = width;
            texDesc.Height = height;
            texDesc.MipLevels = 1;
            texDesc.SampleDescription = sampleDescription;
            bool isMSAA = sampleDescription.Count > 1;
            var rtvDesc = new RenderTargetViewDescription();
            rtvDesc.Dimension = isMSAA ? RenderTargetViewDimension.Texture2DMultisampled : RenderTargetViewDimension.Texture2D;
            rtvDesc.Texture2D.MipSlice = 0;
            var srvDesc = new ShaderResourceViewDescription();
            srvDesc.Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm;
            srvDesc.Dimension = isMSAA ? SharpDX.Direct3D.ShaderResourceViewDimension.Texture2DMultisampled : SharpDX.Direct3D.ShaderResourceViewDimension.Texture2D;
            srvDesc.Texture2D.MipLevels = -1;
            srvDesc.Texture2D.MostDetailedMip = 0;
            foreach (var format in RTFormats)
            {
                texDesc.Format = format;
                srvDesc.Format = format;
                rtvDesc.Format = format;
                RTs.Add(new Texture2D(device, texDesc));
                SRVs.Add(new ShaderResourceView(device, RTs.Last(), srvDesc));
                RTVs.Add(new RenderTargetView(device, RTs.Last(), rtvDesc));
            }
            texDesc.BindFlags = BindFlags.ShaderResource | BindFlags.DepthStencil;
            texDesc.Format = SharpDX.DXGI.Format.R32G8X24_Typeless;
            DS0 = new Texture2D(device, texDesc);
            srvDesc.Format = SharpDX.DXGI.Format.R32_Float_X8X24_Typeless;
            DSSRV = new ShaderResourceView(device, DS0, srvDesc);
            var dsvDesc = new DepthStencilViewDescription();
            dsvDesc.Flags = DepthStencilViewFlags.None;
            dsvDesc.Dimension = isMSAA ? DepthStencilViewDimension.Texture2DMultisampled : DepthStencilViewDimension.Texture2D;
            dsvDesc.Format = SharpDX.DXGI.Format.D32_Float_S8X24_UInt;
            DSV = new DepthStencilView(device, DS0, dsvDesc);
        }
        public void Bind(DeviceContext context)
        {
            context.OutputMerger.SetTargets(DSV, 0, new UnorderedAccessView[0], RTVs.ToArray());
        }
        public void Unbind(DeviceContext context)
        {
            context.OutputMerger.ResetTargets();
        }
        public void Clear(DeviceContext context, Color background)
        {
            context.ClearDepthStencilView(DSV, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);
            foreach (var rtv in RTVs)
                context.ClearRenderTargetView(rtv, background);
        }

        #region Освобождение ресурсов
        private bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    SharpDX.Utilities.Dispose(ref DSSRV);
                    SharpDX.Utilities.Dispose(ref DSV);
                    SharpDX.Utilities.Dispose(ref DS0);
                    RTs.ForEach(rt => SharpDX.Utilities.Dispose(ref rt));
                    SRVs.ForEach(srv => SharpDX.Utilities.Dispose(ref srv));
                    RTVs.ForEach(rtv => SharpDX.Utilities.Dispose(ref rtv));
                    RTs.Clear();
                    SRVs.Clear();
                    RTVs.Clear();

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
