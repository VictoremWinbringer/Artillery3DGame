using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DirectInput;
using SharpDX.DXGI;
using SharpDX.Windows;
using System.Diagnostics;
using VictoremLibrary;
using SharpDX.D3DCompiler;

namespace DifferedRendering
{
    struct PerObject
    {
        public Matrix WorldViewProjection;
        public Matrix World;
        public Matrix View;
        public uint HasTexture;
        public uint HasNormalMap;
        public uint HasSpecMap;
        uint padding0;
        public void Transpose()
        {
            WorldViewProjection.Transpose();
            World.Transpose();
            View.Transpose();
        }
    };
    class AppMy : IDisposable
    {
        PerObject _const;
        Matrix _view;
        Matrix _proj;
        Factory _factory;
        //Форма куда будем вставлять наше представление renderTargetView.
        private RenderForm _renderForm = null;
        //Объектное представление нашей видеокарты
        private SharpDX.Direct3D11.Device _dx11Device = null;
        private DeviceContext _dx11DeviceContext = null;
        //Цепочка замены заднего и отображаемого буфера
        private SwapChain _swapChain = null;
        //Представление куда мы выводим картинку.
        RenderTargetView _renderView = null;
        DepthStencilView _depthView = null;
        //Управление через клавиатуру
        DirectInput _directInput;
        Keyboard _keyboard;
        Stopwatch _stopWatch = new Stopwatch();
        //Шейдеры
        VertexShader fillGBufferVS;
        PixelShader fillGBufferPS;
        InputLayout _inputLayout;
        private ModelSDX model;
        //Свойства
        public float ViewRatio { get; private set; }
        public DeviceContext DeviceContext { get { return _dx11DeviceContext; } }
        public SharpDX.Windows.RenderForm Form { get { return _renderForm; } }
        public SwapChain SwapChain { get { return _swapChain; } }
        public int Width { get { return _renderForm.Width; } }
        public int Height { get { return _renderForm.Height; } }
        public Color Color { get; set; }
        public RenderTargetView RenderView { get { return _renderView; } }
        public DepthStencilView DepthView { get { return _depthView; } }


        /// <summary>
        /// Конструктор класса
        /// </summary>
        /// <param name="renderForm">Форма в котору будем рисовать наши объекты</param>
        public AppMy(RenderForm renderForm)
        {
            Color = new Color(0, 0, 128);

            _renderForm = renderForm;

            ViewRatio = (float)_renderForm.Width / _renderForm.Height;

            InitializeDeviceResources();

            model = new ModelSDX(_dx11Device, "textures\\", "sponza.obj");
            model.World = Matrix.Identity;
            _constBuffer0 = new SharpDX.Direct3D11.Buffer(_dx11Device, Utilities.SizeOf<PerObject>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            _constBuffer1 = new SharpDX.Direct3D11.Buffer(_dx11Device, Utilities.SizeOf<Matrix>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);

            _view = Matrix.LookAtLH(new Vector3(0, 0, -500), Vector3.Zero, Vector3.Up);
            _proj = Matrix.PerspectiveFovLH(MathUtil.PiOverFour, Width / (float)Height, 1f, 10000f);

            _sampler = new SamplerState(_dx11Device, new SamplerStateDescription()
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
            });
            InputElement[] PosNormalTexTanBi = {
                new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, InputClassification.PerVertexData, 0),
                new InputElement("NORMAL", 0, Format.R32G32B32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData, 0),
                new InputElement("TEXCOORD", 0, Format.R32G32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData, 0),
                new InputElement("TANGENT", 0, Format.R32G32B32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData,0 ),
                 new InputElement("BINORMAL", 0, Format.R32G32B32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData,0 ),
                     };


            ShaderFlags flags = ShaderFlags.None;
#if DEBUG
            flags |= ShaderFlags.Debug | ShaderFlags.SkipOptimization;
#endif
            using (var bytecode = ShaderBytecode.CompileFromFile(@"Shaders\FillGBuffer.hlsl", "VSFillGBuffer", "vs_5_0", flags))
            using (var _inputSignature = ShaderSignature.GetInputSignature(bytecode))
            {
                _inputLayout = new InputLayout(_dx11Device, _inputSignature, PosNormalTexTanBi);
                fillGBufferVS = new VertexShader(_dx11Device, bytecode);
            }


            using (var bytecode = ShaderBytecode.CompileFromFile(@"Shaders\FillGBuffer.hlsl", "PSFillGBuffer", "ps_5_0", flags))
            {
                fillGBufferPS = new PixelShader(_dx11Device, bytecode);
            }
            using (var bytecode = ShaderBytecode.CompileFromFile(@"Shaders\GBufferD.hlsl", "PS_GBufferNormal", "ps_5_0", flags))
                gBufferNormalPS = new PixelShader(_dx11Device, bytecode);

