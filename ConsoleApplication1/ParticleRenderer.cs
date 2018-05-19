using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VictoremLibrary;
using SharpDX.D3DCompiler;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;

namespace ConsoleApplication1
{
    public struct PerObject
    {
        public Matrix WVP;
        public Vector4 CP;
    }
    // Structure for particle
    public struct Particle
    {
        public Vector3 Position;
        public float Radius;
        public Vector3 OldPosition;
        public float Energy;
    }
    // Particle constants (updated on initialization) 
    public struct ParticleConstants
    {
        public Vector3 DomainBoundsMin;
        public float ForceStrength;
        public Vector3 DomainBoundsMax;
        public float MaxLifetime;
        public Vector3 ForceDirection;
        public int MaxParticles;
        public Vector3 Attractor;
        public float Radius;
    }
    // particle constant buffer updated per frame
    public struct ParticleFrame
    {
        public float Time;
        public float FrameTime;
        public uint RandomSeed;
        uint _padding0;
    }

    class ParticleRenderer : IDisposable
    {
        #region Fields

        Device device;
        DeviceContext context;
        // Private member fields
        Buffer indirectArgsBuffer;
        List<Buffer> particleBuffers = new List<Buffer>();
        List<ShaderResourceView> particleSRVs = new List<ShaderResourceView>();
        List<UnorderedAccessView> particleUAVs = new List<UnorderedAccessView>();
        private VertexShader vertexShader;
        private PixelShader pixelShader;
        private BlendState blendState;
        private BlendState blendStateLight;
        private DepthStencilState disableDepthWrite;
        private ShaderResourceView particleTextureSRV;
        public Buffer perFrame;
        public Buffer perComputeBuffer;
        private SamplerState linearSampler;
        Random random = new Random();
        float limiter = 0f;
        float genTime = 0f; // time since Generator last run       
        //Public member fields
        public bool UseLightenBlend;
        public Dictionary<String, ComputeShader> computeShaders = new Dictionary<string, ComputeShader>();
        public int ThreadsX = 128; // default thread group width
        public int ThreadsY = 1;   // default thread group height 
        public ParticleConstants Constants;
        public ParticleFrame Frame;
        public PerObject Object;
        public int ParticlesPerBatch = 16;
        public const int GeneratorThreadsX = 16;
        private Buffer perObject;
        #endregion

        public void Dispose()
        {
            Utilities.Dispose(ref indirectArgsBuffer);
            particleBuffers.ForEach(x => Utilities.Dispose(ref x));
            particleSRVs.ForEach(x => Utilities.Dispose(ref x));
            particleUAVs.ForEach(x => Utilities.Dispose(ref x));
            computeShaders.Values.ToList().ForEach(x => Utilities.Dispose(ref x));
            Utilities.Dispose(ref vertexShader);
            Utilities.Dispose(ref pixelShader);
            Utilities.Dispose(ref blendState);
            Utilities.Dispose(ref blendStateLight);
            Utilities.Dispose(ref disableDepthWrite);
            Utilities.Dispose(ref particleTextureSRV);
            Utilities.Dispose(ref perFrame);
            Utilities.Dispose(ref perComputeBuffer);
            Utilities.Dispose(ref linearSampler);
            Utilities.Dispose(ref perObject);
        }

        public ParticleRenderer(Game game)
        {
            device = game.DeviceContext.Device;
            context = game.DeviceContext;
            this.Constants.DomainBoundsMin = new Vector3(-15, -15, 15);
            this.Constants.DomainBoundsMax = new Vector3(0, 0, 0);
            CreateDeviceDependentResources();
        }

