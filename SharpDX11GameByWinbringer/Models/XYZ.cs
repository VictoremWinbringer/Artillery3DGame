using SharpDX;
using SharpDX.Direct3D11;

namespace SharpDX11GameByWinbringer.Models
{
    sealed class XYZ : GameObject<ColoredVertex, Data>
    {
        public XYZ(Device device)
        {
            CreateVerteces();
            CreateBuffers(device, "Textures\\lava.jpg");
        }

        public override void Update(Matrix World, Matrix View, Matrix Proj)
        {
            _constantBufferData.WVP = Matrix.Identity * World * View * Proj;
            _constantBufferData.WVP.Transpose();
        }

        protected override void CreateVerteces()
        {
            _verteces = new ColoredVertex[]
           {
                new ColoredVertex(new Vector3(0,0,0) ,new Vector4(1,1,1,1)),
                new ColoredVertex(new Vector3(400, 0, 0), new Vector4(1, 0, 0, 1)),
                new ColoredVertex(new Vector3(0, 400, 0), new Vector4(0, 1, 0, 1)),
                new ColoredVertex(new Vector3(0, 0, 400), new Vector4(0, 0, 1, 1))
           };
            _indeces = new uint[]
                {
                    0,1,
                    0,2,
                    0,3
                };
        }
    }
}