            saQuad = new ScreenAlignedQuadRenderer(_dx11Device);
            saQuad.Shader = gBufferNormalPS;
            gbuffer = new GBuffer(this.Width, this.Height, new SampleDescription(1, 0), _dx11Device, Format.R8G8B8A8_UNorm, Format.R32_UInt, Format.R8G8B8A8_UNorm);


            _directInput = new DirectInput();
            _keyboard = new Keyboard(_directInput);
            _keyboard.Properties.BufferSize = 128;
            _keyboard.Acquire();
            _stopWatch.Reset();
        }

        /// <summary>
        /// Инициализирует объекты связанные с графическим устройство - Девайс его контекст и Свапчейн
        /// </summary>
        private void InitializeDeviceResources()
        {
            var creationFlags = DeviceCreationFlags.None;
#if DEBUG
            creationFlags = DeviceCreationFlags.Debug;
#endif

            //Создаем объектное преставление нашего GPU, его контекст и класс который будет менят местами буфферы в которые рисует наша GPU
            SharpDX.Direct3D11.Device.CreateWithSwapChain(
                 SharpDX.Direct3D.DriverType.Hardware,
                 creationFlags | DeviceCreationFlags.BgraSupport,
                 new[] { SharpDX.Direct3D.FeatureLevel.Level_11_0 },
                  new SwapChainDescription()
                  {
                      ModeDescription = new ModeDescription(
                         _renderForm.Width,
                         _renderForm.Height,
                          new Rational(60, 1),
                          Format.R8G8B8A8_UNorm),
                      SampleDescription = new SampleDescription(4, 0),
                      Usage = Usage.BackBuffer | Usage.RenderTargetOutput,
                      BufferCount = 2,
                      OutputHandle = _renderForm.Handle,
                      IsWindowed = true,
                      SwapEffect = SwapEffect.Discard,
                      Flags = SwapChainFlags.None
                  },
                 out _dx11Device,
                 out _swapChain);
            //Игноровать все события видновс
            _factory = _swapChain.GetParent<SharpDX.DXGI.Factory>();
            _factory.MakeWindowAssociation(_renderForm.Handle, WindowAssociationFlags.IgnoreAll);
            // Создаем буффер и вьюшку глубины
            using (var _depthBuffer = new Texture2D(
                  _dx11Device,
                  new Texture2DDescription()
                  {
                      Format = Format.D32_Float_S8X24_UInt,
                      ArraySize = 1,
                      MipLevels = 1,
                      Width = _renderForm.Width,
                      Height = _renderForm.Height,
                      SampleDescription = _swapChain.Description.SampleDescription,
                      Usage = ResourceUsage.Default,
                      BindFlags = BindFlags.DepthStencil,
                      CpuAccessFlags = CpuAccessFlags.None,
                      OptionFlags = ResourceOptionFlags.None
                  }))
                _depthView = new DepthStencilView(_dx11Device, _depthBuffer, new SharpDX.Direct3D11.DepthStencilViewDescription()
                {
                    Dimension = (SwapChain.Description.SampleDescription.Count > 1 ||
                     SwapChain.Description.SampleDescription.Quality > 0) ?
                     DepthStencilViewDimension.Texture2DMultisampled :
                     DepthStencilViewDimension.Texture2D,
                    Flags = DepthStencilViewFlags.None
                });
            //Создаем буффер и вьюшку для рисования
            using (Texture2D backBuffer = _swapChain.GetBackBuffer<Texture2D>(0))
                _renderView = new RenderTargetView(_dx11Device, backBuffer);
            //Создаем контекст нашего GPU
            _dx11DeviceContext = _dx11Device.ImmediateContext;
            //Устанавливаем размер конечной картинки            
            _dx11DeviceContext.Rasterizer.SetViewport(0, 0, _renderForm.Width, _renderForm.Height);
            _dx11DeviceContext.OutputMerger.SetTargets(_depthView, _renderView);
        }


