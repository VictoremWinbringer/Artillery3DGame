using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX11GameByWinbringer.Models;
using SharpDX;
using SharpDX.Direct3D11;

namespace SharpDX11GameByWinbringer.Models
{
    sealed class TexturedCube : GameObject<Vertex, Data>
    {
        float size = 10;
        public TexturedCube(Device device)
        {
            _world = Matrix.Identity;
            CreateVerteces();
            CreateBuffers(device,"Textures\\lava.jpg");
        }
        public override void Update(Matrix world, Matrix view, Matrix proj)
        {
            _constantBufferData.WVP = _world * world * view * proj;
            _constantBufferData.WVP.Transpose();
        }

        protected override void CreateVerteces()
        {
            _indeces = new uint[]
                {
                0,1,2, // передняя сторона
                2,3,0,

                6,5,4, // задняя сторона
                4,7,6,

                4,0,3, // левый бок
                3,7,4,

                1,5,6, // правый бок
                6,2,1,

                4,5,1, // вверх
                1,0,4,

                3,2,6, // низ
                6,7,3,
         };

            _verteces = new Vertex[8];
            _verteces[0] = new Vertex(new Vector3(-size, size, size), new Vector2(0, 0));
            _verteces[1] = new Vertex(new Vector3(size, size, size), new Vector2(1, 0));
            _verteces[2] = new Vertex(new Vector3(size, -size, size), new Vector2(1, 1));
            _verteces[3] = new Vertex(new Vector3(-size, -size, size), new Vector2(0, 1));
            _verteces[4] = new Vertex(new Vector3(-size, size, -size), new Vector2(0, 0));
            _verteces[5] = new Vertex(new Vector3(size, size, -size), new Vector2(1, 0));
            _verteces[6] = new Vertex(new Vector3(size, -size, -size), new Vector2(1, 1));
            _verteces[7] = new Vertex(new Vector3(-size, -size, -size), new Vector2(0, 1));
        }
    }
}
