using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX11GameByWinbringer.ViewModels;

namespace SharpDX11GameByWinbringer.Models
{
    public sealed class _3DCubeMeneger : Meneger3D
    {     
        TexturedCube _cube;

        public _3DCubeMeneger(DeviceContext DeviceContext)
        {
            InputElement[] inputElements = new InputElement[]
              {
                new InputElement("POSITION", 0, SharpDX.DXGI.Format.R32G32B32_Float,0, 0),
                new InputElement("TEXCOORD",0,SharpDX.DXGI.Format.R32G32_Float,12,0)
              };

            _drawer = new Drawer("Shaders\\CubeShader.hlsl",
                                      inputElements,
                                      DeviceContext);
            _cube = new TexturedCube(DeviceContext.Device);
        }
        
        public override void Update(double time)
        {
            
        }

        public override void Draw(Matrix _World, Matrix _View, Matrix _Progection)
        {
            _cube.Update(_World, _View, _Progection);
            _cube.FillViewModel(_viewModel);
            _drawer.Draw(_viewModel, PrimitiveTopology.TriangleList, false);
        }


        public override void Dispose()
        {
            base.Dispose();
            Utilities.Dispose(ref _cube);

        }
    }
}