        void CreateDeviceDependentResources()
        {
            #region Compile Vertex/Pixel shaders
            // Compile and create the vertex shader
            using (var vsBytecode = ShaderBytecode.CompileFromFile("ParticleVS.hlsl",
                "VSMain",
                "vs_5_0",
                ShaderFlags.None,
                EffectFlags.None,
                null,
                new HLSLFileIncludeHandler(Environment.CurrentDirectory)))
            using (var psBytecode = ShaderBytecode.CompileFromFile("ParticlePS.hlsl",
                "PSMain",
                "ps_5_0",
                ShaderFlags.None,
                EffectFlags.None,
                null,
                new HLSLFileIncludeHandler(Environment.CurrentDirectory)))
            {
                vertexShader = new VertexShader(device, vsBytecode);
                pixelShader = new PixelShader(device, psBytecode);
            }
            #endregion

            #region Blend States
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
            blendState = new BlendState(device, blendDesc);
            // Additive blend state that lightens when overlapped 
            // (needs a dark background)
            blendDesc.RenderTarget[0].DestinationBlend = BlendOption.One;
            blendStateLight = new BlendState(device, blendDesc);
            #endregion

            // depth stencil state to disable Z-buffer write 
            disableDepthWrite = new DepthStencilState(device,
                new DepthStencilStateDescription
                {
                    DepthComparison = Comparison.Less,
                    DepthWriteMask = SharpDX.Direct3D11.DepthWriteMask.Zero,
                    IsDepthEnabled = true,
                    IsStencilEnabled = false
                });

            // Create the per compute shader constant buffer
            perComputeBuffer = new Buffer(device,
                Utilities.SizeOf<ParticleConstants>(),
                ResourceUsage.Default,
                BindFlags.ConstantBuffer,
                CpuAccessFlags.None,
                ResourceOptionFlags.None,
                0);

            // Create the particle frame buffer 
            perFrame = new Buffer(device,
                Utilities.SizeOf<ParticleFrame>(),
                ResourceUsage.Default,
                BindFlags.ConstantBuffer,
                CpuAccessFlags.None,
                ResourceOptionFlags.None,
                0);
            perObject = new Buffer(device,
                Utilities.SizeOf<PerObject>(),
                ResourceUsage.Default,
                BindFlags.ConstantBuffer,
                CpuAccessFlags.None,
                ResourceOptionFlags.None,
                0);

            particleTextureSRV = StaticMetods.LoadTextureFromFile(context, "Particle.png");

            // Create a linear sampler
            linearSampler = new SamplerState(device, new SamplerStateDescription
            {
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                Filter = Filter.MinMagMipLinear, // Bilinear
                MaximumLod = float.MaxValue,
                MinimumLod = 0,
            });
        }

        public static UnorderedAccessView CreateBufferUAV(SharpDX.Direct3D11.Device device, Buffer buffer, UnorderedAccessViewBufferFlags flags = UnorderedAccessViewBufferFlags.None)
        {
            UnorderedAccessViewDescription uavDesc = new UnorderedAccessViewDescription
            {
                Dimension = UnorderedAccessViewDimension.Buffer,
                Buffer = new UnorderedAccessViewDescription.BufferResource { FirstElement = 0 }
            };
            if ((buffer.Description.OptionFlags & ResourceOptionFlags.BufferAllowRawViews) == ResourceOptionFlags.BufferAllowRawViews)
            {
                // A raw buffer requires R32_Typeless
                uavDesc.Format = Format.R32_Typeless;
                uavDesc.Buffer.Flags = UnorderedAccessViewBufferFlags.Raw | flags;
                uavDesc.Buffer.ElementCount = buffer.Description.SizeInBytes / 4;
            }
            else if ((buffer.Description.OptionFlags & ResourceOptionFlags.BufferStructured) == ResourceOptionFlags.BufferStructured)
            {
                uavDesc.Format = Format.Unknown;
                uavDesc.Buffer.Flags = flags;
                uavDesc.Buffer.ElementCount = buffer.Description.SizeInBytes / buffer.Description.StructureByteStride;
            }
            else
            {
                throw new ArgumentException("Buffer must be raw or structured", "buffer");
            }

            return new UnorderedAccessView(device, buffer, uavDesc);
        }


