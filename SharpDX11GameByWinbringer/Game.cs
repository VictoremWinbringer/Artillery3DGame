using DX11 = SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX;
using SharpDX.DirectInput;

namespace SharpDX11GameByWinbringer
{
    /// <summary>
    /// Основной класс игры. Наша Вьюшка. Отвечает за инициализацию графики и игровой цкил отображения данных на экран.
    /// </summary>
    public sealed class Game : System.IDisposable
    {
        //События
        public delegate void UpdateDraw(double t);
        public delegate void KeyPress(KeyboardState kState, float time);

        public event KeyPress OnKeyPressed = null;
        public event UpdateDraw OnUpdate = null;
        public event UpdateDraw OnDraw = null;
        //Поля
        Factory _factory;
        //Форма куда будем вставлять наше представление renderTargetView.
        private SharpDX.Windows.RenderForm _renderForm = null;
        //Объектное представление нашей видеокарты
        private DX11.Device _dx11Device = null;
        private DX11.DeviceContext _dx11DeviceContext = null;
        //Цепочка замены заднего и отображаемого буфера
        private SwapChain _swapChain;
        //Представление куда мы выводим картинку.
        private DX11.RenderTargetView _renderView = null;
        private DX11.DepthStencilView _depthView = null;
        private Presenter _presenter = null;
        //Управление через клавиатуру
        DirectInput _directInput;
        Keyboard _keyboard;
        Mouse _mouse;
        bool isPaused = false;
        //Свойства
        public float ViewRatio { get; set; }
        public DX11.DeviceContext DeviceContext { get { return _dx11DeviceContext; } }
        public SharpDX.Windows.RenderForm Form { get { return _renderForm; } }
        public SwapChain SwapChain { get { return _swapChain; } }
        public int Width { get { return _renderForm.ClientSize.Width; } }
        public int Height { get { return _renderForm.ClientSize.Height; } }

        public Game(SharpDX.Windows.RenderForm renderForm)
        {
            _renderForm = renderForm;
            _renderForm.KeyDown += (sender, e) => { if (e.KeyCode == System.Windows.Forms.Keys.P) isPaused = !isPaused; };
            ViewRatio = (float)_renderForm.ClientSize.Width / _renderForm.ClientSize.Height;

            InitializeDeviceResources();

            _directInput = new DirectInput();
            _keyboard = new Keyboard(_directInput);
            _keyboard.Properties.BufferSize = 128;
            _keyboard.Acquire();
            _presenter = new Presenter(this);
        }

        #region IDisposable Support
        private bool disposedValue = false;
        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: освободить управляемое состояние (управляемые объекты).
                    Utilities.Dispose(ref _keyboard);
                    Utilities.Dispose(ref _mouse);
                    Utilities.Dispose(ref _directInput);
                    Utilities.Dispose(ref _presenter);
                    Utilities.Dispose(ref _renderView);
                    Utilities.Dispose(ref _swapChain);
                    Utilities.Dispose(ref _factory);
                    Utilities.Dispose(ref _depthView);
                    Utilities.Dispose(ref _dx11Device);
                    Utilities.Dispose(ref _dx11DeviceContext);
                    _mouse?.Dispose();
                    _dx11Device?.Dispose();
                    _swapChain?.Dispose();
                }

                // TODO: освободить неуправляемые ресурсы (неуправляемые объекты) и переопределить ниже метод завершения.
                // TODO: задать большим полям значение NULL.
                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

