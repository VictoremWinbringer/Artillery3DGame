using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VictoremLibrary;
namespace CubeReflection
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var f = StaticMetods.GetRenderForm("Cube Map"))
            using (var g = new App(f))
            {
                g.Run();
            }
        }
    }
}