        // Initialize the particle buffers 
        public void InitializeParticles(int maxParticles, float maxLifetime)
        {
            this.Constants.MaxParticles = maxParticles;
            this.Constants.MaxLifetime = maxLifetime;
            // How often and how many particles to generate  
            this.ParticlesPerBatch = (int)(maxParticles * 0.0128f);
            this.limiter = (float)(Math.Ceiling(ParticlesPerBatch / 16.0) * 16.0 * maxLifetime) / (float)maxParticles;

            #region Create Buffers and Views 
            // Create 2 buffers, these are our append/consume 
            // buffers and will be swapped each frame
            particleBuffers.Add(new Buffer(device,
                Utilities.SizeOf<Particle>() * maxParticles,
                ResourceUsage.Default,
                BindFlags.ShaderResource | BindFlags.UnorderedAccess,
                CpuAccessFlags.None,
                ResourceOptionFlags.BufferStructured,
                Utilities.SizeOf<Particle>()));
            particleBuffers.Add(new Buffer(device,
               Utilities.SizeOf<Particle>() * maxParticles,
               ResourceUsage.Default,
               BindFlags.ShaderResource | BindFlags.UnorderedAccess,
               CpuAccessFlags.None,
               ResourceOptionFlags.BufferStructured,
               Utilities.SizeOf<Particle>()));

            // Create a UAV and SRV for each particle buffer
            particleUAVs.Add(CreateBufferUAV(device,
                particleBuffers[0],
            UnorderedAccessViewBufferFlags.Append));
            particleUAVs.Add(CreateBufferUAV(device,
                particleBuffers[1],
                UnorderedAccessViewBufferFlags.Append));
            particleSRVs.Add(new ShaderResourceView(device,
                particleBuffers[0]));
            particleSRVs.Add(new ShaderResourceView(device,
                particleBuffers[1]));
            // Set the starting number of particles to 0 
            context.ComputeShader.SetUnorderedAccessView(0, particleUAVs[0], 0);
            context.ComputeShader.SetUnorderedAccessView(1, particleUAVs[1], 0);

            // Create particle count buffers:
            var bufDesc = new BufferDescription
            {
                BindFlags = SharpDX.Direct3D11.BindFlags.ConstantBuffer,
                SizeInBytes = 4 * SharpDX.Utilities.SizeOf<uint>(),
                StructureByteStride = 0,
                Usage = ResourceUsage.Default,
                CpuAccessFlags = SharpDX.Direct3D11.CpuAccessFlags.None,
            };
            // Used as input to the context.DrawInstancedIndirect
            // The 4 elements represent the 4 parameters 
            bufDesc.OptionFlags = ResourceOptionFlags.DrawIndirectArguments;
            bufDesc.BindFlags = BindFlags.None;
            indirectArgsBuffer = new Buffer(device, bufDesc);
            // 4 vertices per instance (i.e. quad)
            device.ImmediateContext.UpdateSubresource(new uint[4] { 4, 0, 0, 0 }, indirectArgsBuffer);
            #endregion

            // Update the ParticleConstants buffer 
            device.ImmediateContext.UpdateSubresource(ref Constants, perComputeBuffer);
        }

        public void Update(string generatorCS, string updaterCS)
        {
            var append = particleUAVs[0];
            var consume = particleUAVs[1];
            // Assign UAV of particles  
            context.ComputeShader.SetUnorderedAccessView(0, append);
            context.ComputeShader.SetUnorderedAccessView(1, consume);
            // Update the constant buffers  
            // Generate the next random seed for particle generator 
            Frame.RandomSeed = (uint)random.Next(int.MinValue, int.MaxValue);
            context.UpdateSubresource(ref Frame, perFrame);
            // Copy current consume buffer count into perFrame  
            context.CopyStructureCount(perFrame, 4 * 3, consume);
            context.ComputeShader.SetConstantBuffer(0, perComputeBuffer);
            context.ComputeShader.SetConstantBuffer(1, perFrame);
            // Update existing particles  
            UpdateCS(updaterCS, append, consume);
            // Generate new particles (if reached limiter time) 
            genTime += Frame.FrameTime;
            if (genTime > limiter)
            {
                genTime = 0;
                GenerateCS(generatorCS, append);
            }
            // Retrieve the particle count for the render phase   
            context.CopyStructureCount(indirectArgsBuffer, 4, append);
            // Clear the shader and resources from pipeline stage  
            context.ComputeShader.SetUnorderedAccessViews(0, null, null, null);
            context.ComputeShader.SetUnorderedAccessViews(1, null, null, null);
            context.ComputeShader.Set(null);
            // Flip UAVs/SRVs 
            particleUAVs[0] = consume;
            particleUAVs[1] = append;
            var s = particleSRVs[0];
            particleSRVs[0] = particleSRVs[1];
            particleSRVs[1] = s;
        }

        private void UpdateCS(string csName, UnorderedAccessView append, UnorderedAccessView consume)
        {  // Compile the shader if it isn't already 
            if (!computeShaders.ContainsKey(csName)) CompileComputeShader(csName);
            // Set the shader to run  
            context.ComputeShader.Set(computeShaders[csName]);
            // Dispatch the compute shader thread groups 
            context.Dispatch((int)Math.Ceiling(Constants.MaxParticles / (double)ThreadsX), 1, 1);
        }

