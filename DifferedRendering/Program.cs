
using VictoremLibrary;

namespace DifferedRendering
{
    class Program
    {
        [System.STAThread]
        static void Main(string[] args)
        {
            using (var f = StaticMetods.GetRenderForm("Differed Rendering"))
            using (var g = new AppMy(f))
            {
                g.Run();
            }
        }
    }
}
