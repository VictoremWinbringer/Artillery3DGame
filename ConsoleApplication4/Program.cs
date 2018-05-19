using System;
using VictoremLibrary;
using SharpDX.Windows;
using System.Windows.Forms;
namespace ConsoleApplication4
{
    class Program
    {
        static void Main(string[] args)
        {

            using (var form = StaticMetods.GetRenderForm("By Victorem"))
            using (var game = new Class1(form))
            {
                game.Run();
            }


        }
    }
}
