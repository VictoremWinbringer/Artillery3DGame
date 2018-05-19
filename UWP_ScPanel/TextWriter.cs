using SharpDX.Direct2D1;
using SharpDX.DXGI;
using SharpDX;
using TextAntialiasMode = SharpDX.Direct2D1.TextAntialiasMode;
using AlphaMode = SharpDX.Direct2D1.AlphaMode;
using Factory = SharpDX.Direct2D1.Factory;
using SharpDX.DirectWrite;
using System.Diagnostics;
using SharpDX.Direct3D11;

namespace UWP_ScPanel
{
    /// <summary>
    /// Рисует текст и 2Д объекты на экран.
    /// </summary>
    public sealed class TextWirter : System.IDisposable
    {
        private SharpDX.Direct2D1.Factory1 _Factory2D;
        private SharpDX.DirectWrite.Factory _FactoryDWrite;
        private SharpDX.Direct2D1.DeviceContext _RenderTarget2D;
        private SolidColorBrush _SceneColorBrush;
        private TextFormat _TextFormat;
        private TextLayout _TextLayout;
        string TextFont;
        int TextSize;
        private SharpDX.Direct2D1.Device d2dDevice;
        private Bitmap1 d2dTarget;

        /// <summary>
        /// Обязательно вызвать Бегинд драв перед и Енд драв после рисования 2д примитивов.
        /// </summary>
        public SharpDX.Direct2D1.DeviceContext RenderTarget { get { return _RenderTarget2D; } }

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="BackBuffer">Буффер на который будем рисовать, наш холст</param>
        /// <param name="Width">Ширина области в которую будем рисовать</param>
        /// <param name="Height">Высота объласти в которую будем рисовать</param>
        public TextWirter(SharpDX.Direct3D11.Device2 device, SwapChain2 swapChain,Color color, float dpi = 96f, string font = "Calibri",int size=14)
        {
#if DEBUG
            var debug = SharpDX.Direct2D1.DebugLevel.Error;
#else
            var debug = SharpDX.Direct2D1.DebugLevel.None;
#endif

            _Factory2D = new SharpDX.Direct2D1.Factory1(SharpDX.Direct2D1.FactoryType.SingleThreaded, debug);
            using (var dxgiDevice = device.QueryInterface<SharpDX.DXGI.Device>())
            {
                d2dDevice = new SharpDX.Direct2D1.Device(_Factory2D, dxgiDevice);
            }
            _RenderTarget2D = new SharpDX.Direct2D1.DeviceContext(d2dDevice, SharpDX.Direct2D1.DeviceContextOptions.None);

            BitmapProperties1 properties = new BitmapProperties1(
                new PixelFormat(
                    SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                    SharpDX.Direct2D1.AlphaMode.Premultiplied),
                dpi,
                dpi,
                BitmapOptions.Target | BitmapOptions.CannotDraw);
            Surface backBuffer = swapChain.GetBackBuffer<Surface>(0);
            d2dTarget = new Bitmap1(_RenderTarget2D, backBuffer, properties);
            this.TextFont =font ;
            this.TextSize = size;
            _FactoryDWrite = new SharpDX.DirectWrite.Factory();
            _SceneColorBrush = new SolidColorBrush(_RenderTarget2D,color);
            InitTextFormat();
            _RenderTarget2D.TextAntialiasMode = TextAntialiasMode.Cleartype;
        }
        public void SetDPI(float dpiX, float dpiY)
        {
            _RenderTarget2D.DotsPerInch =new Size2F( dpiX, dpiY);
        }
        /// <summary>
        /// Устанавливает шрифт для текста.
        /// </summary>
        /// <param name="font">Имя шрифта установленного в системе</param>
        public void SetTextFont(string font)
        {
            this.TextFont = font;
            InitTextFormat();
        }

        /// <summary>
        /// Пересоздает класс в котором храняться данные о формате выводимого теста.
        /// </summary>
        private void InitTextFormat()
        {
            _TextFormat?.Dispose();
            _TextFormat = new TextFormat(_FactoryDWrite, TextFont, TextSize)
            {
                TextAlignment = TextAlignment.Leading,
                ParagraphAlignment = ParagraphAlignment.Near
            };
        }

        /// <summary>
        /// Задает цвет выводимого на экран текста.
        /// </summary>
        /// <param name="color">Цвет текста</param>
        public void SetTextColor(Color color)
        {
            _SceneColorBrush.Dispose();
            _SceneColorBrush = new SolidColorBrush(_RenderTarget2D, color);
        }

        /// <summary>
        /// Устанавливает развер шрифта для выводимого текста.
        /// </summary>
        /// <param name="size">Размер шрифта</param>
        public void SetTextSize(int size)
        {
            TextSize = size;
            InitTextFormat();
        }

        /// <summary>
        /// Выводит текст на экран. Должен вызываться последним после всех остальных операций по Рендерингу. Перед метордом Презент Свапчейна.
        /// </summary>
        /// <param name="text">Текст который будет рисоваться.</param>
        /// <param name="x">Отступ текста от левого края экрана, в пикселях.</param>
        /// <param name="y">Отступ текста от верхнего края экрана, в пикселях.</param>
        /// <param name="width">Ширина области в которую будет выводиться текст</param>
        /// <param name="height">Высота области в которую будет выводиться текст</param>
        public void DrawText(string text, float x = 0, float y = 0, float width = 400, float height = 300)
        {
            _RenderTarget2D.Target = d2dTarget;
            _RenderTarget2D.BeginDraw();
            _RenderTarget2D.DrawText(
                text,
                _TextFormat,
                new RectangleF(x, y, width, height),
                _SceneColorBrush,
                DrawTextOptions.Clip);
            _RenderTarget2D.EndDraw();
        }

        /// <summary>
        /// Рисует Битмап (карту битов) на экран.
        /// </summary>
        /// <param name="bitmap">Карта битов которую нужно нарисовать.</param>
        /// <param name="x">Отступ от левого края</param>
        /// <param name="y">Отступ от верхнего края</param>
        /// <param name="scale">Масштаб (0.5 - картинка будет в пол размера, 2 - в два раза больше)</param>
        /// <param name="opacity">Прозрачность картинки. 1 - непрозрачная. 0.5 - полупрозрачная. 0 - невидимая</param>
        /// <param name="interMode">Как будет находиться цвет пикселя при растяжении или сжатии картинки</param>
        public void DrawBitmap(Bitmap bitmap, float x = 0, float y = 0, float scale = 1, float opacity = 1, BitmapInterpolationMode interMode = BitmapInterpolationMode.Linear)
        {
            _RenderTarget2D.BeginDraw();
            _RenderTarget2D.DrawBitmap(bitmap, new SharpDX.Mathematics.Interop.RawRectangleF(x, y, x + bitmap.Size.Width * scale, y + bitmap.Size.Height * scale), opacity, interMode);
            _RenderTarget2D.EndDraw();
        }

        public void Dispose()
        {
            Utilities.Dispose(ref _Factory2D);
            Utilities.Dispose(ref _FactoryDWrite);
            Utilities.Dispose(ref _SceneColorBrush);
            Utilities.Dispose(ref _TextFormat);
            Utilities.Dispose(ref _TextLayout);
            Utilities.Dispose(ref d2dTarget);
            Utilities.Dispose(ref _RenderTarget2D);
            Utilities.Dispose(ref d2dDevice);
            _TextLayout?.Dispose();
        }
    }
}
