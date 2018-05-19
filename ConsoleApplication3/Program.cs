using VictoremLibrary;

namespace ConsoleApplication3
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var form = StaticMetods.GetRenderForm("By Victorem", "LogoVW.ico"))
            using (var game = new Game(form))
            using (var logic = new LogicMy(game))
            {
                game.Run();
            }
        }
    }
}
