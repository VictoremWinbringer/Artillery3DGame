using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using Windows.ApplicationModel.Core;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using D3D = SharpDX.Direct3D;
using D3D11 = SharpDX.Direct3D11;
using DXGI = SharpDX.DXGI;

namespace UWP_ScPanel
{
    public sealed partial class MainPage : Page
    {
        private D3D11.Device2 device;
        private D3D11.DeviceContext2 deviceContext;
        private DXGI.SwapChain2 swapChain;
        private D3D11.Texture2D backBufferTexture;
        private D3D11.RenderTargetView backBufferView;
        public MainPage()
        {
            this.InitializeComponent();
            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown; 
        }
        private void CoreWindow_KeyDown(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
        {
            if (args.VirtualKey == Windows.System.VirtualKey.Escape)
            {
                var view = ApplicationView.GetForCurrentView();
                if (view.IsFullScreenMode)
                {
                    view.ExitFullScreenMode();
                }
                //else
                //{
                //    view.TryEnterFullScreenMode();
                //}
                //if (args.VirtualKey == Windows.System.VirtualKey.Escape)
                //{
                //   // CoreApplication.Exit();
                //    // Application.Current.Exit();                   
                //}
            }
        }

        private void SwapChainPanel_Loaded(object sender, RoutedEventArgs e)
        {
            var v = ApplicationView.GetForCurrentView();
            v.FullScreenSystemOverlayMode = FullScreenSystemOverlayMode.Minimal;
            v.TryEnterFullScreenMode();
#if DEBUG
            var debugLevel = D3D11.DeviceCreationFlags.Debug | D3D11.DeviceCreationFlags.BgraSupport;
#else
                        var debugLevel = D3D11.DeviceCreationFlags.None |D3D11.DeviceCreationFlags.BgraSupport;
#endif
            using (D3D11.Device defaultDevice = new D3D11.Device(D3D.DriverType.Hardware, debugLevel))
            {
                this.device = defaultDevice.QueryInterface<D3D11.Device2>();
            }
            this.deviceContext = this.device.ImmediateContext2;
            float pixelScale = Windows.Graphics.Display.DisplayInformation.GetForCurrentView().LogicalDpi / 96.0f;

            DXGI.SwapChainDescription1 swapChainDescription = new DXGI.SwapChainDescription1()
            {
                AlphaMode = DXGI.AlphaMode.Premultiplied,
                BufferCount = 2,
                Format = DXGI.Format.B8G8R8A8_UNorm,
                Height = (int)(this.SwapChainPanel.RenderSize.Height * pixelScale),
                Width = (int)(this.SwapChainPanel.RenderSize.Width * pixelScale),
                SampleDescription = new DXGI.SampleDescription(1, 0),
                Scaling = DXGI.Scaling.Stretch,
                Stereo = false,
                SwapEffect = DXGI.SwapEffect.FlipSequential,
                Usage = DXGI.Usage.BackBuffer | DXGI.Usage.RenderTargetOutput,
            };
            using (DXGI.Device3 dxgiDevice3 = this.device.QueryInterface<DXGI.Device3>())
            {
                using (DXGI.Factory3 dxgiFactory3 = dxgiDevice3.Adapter.GetParent<DXGI.Factory3>())
                {
                    using (DXGI.SwapChain1 swapChain1 = new DXGI.SwapChain1(dxgiFactory3, this.device, ref swapChainDescription))
                    {
                        this.swapChain = swapChain1.QueryInterface<DXGI.SwapChain2>();
                    }
                }
            }
            using (DXGI.ISwapChainPanelNative nativeObject = ComObject.As<DXGI.ISwapChainPanelNative>(this.SwapChainPanel))
            {
                nativeObject.SwapChain = this.swapChain;
            }
            this.backBufferTexture = D3D11.Texture2D.FromSwapChain<D3D11.Texture2D>(this.swapChain, 0);
            this.backBufferView = new D3D11.RenderTargetView(this.device, this.backBufferTexture);
            isDXInitialized = true;
            tw = new TextWirter(device, swapChain,Color.Black, DisplayInformation.GetForCurrentView().LogicalDpi);
//            #region D2D

//#if DEBUG
//            var debug = SharpDX.Direct2D1.DebugLevel.Error;
//#else
//            var debug = SharpDX.Direct2D1.DebugLevel.None;
//#endif

//            d2dFactory = new SharpDX.Direct2D1.Factory1(SharpDX.Direct2D1.FactoryType.SingleThreaded, debug);
//            using (var dxgiDevice = device.QueryInterface<SharpDX.DXGI.Device>())
//            {
//                d2dDevice = new SharpDX.Direct2D1.Device(d2dFactory, dxgiDevice);
//            }
//            d2dContext = new SharpDX.Direct2D1.DeviceContext(d2dDevice, SharpDX.Direct2D1.DeviceContextOptions.None);

//            BitmapProperties1 properties = new BitmapProperties1(
//                new PixelFormat(
//                    SharpDX.DXGI.Format.B8G8R8A8_UNorm,
//                    SharpDX.Direct2D1.AlphaMode.Premultiplied),
//                DisplayInformation.GetForCurrentView().LogicalDpi,
//                DisplayInformation.GetForCurrentView().LogicalDpi,
//                BitmapOptions.Target | BitmapOptions.CannotDraw);
//            DXGI.Surface backBuffer = swapChain.GetBackBuffer<DXGI.Surface>(0);
//            d2dTarget = new Bitmap1(d2dContext, backBuffer, properties);
//            SharpDX.DirectWrite.Factory fontFactory = new SharpDX.DirectWrite.Factory();
//            textFormat = new TextFormat(fontFactory, "Segoe UI", 24.0f);
//            textLayout1 = new TextLayout(fontFactory, "This is an example of a moving TextLayout object with snapped pixel boundaries.", textFormat, 400.0f, 200.0f);
//            textLayout2 = new TextLayout(fontFactory, "This is an example of a moving TextLayout object with no snapped pixel boundaries.", textFormat, 400.0f, 200.0f);
//            layoutY = 0.0f;
//            backgroundBrush = new SharpDX.Direct2D1.SolidColorBrush(d2dContext, Color.White);
//            textBrush = new SharpDX.Direct2D1.SolidColorBrush(d2dContext, Color.Black);
//            #endregion

            CompositionTarget.Rendering += CompositionTarget_Rendering;
            Application.Current.Suspending += Application_Suspending;
        }
        private void CompositionTarget_Rendering(object sender, object e)
        {
            this.deviceContext.OutputMerger.SetRenderTargets(this.backBufferView);
            this.deviceContext.ClearRenderTargetView(this.backBufferView, Color.CornflowerBlue);
            tw.DrawText("ПОГНАЛИ");
            //#region D2D rendering
            //d2dContext.Target = d2dTarget;
            //d2dContext.BeginDraw();
            //d2dContext.Clear(Color.CornflowerBlue);
            //d2dContext.FillRectangle(new RectangleF(50, 50, 200, 200), backgroundBrush);
            //d2dContext.DrawText("This text is long enough to overflow the designed region but will be clipped to the containing rectangle. Lorem ipsum dolor sit amet, consectetur adipiscing elit. ", textFormat, new RectangleF(50, 50, 200, 200), textBrush, DrawTextOptions.Clip);
            //d2dContext.FillRectangle(new RectangleF(50, 300, 200, 200), backgroundBrush);
            //d2dContext.DrawText("However, this other text isn't going to be clipped: Lorem ipsum dolor sit amet, consectetur adipiscing elit. Aenean gravida dui id accumsan dictum.", textFormat, new RectangleF(50, 300, 200, 200), textBrush, DrawTextOptions.None);
            //d2dContext.FillRectangle(new RectangleF(300, 50, 400, 200), backgroundBrush);
            //d2dContext.DrawText("MeasuringMode: Natural", textFormat, new RectangleF(300, 50, 400, 200), textBrush, DrawTextOptions.None, MeasuringMode.Natural);
            //d2dContext.DrawText("MeasuringMode: GDI classic", textFormat, new RectangleF(300, 80, 400, 200), textBrush, DrawTextOptions.None, MeasuringMode.GdiClassic);
            //d2dContext.DrawText("MeasuringMode: GDI natural", textFormat, new RectangleF(300, 110, 400, 200), textBrush, DrawTextOptions.None, MeasuringMode.GdiNatural);
            //float layoutYOffset = (float)System.Math.Cos(layoutY) * 50.0f;
            //d2dContext.FillRectangle(new RectangleF(300, 300, 400, 200), backgroundBrush);
            //d2dContext.DrawTextLayout(new Vector2(300, 350 + layoutYOffset), textLayout1, textBrush);
            //d2dContext.FillRectangle(new RectangleF(750, 300, 400, 200), backgroundBrush);
            //d2dContext.DrawTextLayout(new Vector2(750, 350 + layoutYOffset), textLayout2, textBrush, DrawTextOptions.NoSnap);
            //d2dContext.EndDraw();
            //layoutY += 1.0f / 60.0f;
            //#endregion

            this.swapChain.Present(1, DXGI.PresentFlags.None, new DXGI.PresentParameters());
        }

        private void SwapChainPanel_Unloaded(object sender, RoutedEventArgs e)
        {
            App.Current.Suspending -= Application_Suspending;
            CompositionTarget.Rendering -= CompositionTarget_Rendering;
            using (DXGI.ISwapChainPanelNative nativeObject = ComObject.As<DXGI.ISwapChainPanelNative>(this.SwapChainPanel))
            {
                nativeObject.SwapChain = null;
            }
            Utilities.Dispose(ref this.backBufferView);
            Utilities.Dispose(ref this.backBufferTexture);
            Utilities.Dispose(ref this.swapChain);
            Utilities.Dispose(ref this.deviceContext);
            Utilities.Dispose(ref this.device);
            Utilities.Dispose(ref this.textBrush);
            Utilities.Dispose(ref this.backgroundBrush);
            Utilities.Dispose(ref this.textLayout2);
            Utilities.Dispose(ref this.textLayout1);
            Utilities.Dispose(ref this.textFormat);
            Utilities.Dispose(ref this.d2dContext);
            Utilities.Dispose(ref this.d2dTarget);
            Utilities.Dispose(ref this.d2dFactory);
            Utilities.Dispose(ref this.d2dDevice);
            tw?.Dispose();
        }

        bool isDXInitialized;
        #region D2D fields
        private DeviceContext d2dContext;
        private Bitmap1 d2dTarget;
        private TextFormat textFormat;
        private TextLayout textLayout1;
        private TextLayout textLayout2;
        private SharpDX.Direct2D1.SolidColorBrush backgroundBrush;
        private SharpDX.Direct2D1.SolidColorBrush textBrush;
        private SharpDX.Direct2D1.Factory1 d2dFactory;
        private Device d2dDevice;
        TextWirter tw;
        #endregion
        private void SwapChainPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (isDXInitialized)
            { 
                Size2 newSize = RenderSizeToPixelSize(e.NewSize);                
                if (newSize.Width > swapChain.Description1.Width || newSize.Height > swapChain.Description1.Height)
                {
                    Utilities.Dispose(ref this.backBufferView);
                    Utilities.Dispose(ref this.backBufferTexture);
                    swapChain.ResizeBuffers(swapChain.Description.BufferCount,
                        (int)e.NewSize.Width, 
                        (int)e.NewSize.Height, 
                        swapChain.Description1.Format,
                        swapChain.Description1.Flags);
                    this.backBufferTexture = D3D11.Texture2D.FromSwapChain<D3D11.Texture2D>(this.swapChain, 0);
                    this.backBufferView = new D3D11.RenderTargetView(this.device, this.backBufferTexture);
                }
                swapChain.SourceSize = newSize;
                tw = new TextWirter(device, swapChain, Color.Red, DisplayInformation.GetForCurrentView().LogicalDpi);
                tw.SetTextSize(36);
                #region D2D изменение размера окна               
                //   BitmapProperties1 properties = new BitmapProperties1(
                //new PixelFormat(
                //    SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                //    SharpDX.Direct2D1.AlphaMode.Premultiplied),
                //DisplayInformation.GetForCurrentView().LogicalDpi,
                //DisplayInformation.GetForCurrentView().LogicalDpi,
                //BitmapOptions.Target | BitmapOptions.CannotDraw);
                //   DXGI.Surface backBuffer = swapChain.GetBackBuffer<DXGI.Surface>(0);
                //   Utilities.Dispose(ref d2dTarget);
                //   d2dTarget = new Bitmap1(d2dContext, backBuffer, properties);
                //   SharpDX.DirectWrite.Factory fontFactory = new SharpDX.DirectWrite.Factory();
                //   Utilities.Dispose(ref textLayout1);
                //   Utilities.Dispose(ref textLayout2);
                //   Utilities.Dispose(ref textFormat);
                //   textFormat = new TextFormat(fontFactory, "Segoe UI", 24.0f);
                //   textLayout1 = new TextLayout(fontFactory, "This is an example of a moving TextLayout object with snapped pixel boundaries.", textFormat, 400.0f, 200.0f);
                //   textLayout2 = new TextLayout(fontFactory, "This is an example of a moving TextLayout object with no snapped pixel boundaries.", textFormat, 400.0f, 200.0f);
                // d2dContext.DotsPerInch = new Size2F(DisplayInformation.GetForCurrentView().LogicalDpi, DisplayInformation.GetForCurrentView().LogicalDpi);
                #endregion

            }
        }
        private void Application_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            if (isDXInitialized)
            {
                this.deviceContext.ClearState();
                using (DXGI.Device3 dxgiDevice3 = this.swapChain.GetDevice<DXGI.Device3>())
                {
                    dxgiDevice3.Trim();
                }
            }
        }
        private Size2 RenderSizeToPixelSize(Windows.Foundation.Size renderSize)
        {
            float pixelScale = Windows.Graphics.Display.DisplayInformation.GetForCurrentView().LogicalDpi / 96.0f;
            return new Size2((int)(renderSize.Width * pixelScale), (int)(renderSize.Height * pixelScale));
        }
    }
}
