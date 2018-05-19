using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX11GameByWinbringer.ViewModels;


namespace SharpDX11GameByWinbringer.Models
{
    sealed class _3DLineMaganer : Meneger3D
    {
        XYZ _cube;

        public _3DLineMaganer(DeviceContext DeviceContext)
        {
            InputElement[] inputElements1 = new InputElement[]
           {
                new InputElement("POSITION", 0, SharpDX.DXGI.Format.R32G32B32_Float,0, 0),
                new InputElement("COLOR",0,SharpDX.DXGI.Format.R32G32B32A32_Float,12,0)
           };

            _drawer = new Drawer("Shaders\\ColoredVertex.hlsl",
                                      inputElements1,
                                      DeviceContext);
            _cube = new XYZ(DeviceContext.Device);
        }

        public override void Dispose()
        {
            base.Dispose();
            Utilities.Dispose(ref _cube);
        }

        public override void Update(double time)
        {

        }

        public override void Draw(Matrix _World, Matrix _View, Matrix _Progection)
        {
            _cube.Update(_World, _View, _Progection);
            _cube.FillViewModel(_viewModel);
            _drawer.Draw(_viewModel, PrimitiveTopology.LineList, false);
        }
    }
}
