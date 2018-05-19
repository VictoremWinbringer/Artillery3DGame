using System;
using SharpDX;
using SharpDX.Direct3D11;

namespace SharpDX11GameByWinbringer.Models
{
    sealed class Wave : GameObject<Vertex, DataT>
    {
        private readonly float _size = 500f;
        readonly int _N = 500;
        public string Texture { set { _texture = _device.ImmediateContext.LoadTextureFromFile(value); } }
        public Wave(Device device, string textureFile)
        {
            _world = Matrix.Translation(new Vector3(-500,-4,-500));
            CreateVerteces();
            _constantBufferData = new DataT();
            CreateBuffers(device, textureFile);
        }


        public override void Update(Matrix world, Matrix view, Matrix proj)
        {
            _constantBufferData.WVP =_world * world * view * proj;//Matrix.Translation(Matrix.Invert(view).TranslationVector)* 
            _constantBufferData.WVP.Transpose();
            _constantBufferData.Time = System.Environment.TickCount;
        }

        protected override void CreateVerteces()
        {
            _verteces = new Vertex[_N * _N];
            //Создание верщин           
            float delta = _size / (_N - 1);

            for (int i = 0; i < _N; ++i)
            {
                for (int j = 0; j < _N; ++j)
                {
                    int index = (i * _N) + j;

                    _verteces[index].Position = new Vector3(delta * j, 0, delta * i);
                    _verteces[index].TextureUV = new Vector2(j, i) / 500;

                }
            }

            //Создание индексов
            _indeces = new uint[(_N - 1) * (_N - 1) * 6];
            uint counter = 0;

            for (int z = 0; z < (_N - 1); ++z)
            {
                for (int x = 0; x < (_N - 1); ++x)
                {
                    uint lowerLeft = (uint)(z * _N + x);
                    uint lowerRight = lowerLeft + 1;
                    uint upperLeft = lowerLeft + (uint)_N;
                    uint upperRight = upperLeft + 1;

                    _indeces[counter++] = lowerLeft;
                    _indeces[counter++] = upperLeft;
                    _indeces[counter++] = upperRight;

                    _indeces[counter++] = lowerLeft;
                    _indeces[counter++] = upperRight;
                    _indeces[counter++] = lowerRight;
                }
            }
        }
    }
}
