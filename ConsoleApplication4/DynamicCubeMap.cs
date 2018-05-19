using SharpDX;
using Buffer = SharpDX.Direct3D11.Buffer;
using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using VictoremLibrary;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;
namespace ConsoleApplication4
{
       public struct CubeFaceCamera
    {
        public Matrix View;
        public Matrix Projection;
    }

    public class DynamicCubeMap
    {

        Texture2D EnvMap;
        RenderTargetView EnvMapRTV;
        DepthStencilView EnvMapDSV;
        public ShaderResourceView EnvMapSRV;
        Buffer PerEnvMapBuffer;
        ViewportF Viewport;
        public CubeFaceCamera[] Cameras = new CubeFaceCamera[6];
        public int Size { get; private set; }
        public Matrix World { get; private set; }
        public bool Show { get; private set; }
      //  Game game;
        private Device device;

        public DynamicCubeMap(Device dv, int size = 256)
        {

            device = dv;
            Size = size;
            World = Matrix.Identity;
            Show = true;
            CreateDeviceDependentResources();
        }

        void CreateDeviceDependentResources()
        {
            Utilities.Dispose(ref EnvMap);
            Utilities.Dispose(ref EnvMapSRV);
            Utilities.Dispose(ref EnvMapRTV);
            Utilities.Dispose(ref EnvMapDSV);
            Utilities.Dispose(ref PerEnvMapBuffer);

            var textureDesc = new Texture2DDescription()
            {
                Format = Format.R8G8B8A8_UNorm,
                Height = this.Size,
                Width = this.Size,
                ArraySize = 6,
                BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget,
                OptionFlags = ResourceOptionFlags.GenerateMipMaps | ResourceOptionFlags.TextureCube,
                SampleDescription = new SampleDescription(1, 0),
                MipLevels = 0,
                Usage = ResourceUsage.Default,
                CpuAccessFlags = CpuAccessFlags.None,
            };

            EnvMap = new Texture2D(device, textureDesc);

            var descSRV = new ShaderResourceViewDescription();
            descSRV.Format = textureDesc.Format;
            descSRV.Dimension = SharpDX.Direct3D.ShaderResourceViewDimension.TextureCube;
            descSRV.TextureCube.MostDetailedMip = 0;
            descSRV.TextureCube.MipLevels = -1;
            EnvMapSRV = new ShaderResourceView(device, EnvMap, descSRV);

            var descRTV = new RenderTargetViewDescription();
            descRTV.Format = textureDesc.Format;
            descRTV.Dimension = RenderTargetViewDimension.Texture2DArray;
            descRTV.Texture2DArray.MipSlice = 0;
            descRTV.Texture2DArray.FirstArraySlice = 0;
            descRTV.Texture2DArray.ArraySize = 6;
            EnvMapRTV = new RenderTargetView(device, EnvMap, descRTV);

            using (var depth = new Texture2D(device, new Texture2DDescription
            {
                Format = Format.D32_Float,
                BindFlags = BindFlags.DepthStencil,
                Height = Size,
                Width = Size,
                Usage = ResourceUsage.Default,
                SampleDescription = new SampleDescription(1, 0),
                CpuAccessFlags = CpuAccessFlags.None,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.TextureCube,
                ArraySize = 6
            }))
            {
                var descDSV = new DepthStencilViewDescription();
                descDSV.Format = depth.Description.Format;
                descDSV.Dimension = DepthStencilViewDimension.Texture2DArray;
                descDSV.Flags = DepthStencilViewFlags.None;
                descDSV.Texture2DArray.MipSlice = 0;
                descDSV.Texture2DArray.FirstArraySlice = 0;
                descDSV.Texture2DArray.ArraySize = 6;
                EnvMapDSV = new DepthStencilView(device, depth, descDSV);
            }

            Viewport = new Viewport(0, 0, Size, Size);

            PerEnvMapBuffer = new Buffer(device, Utilities.SizeOf<Matrix>() * 6, ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
        }

        public void SetViewPoint(Vector3 camera)
        {
            var targets = new[] {
                camera + Vector3.UnitX, // +X    
            camera - Vector3.UnitX, // -X    
            camera + Vector3.UnitY, // +Y 
            camera - Vector3.UnitY, // -Y 
            camera + Vector3.UnitZ, // +Z  
            camera - Vector3.UnitZ  // -Z 
        };

            var upVectors = new[] {
            Vector3.UnitY, // +X   
            Vector3.UnitY, // -X 
            -Vector3.UnitZ,// +Y  
            +Vector3.UnitZ,// -Y 
            Vector3.UnitY, // +Z   
            Vector3.UnitY, // -Z 
        };

            for (int i = 0; i < 6; i++)
            {
                Cameras[i].View = Matrix.LookAtLH(camera, targets[i], upVectors[i]);
                Cameras[i].Projection = Matrix.PerspectiveFovLH(MathUtil.Pi * 0.5f, 1.0f, 0.1f, 100.0f);
            }

        }

        public void UpdateSinglePass(DeviceContext context, System.Action<DeviceContext, Matrix, Matrix, RenderTargetView, DepthStencilView, DynamicCubeMap> renderScene)
        {
            context.OutputMerger.SetRenderTargets(EnvMapDSV, EnvMapRTV);
            context.Rasterizer.SetViewport(Viewport);
            Matrix[] viewProjections = new Matrix[6];
            for (var i = 0; i < 6; i++)
                viewProjections[i] = Matrix.Transpose(Cameras[i].View * Cameras[i].Projection);
            context.UpdateSubresource(viewProjections, PerEnvMapBuffer);
            context.GeometryShader.SetConstantBuffer(2, PerEnvMapBuffer);
            renderScene(context, Cameras[0].View, Cameras[0].Projection, EnvMapRTV, EnvMapDSV, this);
            context.OutputMerger.ResetTargets();
            context.GenerateMips(EnvMapSRV);
        }
    }
}