using SharpDX.Direct2D1;
using SharpDX.DXGI;
using SharpDX;
using TextAntialiasMode = SharpDX.Direct2D1.TextAntialiasMode;
using AlphaMode = SharpDX.Direct2D1.AlphaMode;
using Factory = SharpDX.Direct2D1.Factory;
using SharpDX.DirectWrite;
using System.Diagnostics;

namespace SharpDX11GameByWinbringer.Models
{

    /// <summary>
    /// Рисует текст и 2Д объекты на экран.
    /// </summary>
    public sealed class TextWirter : System.IDisposable
    {
        private Factory _Factory2D;
        private SharpDX.DirectWrite.Factory _FactoryDWrite;
        private RenderTarget _RenderTarget2D;
        private SolidColorBrush _SceneColorBrush;
        private TextFormat _TextFormat;
        private TextLayout _TextLayout;
        private Stopwatch _sw;
        int _width;
        int _heght;    
          
        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="BackBuffer">Буффер на который будем рисовать, наш холст</param>
        /// <param name="Width">Ширина области в которую будем рисовать</param>
        /// <param name="Height">Высота объласти в которую будем рисовать</param>
        public TextWirter(SharpDX.Direct3D11.Texture2D BackBuffer, int Width, int Height)
        {
            _width = Width;
            _heght = Height;
            _sw = new Stopwatch();
            _sw.Start();
            _Factory2D = new SharpDX.Direct2D1.Factory();
            using (var surface = BackBuffer.QueryInterface<Surface>())
            {
                _RenderTarget2D = new RenderTarget(_Factory2D, surface,
                                                  new RenderTargetProperties(
                                                      new PixelFormat(
                                                      Format.R8G8B8A8_UNorm,
                                                      AlphaMode.Premultiplied)));
            }
            _RenderTarget2D.AntialiasMode = AntialiasMode.PerPrimitive;
            _FactoryDWrite = new SharpDX.DirectWrite.Factory();
            _SceneColorBrush = new SolidColorBrush(_RenderTarget2D, Color.White);
            // Initialize a TextFormat
            _TextFormat = new TextFormat(_FactoryDWrite, "Calibri", 14) {
                TextAlignment = TextAlignment.Leading,
                ParagraphAlignment = ParagraphAlignment.Near };
            _RenderTarget2D.TextAntialiasMode = TextAntialiasMode.Cleartype;    
            // Initialize a TextLayout
            _TextLayout = new TextLayout(_FactoryDWrite, "SharpDX D2D1 - DWrite", _TextFormat,Width, Height);
        }

        public void DrawText(string text, float x0=0, float y0=0, float x1=200, float y1=200)
        {
            _sw.Stop();
            string s = string.Format("FPS : {0:#####}", 1000.0f / _sw.Elapsed.TotalMilliseconds);
            _sw.Reset();
            _sw.Start();
            s = s + "  " + text;
            _RenderTarget2D.BeginDraw();
            _RenderTarget2D.DrawText(
                s, s.Length,
                _TextFormat,
                new RectangleF(x0,y0, x1, y1), 
                _SceneColorBrush,
                DrawTextOptions.None,
                MeasuringMode.GdiClassic);
          //  _RenderTarget2D.DrawTextLayout(new Vector2(300, 300), _TextLayout, _SceneColorBrush, DrawTextOptions.None);
            _RenderTarget2D.EndDraw();
        }
        public void Text(string s,float x0 = 0, float y0 = 0, float x1 = 200, float y1 = 200)
        {
            _RenderTarget2D.BeginDraw();
            _RenderTarget2D.DrawText(
                s, s.Length,
                _TextFormat,
                new RectangleF(x0, y0, x1, y1),
                _SceneColorBrush,
                DrawTextOptions.None,
                MeasuringMode.GdiClassic);
            _RenderTarget2D.EndDraw();
        }
        public void DrawLine(float x0, float y0, float x1, float y1)
        {
            _RenderTarget2D.BeginDraw();
            _RenderTarget2D.DrawLine(new SharpDX.Mathematics.Interop.RawVector2(x0, y0), new SharpDX.Mathematics.Interop.RawVector2(x1, y1), _SceneColorBrush);
            _RenderTarget2D.EndDraw();
        }
        public void Dispose()
        {
            Utilities.Dispose(ref _Factory2D);
            Utilities.Dispose(ref _FactoryDWrite);
            Utilities.Dispose(ref _RenderTarget2D);
            Utilities.Dispose(ref _SceneColorBrush);
            Utilities.Dispose(ref _TextFormat);
            Utilities.Dispose(ref _TextLayout);
            _TextLayout?.Dispose();
        }

    }
}
