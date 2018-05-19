using SharpDX.Windows;
using System.Windows.Forms;
using System.Drawing;

namespace SharpDX11GameByWinbringer
{

    sealed class Program
    {
        [System.STAThread]
        static void Main(string[] args)
        {
            System.Console.WriteLine("Victorem for by_Owl 17.07.2016");
            System.Console.WriteLine("Загрузка ресурсов ...");
            double start = System.Environment.TickCount;
            if (!SharpDX.Direct3D11.Device.IsSupportedFeatureLevel(SharpDX.Direct3D.FeatureLevel.Level_11_0))
            {
                MessageBox.Show("Для запуска этой игры нужен DirectX 11 ОБЯЗАТЕЛЬНО!");
                return;
            }
#if DEBUG
            SharpDX.Configuration.EnableObjectTracking = true;
#endif
            using (var _renderForm = new RenderForm("SharpDX game by Winbringer")
            {
                AllowUserResizing = false,
                IsFullscreen = false,
                StartPosition = FormStartPosition.CenterScreen,
                ClientSize = new Size(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height),
                FormBorderStyle = FormBorderStyle.None,
                Icon = new Icon("LogoVW.ico")
            })
            {
                _renderForm.Shown += (sender, e) => { _renderForm.Activate(); };
                _renderForm.KeyDown += (sender, e) => { if (e.KeyCode == Keys.Escape) _renderForm.Close(); };
                using (Game game = new Game(_renderForm))
                {
                    System.Console.WriteLine("Для начала игры нажмите \"Enter\"");
                    System.Console.WriteLine("Для выхода из игры нажмите \"Esc\"");
                    System.Console.WriteLine("Для паузы нажмите латинскую \"P\"");
                    System.Console.WriteLine("\"U\" - Возврат снаряда ");
                    System.Console.WriteLine("\" W A S D \" - Движение камеры ");
                    System.Console.WriteLine("\"<- | -> \" - Стрелки на клавиатуре, поворот камеры ");
                    System.Console.WriteLine("\"Z X\" - Изменение высоты камеры ");
                    System.Console.WriteLine("\" 1 2 3 4 5 6\" - Цифры на клавиатуре, управление направлением, углом стрельбы и скоростью полета планеты");
                    System.Console.WriteLine("\"Пробел\" - Клавиша пробела запускает снаряд в цель");
                    System.Console.ReadLine();

                    game.Run();
                }
            }

            double end = (System.Environment.TickCount - start) / 1000;
            System.Console.WriteLine("Всего проведено в игре секунд : " + end.ToString());
            System.Console.WriteLine("Для завершения нажмите ввод");
            System.Console.ReadLine();
        }
    }
}