        private void Update(float time)
        {
            var m = _keyboard.GetCurrentState();
            if (m.PressedKeys.Count > 0)
                SetViewMatrix(time, m);
        }
        protected virtual void SetViewMatrix(float time, SharpDX.DirectInput.KeyboardState kState)
        {
            float speed = 0.001f;
            var rotation = Matrix.Identity;
            var translation = Matrix.Identity;
            if (kState.IsPressed(SharpDX.DirectInput.Key.D))
                rotation = Matrix.RotationY(-speed * time);
            if (kState.IsPressed(SharpDX.DirectInput.Key.A))
                rotation = Matrix.RotationY(speed * time);
            if (kState.IsPressed(SharpDX.DirectInput.Key.W))
                rotation = Matrix.RotationX(speed * time);
            if (kState.IsPressed(SharpDX.DirectInput.Key.S))
                rotation = Matrix.RotationX(-speed * time);
            if (kState.IsPressed(SharpDX.DirectInput.Key.Left))
                rotation = Matrix.RotationZ(speed * time);
            if (kState.IsPressed(SharpDX.DirectInput.Key.Right))
                rotation = Matrix.RotationZ(-speed * time);

            if (kState.IsPressed(SharpDX.DirectInput.Key.Up))
                translation = Matrix.Translation(0,0,-speed*time*100);
            if (kState.IsPressed(SharpDX.DirectInput.Key.Down))
                translation = Matrix.Translation(0, 0,speed * time*100);

            _view = _view  * rotation*translation;
        }
        private void Draw(float time)
        {
            _dx11DeviceContext.InputAssembler.InputLayout = _inputLayout;
            _dx11DeviceContext.ClearRenderTargetView(_renderView, Color);
            _dx11DeviceContext.ClearDepthStencilView(_depthView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);

            _const.World = model.World;
            _const.View = _view;
            _const.WorldViewProjection = model.World * _view * _proj;
            _const.Transpose();
          
            _dx11DeviceContext.VertexShader.Set(fillGBufferVS);
            _dx11DeviceContext.PixelShader.Set(fillGBufferPS);
            _dx11DeviceContext.VertexShader.SetConstantBuffer(0, _constBuffer0);
            _dx11DeviceContext.PixelShader.SetConstantBuffer(0, _constBuffer0);
            _dx11DeviceContext.VertexShader.SetSampler(0, _sampler);
            _dx11DeviceContext.PixelShader.SetSampler(0, _sampler);
            gbuffer.Clear(_dx11DeviceContext, new Color(0, 0, 0, 1));
            gbuffer.Bind(_dx11DeviceContext);
            foreach (var item in model.Meshes3D)
            {
                _const.HasTexture = item.Texture != null ? 1u : 0;
                _dx11DeviceContext.UpdateSubresource(ref _const, _constBuffer0);
                _dx11DeviceContext.VertexShader.SetShaderResource(0, item.Texture);
                _dx11DeviceContext.PixelShader.SetShaderResource(0, item.Texture);
                _dx11DeviceContext.InputAssembler.PrimitiveTopology = item.primitiveType;
                _dx11DeviceContext.InputAssembler.SetVertexBuffers(0, item.VertexBinding);
                _dx11DeviceContext.InputAssembler.SetIndexBuffer(item.IndexBuffer, SharpDX.DXGI.Format.R32_UInt, 0);
                _dx11DeviceContext.OutputMerger.SetBlendState(null, null);
                _dx11DeviceContext.DrawIndexed(item.IndexCount, 0, 0);

            }
            gbuffer.Unbind(_dx11DeviceContext);
            saQuad.ShaderResources = gbuffer.SRVs.ToArray().Concat(new[] { gbuffer.DSSRV }).ToArray();
            _dx11DeviceContext.OutputMerger.SetRenderTargets(this._depthView, this._renderView);

           
            var invView =Matrix.Invert( _proj);
            invView.Transpose();

            _dx11DeviceContext.UpdateSubresource(ref invView, _constBuffer1);
            _dx11DeviceContext.PixelShader.SetConstantBuffer(0, _constBuffer1);           
            saQuad.Render();

            _swapChain.Present(0, PresentFlags.None);
        }

        /// <summary>
        /// Запускает бесконечный цикл игры
        /// </summary>
        public void Run()
        {
            RenderLoop.Run(_renderForm, RenderCallback);
        }

        double totalTime = 0;
        private GBuffer gbuffer;
        private SharpDX.Direct3D11.Buffer _constBuffer0;
        private SharpDX.Direct3D11.Buffer _constBuffer1;
        private SamplerState _sampler;
        private Shader _shader;
        private PixelShader gBufferNormalPS;
        private ScreenAlignedQuadRenderer saQuad;

        private void RenderCallback()
        {
            var elapsed = _stopWatch.ElapsedMilliseconds;
            totalTime += elapsed;
            _stopWatch.Reset();
            _stopWatch.Start();

            if (totalTime > 30)
            {
                Update((float)totalTime);
                totalTime = 0;
            }
            Draw(elapsed);
        }

        public void Dispose()
        {
            Utilities.Dispose(ref _keyboard);
            Utilities.Dispose(ref _directInput);
            Utilities.Dispose(ref _renderView);
            Utilities.Dispose(ref _factory);
            Utilities.Dispose(ref _depthView);
            Utilities.Dispose(ref _dx11DeviceContext);
            Utilities.Dispose(ref fillGBufferPS);
            Utilities.Dispose(ref fillGBufferVS);
            Utilities.Dispose(ref gbuffer);
            Utilities.Dispose(ref _constBuffer0);
            Utilities.Dispose(ref _constBuffer1);
            Utilities.Dispose(ref _sampler);
            Utilities.Dispose(ref _inputLayout);
            Utilities.Dispose(ref gBufferNormalPS);
            saQuad?.Dispose();
            _shader?.Dispose();
            model?.Dispose();
            Utilities.Dispose(ref _swapChain);
            Utilities.Dispose(ref _dx11Device);

            _swapChain?.Dispose();
            _dx11Device?.Dispose();
        }

    }
}
