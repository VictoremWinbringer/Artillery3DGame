
using VictoremLibrary;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var form = StaticMetods.GetRenderForm("Victorem_P", "LogoVW.ico"))
            using (var game = new Game(form))
            using (var presenter = new Presenter(game))
            {                
                game.Run();
            }
        }
    }
}
