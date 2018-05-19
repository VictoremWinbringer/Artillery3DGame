using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VictoremLibrary;

namespace FramevorkTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Загрузка ресурсов ...");
            using (var window = StaticMetods.GetRenderForm("FrameworkTesting", "LogoVW.ico"))
            using (var game = new Game(window))
            using (var presenter = new Presenter(game))
            {
                Console.WriteLine("Для начала игры нажмите \"Enter\"");
                Console.ReadLine();
                game.Run();
            }
        }
    }
}
