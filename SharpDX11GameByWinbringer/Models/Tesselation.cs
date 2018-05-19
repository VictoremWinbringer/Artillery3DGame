
using System;
using SharpDX;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;
using SharpDX11GameByWinbringer.ViewModels;
using SharpDX.D3DCompiler;

namespace SharpDX11GameByWinbringer.Models
{
    struct TesConst
    {
        public Matrix W;
        public Matrix VP;
        public Vector4 TF;

        internal void Transpose()
        {
           W.Transpose();
            VP.Transpose();
        }
    }

    class Quad : Object3D11<Vertex>
    {
        public Quad(Device dv)
        {
            _indeces = new uint[]
           {
                0, 1, 2,
                1,3,2
           };
            _veteces = new[]
            {
                new Vertex(new Vector3(0,0,0),new Vector2(0,0)),
                new Vertex(new Vector3(-100,0,0),new Vector2(1,0)),
                new Vertex(new Vector3(0,100,0),new Vector2(0,1)),
                new Vertex(new Vector3(-100,100,0),new Vector2(1,1)),
            };
            InitBuffers(dv);
        }
    }

    class Tri : Object3D11<Vertex>
    {
        public Tri(Device dv)
        {
            _indeces = new uint[]
            {
                0, 1, 2
            };
            _veteces = new[]
            {
                new Vertex(new Vector3(0,0,0),new Vector2(0,0)),
                new Vertex(new Vector3(100,0,0),new Vector2(1,0)),
                new Vertex(new Vector3(0,100,0),new Vector2(0,1)),
            };
            InitBuffers(dv);
        }

    }

    class Tesselation : Meneger3D
    {

        Buffer _cb;
        Device _dv;
        Tri _tri;
        Quad _quad;
        private HullShader _HShader;
        private DomainShader _DShader;
        private GeometryShader _GShader;
        public int tFactor=32;
        public Tesselation(Device dv,int tFactor)
        {
            this.tFactor = tFactor;
            this.World = Matrix.Identity;
            _dv = dv;
            _tri = new Tri(dv);
            _quad = new Quad(dv);

            _cb = new Buffer(dv, Utilities.SizeOf<TesConst>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);

            var inputElements = new InputElement[]
                            {
                         new InputElement("POSITION",0,SharpDX.DXGI.Format.R32G32B32_Float,0,0)
                      // new InputElement("TEXCOORD", 0, SharpDX.DXGI.Format.R32G32_Float,12, 0)
                             };

            _drawer = new Drawer("Shaders\\Tes.hlsl", inputElements, dv.ImmediateContext);
            var m = _dv.ImmediateContext.LoadTextureFromFile("Textures\\grass.jpg");
            _viewModel.Textures = new[] { m };
            _viewModel.ConstantBuffers = new[] { _cb };

            ShaderFlags shaderFlags = ShaderFlags.None;
#if DEBUG
            shaderFlags = ShaderFlags.Debug;
#endif
            using (var pixelShaderByteCode = ShaderBytecode.CompileFromFile("Shaders\\Tes.hlsl", "HS", "hs_5_0", shaderFlags))
            {
                _HShader = new HullShader(_dv, pixelShaderByteCode);
            }
            using (var pixelShaderByteCode = ShaderBytecode.CompileFromFile("Shaders\\Tes.hlsl", "DS", "ds_5_0", shaderFlags))
            {
                 _DShader = new DomainShader(_dv, pixelShaderByteCode);
            }           
            using (var pixelShaderByteCode = ShaderBytecode.CompileFromFile("Shaders\\Tes.hlsl", "GS", "gs_5_0", shaderFlags))
            {
                _GShader = new GeometryShader(_dv, pixelShaderByteCode);
            }

            var r = RasterizerStateDescription.Default();
            r.CullMode = CullMode.None;
            r.FillMode = FillMode.Wireframe;
            _drawer.RasterizerDescription = r;
        }

        public override void Update(double time)
        {

        }

        public override void Draw(Matrix w, Matrix v, Matrix p)
        {
            TesConst mvp = new TesConst();
            mvp.W =Matrix.Identity;
            mvp.VP=v * p;
            mvp.TF = new Vector4(tFactor);
            mvp.Transpose();

            _dv.ImmediateContext.UpdateSubresource(ref mvp, _cb);

            _dv.ImmediateContext.GeometryShader.Set(_GShader);
            _dv.ImmediateContext.DomainShader.Set(_DShader);
            _dv.ImmediateContext.HullShader.Set(_HShader);
            _dv.ImmediateContext.GeometryShader.SetConstantBuffer(0, _cb);
            _dv.ImmediateContext.DomainShader.SetConstantBuffer(0, _cb);
            _dv.ImmediateContext.HullShader.SetConstantBuffer(0, _cb);

            _tri.FillVM(ref _viewModel);           
            _drawer.Draw(_viewModel,SharpDX.Direct3D.PrimitiveTopology.PatchListWith3ControlPoints);

            _quad.FillVM(ref _viewModel);
            _drawer.Draw(_viewModel, SharpDX.Direct3D.PrimitiveTopology.PatchListWith3ControlPoints);

            _dv.ImmediateContext.GeometryShader.Set(null);
            _dv.ImmediateContext.DomainShader.Set(null);
            _dv.ImmediateContext.HullShader.Set(null);
        }

        public override void Dispose()
        {
            base.Dispose();
            Utilities.Dispose(ref _cb);
            _tri.Dispose();
            _quad.Dispose();
        _HShader.Dispose();
        _DShader.Dispose();
        _GShader.Dispose();
    }
    }
}