        private void GenerateCS(string name, UnorderedAccessView append)
        {    // Compile the shader if it isn't already  
            if (!computeShaders.ContainsKey(name))
            {
                int oldThreadsX = ThreadsX;
                ThreadsX = GeneratorThreadsX;
                CompileComputeShader(name);
                ThreadsX = oldThreadsX;
            }
            // Set the shader to run 
            context.ComputeShader.Set(computeShaders[name]);
            // Dispatch the compute shader thread groups 
            context.Dispatch((int)Math.Ceiling(ParticlesPerBatch / 16.0), 1, 1);
        }

        // Compile compute shader from file
        public void CompileComputeShader(string csFunction, string csFile = "ParticleCS.hlsl")
        {
            SharpDX.Direct3D.ShaderMacro[] defines = new[] {
                new SharpDX.Direct3D.ShaderMacro("THREADSX",             ThreadsX),
                new SharpDX.Direct3D.ShaderMacro("THREADSY",             ThreadsY),
            };
            using (var bytecode = ShaderBytecode.CompileFromFile(csFile, csFunction, "cs_5_0", ShaderFlags.None, EffectFlags.None, defines, new HLSLFileIncludeHandler(Environment.CurrentDirectory)))
            {
                computeShaders[csFunction] = new ComputeShader(device, bytecode);
            }
        }

        void DoRender()
        {    // Retrieve existing pipeline states for backup  
            RawColor4 oldBlendFactor;
            int oldSampleMask;
            int oldStencil;
            var oldPSBufs = context.PixelShader.GetConstantBuffers(0, 1);
            using (var oldVS = context.VertexShader.Get())
            using (var oldPS = context.PixelShader.Get())
            using (var oldGS = context.GeometryShader.Get())
            using (var oldSamp = context.PixelShader.GetSamplers(0, 1).FirstOrDefault())
            using (var oldBlendState = context.OutputMerger.GetBlendState(out oldBlendFactor, out oldSampleMask))
            using (var oldIA = context.InputAssembler.InputLayout)
            using (var oldDepth = context.OutputMerger.GetDepthStencilState(out oldStencil))
            {
                #region DrawingLogic
                // There is no input layout for this renderer
                context.InputAssembler.InputLayout = null;
                // The triangle strip input topology 
                context.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleStrip;
                // Disable depth write
                context.OutputMerger.SetDepthStencilState(disableDepthWrite);
                // Set the additive blend state
                if (!UseLightenBlend)
                    context.OutputMerger.SetBlendState(blendState, null, 0xFFFFFFFF);
                else context.OutputMerger.SetBlendState(blendStateLight, Color.White, 0xFFFFFFFF);
                // Assign consume particle buffer SRV to vertex shader
                context.VertexShader.SetShaderResource(0, particleSRVs[1]);
                context.VertexShader.Set(vertexShader);
                // Set pixel shader resources
                context.PixelShader.SetShaderResource(0, particleTextureSRV);
                context.PixelShader.SetSampler(0, linearSampler);
                context.PixelShader.Set(pixelShader);
                SetConstants();
                // Draw the number of quad instances stored in the
                // indirectArgsBuffer. The vertex shader will rely upon 
                // the SV_VertexID and SV_InstanceID input semantics
                context.DrawInstancedIndirect(indirectArgsBuffer, 0);
                #endregion

                // Restore previous pipeline state  
                context.VertexShader.Set(oldVS);
                context.PixelShader.SetConstantBuffers(0, oldPSBufs);
                context.PixelShader.Set(oldPS);
                context.GeometryShader.Set(oldGS);
                context.PixelShader.SetSampler(0, oldSamp);
                context.InputAssembler.InputLayout = oldIA;
                // Restore previous blend and depth state   
                context.OutputMerger.SetBlendState(oldBlendState, oldBlendFactor, oldSampleMask);
                context.OutputMerger.SetDepthStencilState(oldDepth, oldStencil);
            }
        }

        void SetConstants()
        {
            context.UpdateSubresource(ref Object, perObject);
            context.VertexShader.SetConstantBuffer(0, perComputeBuffer);
            context.VertexShader.SetConstantBuffer(1, perFrame);
            context.VertexShader.SetConstantBuffer(2, perObject);
            context.PixelShader.SetConstantBuffer(0, perComputeBuffer);
            context.PixelShader.SetConstantBuffer(1, perFrame);


        }
        public void Render()
        {
            DoRender();
        }

    }
}
