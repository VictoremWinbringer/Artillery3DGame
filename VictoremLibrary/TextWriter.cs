using SharpDX.Direct2D1;
using SharpDX.DXGI;
using SharpDX;
using TextAntialiasMode = SharpDX.Direct2D1.TextAntialiasMode;
using AlphaMode = SharpDX.Direct2D1.AlphaMode;
using Factory = SharpDX.Direct2D1.Factory;
using SharpDX.DirectWrite;
using System.Diagnostics;

namespace VictoremLibrary
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
        int _width;
        int _heght;
        string TextFont;
        int TextSize;

        /// <summary>
        /// Обязательно вызвать Бегинд драв перед и Енд драв после рисования 2д примитивов.
        /// </summary>
        public RenderTarget RenderTarget { get { return _RenderTarget2D; } }

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="BackBuffer">Буффер на который будем рисовать, наш холст</param>
        /// <param name="Width">Ширина области в которую будем рисовать</param>
        /// <param name="Height">Высота объласти в которую будем рисовать</param>
        public TextWirter(SharpDX.Direct3D11.Texture2D BackBuffer, int Width, int Height)
        {
            this.TextFont = "Calibri";
            this.TextSize = 14;
            _width = Width;
            _heght = Height;
            _Factory2D = new SharpDX.Direct2D1.Factory();
            using (var surface = BackBuffer.QueryInterface<Surface>())
            {
                _RenderTarget2D = new RenderTarget(
                    _Factory2D,
                    surface,
                    new RenderTargetProperties(
                        new PixelFormat(
                            Format.R8G8B8A8_UNorm,
                            AlphaMode.Premultiplied)));
            }
            _RenderTarget2D.AntialiasMode = AntialiasMode.PerPrimitive;
            _FactoryDWrite = new SharpDX.DirectWrite.Factory();
            _SceneColorBrush = new SolidColorBrush(_RenderTarget2D, Color.White);
            // Initialize a TextFormat
            InitTextFormat();
            _RenderTarget2D.TextAntialiasMode = TextAntialiasMode.Cleartype;
            // Initialize a TextLayout
            // _TextLayout = new TextLayout(_FactoryDWrite, "SharpDX D2D1 - DWrite", _TextFormat, Width, Height);
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
            _RenderTarget2D.BeginDraw();
            _RenderTarget2D.DrawText(
                text, text.Length,
                _TextFormat,
                new RectangleF(x, y, width, height),
                _SceneColorBrush,
                DrawTextOptions.None,
                MeasuringMode.GdiClassic);
            //  RenderTarget2D.DrawTextLayout(new Vector2(0, 0), TextLayout, SceneColorBrush, DrawTextOptions.None);
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
            Utilities.Dispose(ref _SceneColorBrush);
            Utilities.Dispose(ref _TextLayout);
            Utilities.Dispose(ref _TextFormat);
            Utilities.Dispose(ref _RenderTarget2D);
        }
    }
}