        /// <summary>
        /// Инициализирует объекты связанные с графическим устройство - Девайс его контекст и Свапчейн
        /// </summary>
        private void InitializeDeviceResources()
        {
            //Создаем объектное преставление нашего GPU, его контекст и класс который будет менят местами буфферы в которые рисует наша GPU
            DX11.Device.CreateWithSwapChain(
                SharpDX.Direct3D.DriverType.Hardware,
                DX11.DeviceCreationFlags.None | DX11.DeviceCreationFlags.BgraSupport,
                new[] { SharpDX.Direct3D.FeatureLevel.Level_11_0 },
                 new SwapChainDescription()
                 {
                     ModeDescription = new ModeDescription(
                        _renderForm.ClientSize.Width,
                        _renderForm.ClientSize.Height,
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
            _factory = _swapChain.GetParent<Factory>();
            _factory.MakeWindowAssociation(_renderForm.Handle, WindowAssociationFlags.IgnoreAll);
            // Создаем буффер и вьюшку глубины
            using (var _depthBuffer = new DX11.Texture2D(
                  _dx11Device,
                  new DX11.Texture2DDescription()
                  {
                      Format = Format.D32_Float_S8X24_UInt,
                      ArraySize = 1,
                      MipLevels = 1,
                      Width = _renderForm.ClientSize.Width,
                      Height = _renderForm.ClientSize.Height,
                      SampleDescription = _swapChain.Description.SampleDescription,
                      Usage = DX11.ResourceUsage.Default,
                      BindFlags = DX11.BindFlags.DepthStencil,
                      CpuAccessFlags = DX11.CpuAccessFlags.None,
                      OptionFlags = DX11.ResourceOptionFlags.None
                  }))
                _depthView = new DX11.DepthStencilView(_dx11Device, _depthBuffer, new SharpDX.Direct3D11.DepthStencilViewDescription()
                {
                    Dimension = (SwapChain.Description.SampleDescription.Count > 1 ||
                     SwapChain.Description.SampleDescription.Quality > 0) ?
                     DX11.DepthStencilViewDimension.Texture2DMultisampled :
                     DX11.DepthStencilViewDimension.Texture2D,
                    Flags = DX11.DepthStencilViewFlags.None
                });
            //Создаем буффер и вьюшку для рисования
            using (DX11.Texture2D backBuffer = _swapChain.GetBackBuffer<DX11.Texture2D>(0))
                _renderView = new DX11.RenderTargetView(_dx11Device, backBuffer);
            //Создаем контекст нашего GPU
            _dx11DeviceContext = _dx11Device.ImmediateContext;
            //Устанавливаем размер конечной картинки            
            _dx11DeviceContext.Rasterizer.SetViewport(0, 0, _renderForm.ClientSize.Width, _renderForm.ClientSize.Height);
            _dx11DeviceContext.OutputMerger.SetTargets(_depthView, _renderView);
        }

        private void Update(double time)
        {
            if (isPaused) return;
            // Poll events from joystick
            // keyboard.Poll();
            var m = _keyboard.GetCurrentState();
            if (m.PressedKeys.Count > 0)
                OnKeyPressed?.Invoke(m, (float)time);
            //var v = _mouse.GetCurrentState();
            //if (v.Buttons[0]) System.Windows.Forms.MessageBox.Show("Мыш нажата!");
            OnUpdate?.Invoke(time);
        }

        private void Draw()
        {
            if (isPaused) return;
            _dx11DeviceContext.ClearRenderTargetView(_renderView, new SharpDX.Color(0, 0, 0));
            _dx11DeviceContext.ClearDepthStencilView(_depthView, DX11.DepthStencilClearFlags.Depth | DX11.DepthStencilClearFlags.Stencil, 1.0f, 0);            
            OnDraw?.Invoke(1);          
            _swapChain.Present(0, PresentFlags.None);
        }

        public void Run()
        {
            SharpDX.Windows.RenderLoop.Run(_renderForm, RenderCallback);
        }

        double nextFrameTime = System.Environment.TickCount;

        private void RenderCallback()
        {
            double lag = System.Environment.TickCount - nextFrameTime;
            if (lag > 30)
            {
                nextFrameTime = System.Environment.TickCount;
                Update(lag);
            }
            Draw();
        }

    }
}

//int loops;
//private void RenderCallback()
//{
//    _timer.Tick();
//    loops = 0;
//    while (Environment.TickCount > nextFrameTime && loops < MaxFrameSkip)
//    {
//        Update(_timer.DeltaTime);
//        nextFrameTime += FrameDuration;
//        loops++;
//    }

//    Draw(_timer.DeltaTime);
//}