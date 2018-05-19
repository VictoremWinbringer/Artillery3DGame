using SharpDX.Direct3D11;
using System;
using SharpDX;
using System.Runtime.InteropServices;
using SharpDX.DXGI;

namespace VictoremLibrary
{


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct AnimConst
    {
        public Matrix WVP;
        public uint HasAnimaton;
        public uint HasDiffuseTexture;
        Vector2 padding0;
        public Matrix World;
        public Color4 Dif;
        public AnimConst(Matrix w, Matrix v, Matrix p, uint HasAnim, uint HasTex)
        {

            HasAnimaton = HasAnim;
            HasDiffuseTexture = HasTex;
            WVP = w * v * p;
            World = w;
            padding0 = new Vector2();
            Dif = Color4.White;
        }

        public void Transpose()
        {
            WVP.Transpose();
            World.Transpose();
        }
    }


    internal class BonesConst
    {
        public Matrix[] Bones;

        public BonesConst()
        {
            Bones = new Matrix[1024];

        }
        public void init(Matrix[] bones)
        {
            for (int i = 0; i < bones.Length; i++)
            {
                var m = bones[i];
                m.Transpose();
                Bones[i] = m;
            }
        }
        public static int Size()
        {
            return Utilities.SizeOf<Matrix>() * 1024;
        }

    }



    public class Assimp3DModel : IDisposable
    {

        #region Инпут элементы

        public static readonly InputElement[] SkinnedPosNormalTexTanBi = {
                new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, InputClassification.PerVertexData, 0),
                new InputElement("NORMAL", 0, Format.R32G32B32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData, 0),
                new InputElement("TEXCOORD", 0, Format.R32G32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData, 0),
                new InputElement("TANGENT", 0, Format.R32G32B32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData,0 ),
                 new InputElement("BINORMAL", 0, Format.R32G32B32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData,0 ),
                  new InputElement("BLENDINDICES", 0, Format.R32G32B32A32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData, 0),
                new InputElement("BLENDWEIGHT", 0, Format.R32G32B32A32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData, 0),
                 new InputElement("COLOR", 0, Format.R32G32B32A32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData, 0)
            };
        public static readonly InputElement[] PosNormalTexTanBi = {
                new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, InputClassification.PerVertexData, 0),
                new InputElement("NORMAL", 0, Format.R32G32B32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData, 0),
                new InputElement("TEXCOORD", 0, Format.R32G32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData, 0),
                new InputElement("TANGENT", 0, Format.R32G32B32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData,0 ),
                 new InputElement("BINORMAL", 0, Format.R32G32B32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData,0 ),

            };
        public static readonly InputElement[] PosNormalTex = {
                new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, InputClassification.PerVertexData, 0),
                new InputElement("NORMAL", 0, Format.R32G32B32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData, 0),
                new InputElement("TEXCOORD", 0, Format.R32G32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData, 0),
            };

        public static readonly InputElement[] PosNormal = {
                new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, InputClassification.PerVertexData, 0),
                new InputElement("NORMAL", 0, Format.R32G32B32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData, 0),
            };
        #endregion

        Shader _shader;
        ModelSDX _model;
        Game _game;
        SamplerState _samler;
        BonesConst _bones;
        public Matrix _world = Matrix.Identity;
        public Matrix _view = Matrix.LookAtLH(new Vector3(-50, 50, -50), Vector3.Zero, Vector3.Up);
        public Matrix _proj;
        AnimConst _constData = new AnimConst();
        private SharpDX.Direct3D11.Buffer _constBuffer1;
        private SharpDX.Direct3D11.Buffer _constBuffer0;

        public Assimp3DModel(Game game, string modelFile, string Folder)
        {
            _proj = Matrix.PerspectiveFovLH(MathUtil.PiOverFour, game.Form.Width / (float)game.Form.Height, 1f, 10000f);
            _bones = new BonesConst();
            _game = game;
            _model = new ModelSDX(game.DeviceContext.Device, Folder, modelFile);
            _shader = new Shader(game.DeviceContext, "Shaders\\Assimp.hlsl", SkinnedPosNormalTexTanBi);

            _samler = new SamplerState(game.DeviceContext.Device, new SamplerStateDescription()
            {
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                BorderColor = new Color4(0, 0, 0, 0),
                ComparisonFunction = Comparison.Never,
                Filter = Filter.Anisotropic,
                MaximumAnisotropy = 16,
                MaximumLod = float.MaxValue,
                MinimumLod = 0,
                MipLodBias = 0.0f
            });
            _constData.HasAnimaton = _model.HasAnimation ? 1u : 0;
            _constData.HasDiffuseTexture = _model.Meshes3D[0].Texture != null ? 1u : 0;
            _constData.World = _world;
            _constData.WVP = _world * _view * _proj;
            _constData.Transpose();

            _constBuffer0 = new SharpDX.Direct3D11.Buffer(_game.DeviceContext.Device, Utilities.SizeOf<AnimConst>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            _constBuffer1 = new SharpDX.Direct3D11.Buffer(_game.DeviceContext.Device, BonesConst.Size(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
        }

        public void Update(float time, bool animate = false, int numAnimation = 0)
        {
            _constData.HasAnimaton = 0;
            if (animate && _model.HasAnimation && numAnimation < _model.AnimationsCount)
            {
                _constData.HasAnimaton = 1;
                _bones.init(_model.Animate(time, numAnimation));
            }
            else if (_model.HasAnimation)
            {
                _constData.HasAnimaton = 1;
                _bones.init(_model.BaseBones);
            }

        }

        public void Draw(DeviceContext context)
        {
            _constData.World = _world;
            _constData.WVP = _world * _view * _proj;
            _constData.Transpose();

            context.UpdateSubresource(_bones.Bones, _constBuffer1);
            foreach (var item in _model.Meshes3D)
            {
                _constData.Dif = item.Diff;
                _constData.HasDiffuseTexture = item.Texture != null ? 1u : 0;
                context.UpdateSubresource(ref _constData, _constBuffer0);
                _shader.Begin(context, new[] { _samler }, new[] { item.Texture }, new[] { _constBuffer0, _constBuffer1 });

                context.InputAssembler.PrimitiveTopology = item.primitiveType;
                context.InputAssembler.SetVertexBuffers(0, item.VertexBinding);
                context.InputAssembler.SetIndexBuffer(item.IndexBuffer, SharpDX.DXGI.Format.R32_UInt, 0);
                context.OutputMerger.SetBlendState(null, null);
                context.DrawIndexed(item.IndexCount, 0, 0);
                _shader.End(context);
            }
        }

        public void Dispose()
        {
            Utilities.Dispose(ref _samler);
            Utilities.Dispose(ref _constBuffer0);
            Utilities.Dispose(ref _constBuffer1);
            _shader?.Dispose();
            _model?.Dispose();
        }
    }
}
