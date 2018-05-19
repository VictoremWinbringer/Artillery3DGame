using SharpDX;
using SharpDX.Direct3D11;
using System.Runtime.InteropServices;
using VictoremLibrary;
using SharpDX.D3DCompiler;
using SharpDX.DXGI;

namespace ConsoleApplication2
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Matrixes
    {
        public Matrix World;
        public Matrix View;
        public Matrix Proj;
        public float Size;
        Vector3 _padding0;
        public void Trans()
        {
            World.Transpose();
            View.Transpose();
            Proj.Transpose();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct GPUParticleData
    {
        public Vector3 Position;
        public Vector3 Velocity;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Constants
    {
        public int GroupDim;
        public uint MaxParticles;
        public float DeltaTime;
        float padding0;
        public Vector3 Atractor;
        float padding1;
    }

    class Logic : LogicBase
    {
        const int PARTICLES_COUNT = 10000;
        private Buffer _perFrame;
        private Buffer _particlesBuffer;
        Buffer _csConstants;
        private ShaderResourceView _SRV;
        private UnorderedAccessView _UAV;
        private SamplerState _particleSampler;
        private ShaderResourceView _texture;
        private DepthStencilState _DState;
        private BlendState _blendState;
        private int _groupSizeX;
        private int _groupSizeY;
        VertexShader _vs;
        GeometryShader _gs;
        PixelShader _ps;
        ComputeShader _cs;
        private Constants _c;
        private Matrixes m;

        public Matrix World { get { return worldMatrix; } set { worldMatrix = value; } }

        public Logic(Game game) : base(game)
        {

            int numGroups = (PARTICLES_COUNT % 768 != 0) ? ((PARTICLES_COUNT / 768) + 1) : (PARTICLES_COUNT / 768);
            double secondRoot = System.Math.Pow((double)numGroups, (double)(1.0 / 2.0));
            secondRoot = System.Math.Ceiling(secondRoot);
            _groupSizeX = _groupSizeY = (int)secondRoot;

            game.Color = Color.Black;
            System.Random random = new System.Random();

            GPUParticleData[] initialParticles = new GPUParticleData[PARTICLES_COUNT];
            Vector3 min = new Vector3(-30f, -30f, -30f);
            Vector3 max = new Vector3(30f, 30f, 30f);

            for (int i = 0; i < PARTICLES_COUNT; i++)
            {
                initialParticles[i].Position = random.NextVector3(min, max);

                float angle = -(float)System.Math.Atan2(initialParticles[i].Position.X, initialParticles[i].Position.Z);
                initialParticles[i].Velocity = new Vector3((float)System.Math.Cos(angle), 0f, (float)System.Math.Sin(angle)) * 5f;
            }

            _particlesBuffer = new Buffer(game.DeviceContext.Device,
               Utilities.SizeOf<GPUParticleData>() * PARTICLES_COUNT,
               ResourceUsage.Default,
               BindFlags.ShaderResource | BindFlags.UnorderedAccess,
               CpuAccessFlags.None,
               ResourceOptionFlags.BufferStructured,
               Utilities.SizeOf<GPUParticleData>());
            game.DeviceContext.UpdateSubresource(initialParticles, _particlesBuffer);

            #region Blend and Depth States

            var blendDesc = new BlendStateDescription()
            {
                IndependentBlendEnable = false,
                AlphaToCoverageEnable = false,
            };
            // Additive blend state that darkens when overlapped
            blendDesc.RenderTarget[0] = new RenderTargetBlendDescription
            {
                IsBlendEnabled = true,
                BlendOperation = BlendOperation.Add,
                AlphaBlendOperation = BlendOperation.Add,
                SourceBlend = BlendOption.SourceAlpha,
                DestinationBlend = BlendOption.InverseSourceAlpha,
                SourceAlphaBlend = BlendOption.One,
                DestinationAlphaBlend = BlendOption.Zero,
                RenderTargetWriteMask = ColorWriteMaskFlags.All
            };

            blendDesc.RenderTarget[0].DestinationBlend = BlendOption.One;

            BlendStateDescription blendDescription = BlendStateDescription.Default();
            blendDescription.RenderTarget[0].IsBlendEnabled = true;
            blendDescription.RenderTarget[0].SourceBlend = BlendOption.One;
            blendDescription.RenderTarget[0].DestinationBlend = BlendOption.One;
            blendDescription.RenderTarget[0].BlendOperation = BlendOperation.Add;


            var depthDesc = new DepthStencilStateDescription
            {
                DepthComparison = Comparison.Less,
                DepthWriteMask = DepthWriteMask.Zero,
                IsDepthEnabled = true,
                IsStencilEnabled = false
            };

            _DState = new DepthStencilState(game.DeviceContext.Device, depthDesc);
            _blendState = new BlendState(game.DeviceContext.Device, blendDescription);
            #endregion

            worldMatrix = Matrix.Identity;
            viewMatrix = Matrix.LookAtLH(new Vector3(0, 0, 100), Vector3.Zero, Vector3.Up);
            projectionMatrix = Matrix.PerspectiveFovLH(MathUtil.PiOverFour, game.ViewRatio, 1f, 1000);
            m = new Matrixes();
            m.World = worldMatrix;
            m.View = viewMatrix;
            m.Proj = projectionMatrix;
            m.Size = 1f;
            m.Trans();

            _perFrame = new Buffer(game.DeviceContext.Device,
              Utilities.SizeOf<Matrixes>(),
              ResourceUsage.Default,
              BindFlags.ConstantBuffer,
              CpuAccessFlags.None,
              ResourceOptionFlags.None,
              0);
            game.DeviceContext.UpdateSubresource(ref m, _perFrame);

            _c = new Constants();
            _c.GroupDim = _groupSizeX;
            _c.MaxParticles = PARTICLES_COUNT;

            _csConstants = new Buffer(game.DeviceContext.Device,
              Utilities.SizeOf<Constants>(),
              ResourceUsage.Default,
              BindFlags.ConstantBuffer,
              CpuAccessFlags.None,
              ResourceOptionFlags.None,
              0);

            ShaderFlags shaderFlags = ShaderFlags.None;
#if DEBUG
            shaderFlags = ShaderFlags.Debug;
#endif
            using (var shaderByteCode = ShaderBytecode.CompileFromFile(@"Shaders\VS.hlsl", "VS", "vs_5_0", shaderFlags))
                _vs = new VertexShader(game.DeviceContext.Device, shaderByteCode);
            using (var shaderByteCode = ShaderBytecode.CompileFromFile(@"Shaders\GS.hlsl", "GS", "gs_5_0", shaderFlags))
                _gs = new GeometryShader(game.DeviceContext.Device, shaderByteCode);
            using (var shaderByteCode = ShaderBytecode.CompileFromFile(@"Shaders\PS.hlsl", "PS", "ps_5_0", shaderFlags))
                _ps = new PixelShader(game.DeviceContext.Device, shaderByteCode);
            using (var shaderByteCode = ShaderBytecode.CompileFromFile(@"Shaders\CS.hlsl", "CS", "cs_5_0", shaderFlags))
                _cs = new ComputeShader(game.DeviceContext.Device, shaderByteCode);

            _SRV = new ShaderResourceView(game.DeviceContext.Device, _particlesBuffer);

            UnorderedAccessViewDescription uavDesc = new UnorderedAccessViewDescription
            {
                Dimension = UnorderedAccessViewDimension.Buffer,
                Buffer = new UnorderedAccessViewDescription.BufferResource { FirstElement = 0 }
            };

            uavDesc.Format = Format.Unknown;
            uavDesc.Buffer.Flags = UnorderedAccessViewBufferFlags.None;
            uavDesc.Buffer.ElementCount = _particlesBuffer.Description.SizeInBytes / _particlesBuffer.Description.StructureByteStride;

            _UAV = new UnorderedAccessView(game.DeviceContext.Device, _particlesBuffer, uavDesc);

            SamplerStateDescription samplerDecription = SamplerStateDescription.Default();
            {
                samplerDecription.AddressU = TextureAddressMode.Clamp;
                samplerDecription.AddressV = TextureAddressMode.Clamp;
                samplerDecription.Filter = Filter.MinMagMipLinear;
            };

            _particleSampler = new SamplerState(game.DeviceContext.Device, samplerDecription);

            _texture = StaticMetods.LoadTextureFromFile(game.DeviceContext, "smoke5.png");

            game.DeviceContext.OutputMerger.DepthStencilState = _DState;
        }

        public override void Dispose()
        {
            Utilities.Dispose(ref _perFrame);
            Utilities.Dispose(ref _particlesBuffer);
            Utilities.Dispose(ref _SRV);
            Utilities.Dispose(ref _UAV);
            Utilities.Dispose(ref _particleSampler);
            Utilities.Dispose(ref _texture);
            Utilities.Dispose(ref _blendState);
            Utilities.Dispose(ref _DState);
            Utilities.Dispose(ref _vs);
            Utilities.Dispose(ref _ps);
            Utilities.Dispose(ref _gs);
            Utilities.Dispose(ref _cs);
            Utilities.Dispose(ref _csConstants);
        }

        protected override void Draw(float time)
        {
            game.DeviceContext.VertexShader.Set(_vs);
            game.DeviceContext.VertexShader.SetConstantBuffer(0, _perFrame);
            game.DeviceContext.VertexShader.SetShaderResource(0, _SRV);
            game.DeviceContext.PixelShader.Set(_ps);
            game.DeviceContext.PixelShader.SetShaderResource(0, _texture);
            game.DeviceContext.PixelShader.SetSampler(0, _particleSampler);
            game.DeviceContext.GeometryShader.Set(_gs);
            game.DeviceContext.GeometryShader.SetConstantBuffer(0, _perFrame);
            game.DeviceContext.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.PointList;
            game.DeviceContext.OutputMerger.BlendState = _blendState;
            game.DeviceContext.Draw(PARTICLES_COUNT, 0);
        }

        protected override void KeyKontroller(float time, SharpDX.DirectInput.KeyboardState kState)
        {
            float speed = 0.001f;
            var rotation = Matrix.Identity;
            var scale = Matrix.Identity;
            if (kState.IsPressed(SharpDX.DirectInput.Key.D))
                rotation = Matrix.RotationY(speed * time);
            if (kState.IsPressed(SharpDX.DirectInput.Key.A))
                rotation = Matrix.RotationY(-speed * time);
            if (kState.IsPressed(SharpDX.DirectInput.Key.W))
                rotation = Matrix.RotationX(speed * time);
            if (kState.IsPressed(SharpDX.DirectInput.Key.S))
                rotation = Matrix.RotationX(-speed * time);

            if (kState.IsPressed(SharpDX.DirectInput.Key.Up))
                scale = Matrix.Scaling(System.Math.Max(0.1f, 1 - speed * time));
            if (kState.IsPressed(SharpDX.DirectInput.Key.Down))
                scale = Matrix.Scaling(1 + speed * time);

            worldMatrix = worldMatrix * scale * rotation;
            m.World = worldMatrix;
            game.DeviceContext.UpdateSubresource(ref m, _perFrame);
        }

        protected override void Upadate(float time)
        {
            float angle = (float)time / 2000;
            Vector3 attractor = new Vector3((float)System.Math.Cos(angle), (float)System.Math.Cos(angle) * (float)System.Math.Sin(angle), (float)System.Math.Sin(angle));
            _c.DeltaTime = time/1000 ;
            _c.Atractor = attractor * 2;

            game.DeviceContext.UpdateSubresource(ref _c, _csConstants);
            game.DeviceContext.ComputeShader.Set(_cs);
            game.DeviceContext.ComputeShader.SetConstantBuffer(0, _csConstants);
            game.DeviceContext.ComputeShader.SetUnorderedAccessView(0, _UAV);
            game.DeviceContext.Dispatch(_groupSizeX, _groupSizeY, 1);
            game.DeviceContext.ComputeShader.SetUnorderedAccessView(0, null);
        }
    }
}
