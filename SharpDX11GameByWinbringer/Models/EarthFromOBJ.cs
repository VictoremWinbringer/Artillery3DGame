using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace SharpDX11GameByWinbringer.Models
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Light
    {
        public Color4 Color;
        public Vector3 Direction;
        float padding0;
        public Vector3 CameraPosition;
        float padding1;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Matrices
    {
        public Matrix WorldViewProjection;
        public Matrix World;
        public Matrix WorldIT;
        public Matrix ViewProjection;
        public float DisplaceScale;
        public float TessellationFactor;
        Vector2 padding;
        public Matrices(Matrix w, Matrix v, Matrix p)
        {
            WorldViewProjection = w * v * p;
            World = w;
            WorldIT = Matrix.Transpose(Matrix.Invert(w));
            ViewProjection = v * p;
            DisplaceScale = 0;
            TessellationFactor = 1;
            padding = new Vector2();
        }
        internal void Transpose()
        {
            World.Transpose();
            WorldViewProjection.Transpose();
            WorldIT.Transpose();
            ViewProjection.Transpose();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MtlMaterial
    {
        public float Ns_SpecularPower;
        public float Ni_OpticalDensity;
        public float d_Transparency;
        public float Tr_Transparency;
        public Vector3 Tf_TransmissionFilter;
        float padding0;
        public Color4 Ka_AmbientColor;
        public Color4 Kd_DiffuseColor;
        public Color4 Ks_SpecularColor;
        public Color4 Ke_EmissiveColor;
    }

    public struct Face
    {
        public Vector3 V;
        public Vector3 Vn;
        public Vector3 Vt;
    }

    public class EarthFromOBJ : IDisposable
    {
        #region Поля и свойства

        Buffer _vertexBuffer;
        Buffer _constantBuffer;
        Buffer _materialsBuffer;
        Buffer _lightBuffer;
        Buffer _indexBuffer;

        VertexBufferBinding _vertexBinding;
        Light _light = new Light();
        Matrices _matrices = new Matrices();

        public Matrix World { get; set; }
        public Vector3 Center { get; set; }

        DeviceContext _dx11Context;



        int _facesCount;
        private SamplerState _samplerState;
        private ShaderResourceView _textureResourse;
        private ShaderResourceView _textureResourse2;
        private ShaderSignature _inputSignature;
        private VertexShader _vertexShader;
        private PixelShader _pixelShader;
        private InputLayout _inputLayout;
        private RasterizerState _rasterizerState;
        private DepthStencilState _DState;
        private ShaderResourceView _textureResourse1;
        private HullShader _hShader;
        private DomainShader _dShader;

        #endregion
        public bool Moving { get; set; }
        public bool isFaced { get; set; }
        public EarthFromOBJ(DeviceContext dx11Context)
        {

            _dx11Context = dx11Context;
            World = Matrix.Translation(0, 100, -5000);
            _light.Color = Color4.White;

            const string obj = "3DModelsFiles\\Earth\\earth.obj";
            const string mtl = "3DModelsFiles\\Earth\\earth.mtl";
            const string jpg = "3DModelsFiles\\Earth\\earthmap.jpg";
            const string shadersFile = "Shaders\\EarthT.hlsl";
            Tuple<List<Face>, List<uint>> tuple = GetFaces(obj);
            _facesCount = tuple.Item2.Count;

            _vertexBuffer = Buffer.Create(dx11Context.Device, BindFlags.VertexBuffer, tuple.Item1.ToArray());
            _indexBuffer = Buffer.Create(dx11Context.Device, BindFlags.IndexBuffer, tuple.Item2.ToArray());
            _vertexBinding = new VertexBufferBinding(_vertexBuffer, Utilities.SizeOf<Face>(), 0);

            MtlMaterial material = GetMaterial(mtl);
            _materialsBuffer = Buffer.Create(_dx11Context.Device, BindFlags.ConstantBuffer, ref material);

            _constantBuffer = new Buffer(_dx11Context.Device, Utilities.SizeOf<Matrices>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            _lightBuffer = new Buffer(_dx11Context.Device, Utilities.SizeOf<Light>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);

            _textureResourse = _dx11Context.LoadTextureFromFile(jpg);
            _textureResourse1 = _dx11Context.LoadTextureFromFile("3DModelsFiles\\Earth\\map.jpg");
            _textureResourse2 = _dx11Context.LoadTextureFromFile("3DModelsFiles\\Earth\\map1.jpg");


            SamplerStateDescription description = SamplerStateDescription.Default();
            description.Filter = Filter.MinMagMipLinear;
            description.AddressU = TextureAddressMode.Wrap;
            description.AddressV = TextureAddressMode.Wrap;
            description.AddressW = TextureAddressMode.Wrap;
            _samplerState = new SamplerState(_dx11Context.Device, description);

            //Загружаем шейдеры из файлов
            InputElement[] inputElements = new InputElement[]
        {
             new InputElement("SV_Position",0,Format.R32G32B32_Float,0,0),
             new InputElement("NORMAL", 0, Format.R32G32B32_Float, 12, 0),
             new InputElement("TEXCOORD", 0, Format.R32G32B32_Float, 24, 0)
        };
            ShaderFlags shaderFlags = ShaderFlags.None;
#if DEBUG
            shaderFlags = ShaderFlags.Debug;
#endif

            using (var vertexShaderByteCode = ShaderBytecode.CompileFromFile(shadersFile, "VS", "vs_5_0", shaderFlags))
            {
                //Синатура храянящая сведения о том какие входные переменные есть у шейдера
                _inputSignature = ShaderSignature.GetInputSignature(vertexShaderByteCode);
                _vertexShader = new VertexShader(_dx11Context.Device, vertexShaderByteCode);
            }
            using (var pixelShaderByteCode = ShaderBytecode.CompileFromFile(shadersFile, "PS", "ps_5_0", shaderFlags))
            {
                _pixelShader = new PixelShader(_dx11Context.Device, pixelShaderByteCode);
            }
            using (var pixelShaderByteCode = ShaderBytecode.CompileFromFile(shadersFile, "HS", "hs_5_0", shaderFlags))
            {
                _hShader = new HullShader(_dx11Context.Device, pixelShaderByteCode);
            }

            using (var pixelShaderByteCode = ShaderBytecode.CompileFromFile(shadersFile, "DS", "ds_5_0", shaderFlags))
            {
                _dShader = new DomainShader(_dx11Context.Device, pixelShaderByteCode);
            }

            _inputLayout = new InputLayout(_dx11Context.Device, _inputSignature, inputElements);

            RasterizerStateDescription rasterizerStateDescription = RasterizerStateDescription.Default();
            rasterizerStateDescription.CullMode = CullMode.None;
            rasterizerStateDescription.FillMode = FillMode.Solid;

            DepthStencilStateDescription DStateDescripshion = DepthStencilStateDescription.Default();
            DStateDescripshion.IsDepthEnabled = true;

            _DState = new DepthStencilState(_dx11Context.Device, DStateDescripshion);
            _rasterizerState = new RasterizerState(_dx11Context.Device, rasterizerStateDescription);

            _light.Direction = new Vector3(1f, -1f, 1f);
        }


        #region Методы
        bool reseting;
        public void Update(float time, Matrix w)
        {
            if (isFaced)
            {
                if (reseting) World = Matrix.Translation(0, 100, -5000);
                Moving = false;
            }
            if (Moving) reseting = true; else reseting = false;
            World = Moving ? Matrix.RotationY(-0.0005f * time) * World * w : Matrix.RotationY(-0.0005f * time) * World;
        }

        public void Draw(Matrix world, Matrix view, Matrix proj, float dispScale, int tFactor)
        {
            Matrix oWorld = World * world;
            Center = Vector3.TransformCoordinate(Vector3.Zero, oWorld);

            var camPosition = Matrix.Transpose(Matrix.Invert(view)).Column4;
            _light.CameraPosition = new Vector3(camPosition.X, camPosition.Y, camPosition.Z);

            _matrices = new Matrices(oWorld, view, proj);
            _matrices.DisplaceScale = dispScale;
            _matrices.TessellationFactor = tFactor;
            _matrices.Transpose();

            _dx11Context.UpdateSubresource(ref _light, _lightBuffer);
            _dx11Context.UpdateSubresource(ref _matrices, _constantBuffer);

            _dx11Context.VertexShader.Set(_vertexShader);
            _dx11Context.PixelShader.Set(_pixelShader);
            _dx11Context.HullShader.Set(_hShader);
            _dx11Context.DomainShader.Set(_dShader);

            _dx11Context.VertexShader.SetConstantBuffer(0, _constantBuffer);
            _dx11Context.VertexShader.SetConstantBuffer(1, _materialsBuffer);
            _dx11Context.VertexShader.SetConstantBuffer(2, _lightBuffer);
            _dx11Context.VertexShader.SetSampler(0, _samplerState);
            _dx11Context.VertexShader.SetShaderResource(0, _textureResourse);
            _dx11Context.VertexShader.SetShaderResource(1, _textureResourse1);
            _dx11Context.VertexShader.SetShaderResource(2, _textureResourse2);

            _dx11Context.PixelShader.SetConstantBuffer(0, _constantBuffer);
            _dx11Context.PixelShader.SetConstantBuffer(1, _materialsBuffer);
            _dx11Context.PixelShader.SetConstantBuffer(2, _lightBuffer);
            _dx11Context.PixelShader.SetSampler(0, _samplerState);
            _dx11Context.PixelShader.SetShaderResource(0, _textureResourse);
            _dx11Context.PixelShader.SetShaderResource(1, _textureResourse1);
            _dx11Context.PixelShader.SetShaderResource(2, _textureResourse2);

            _dx11Context.HullShader.SetConstantBuffer(0, _constantBuffer);
            _dx11Context.HullShader.SetConstantBuffer(1, _materialsBuffer);
            _dx11Context.HullShader.SetConstantBuffer(2, _lightBuffer);
            _dx11Context.HullShader.SetSampler(0, _samplerState);
            _dx11Context.HullShader.SetShaderResource(0, _textureResourse);
            _dx11Context.HullShader.SetShaderResource(1, _textureResourse1);
            _dx11Context.HullShader.SetShaderResource(2, _textureResourse2);

            _dx11Context.DomainShader.SetConstantBuffer(0, _constantBuffer);
            _dx11Context.DomainShader.SetConstantBuffer(1, _materialsBuffer);
            _dx11Context.DomainShader.SetConstantBuffer(2, _lightBuffer);
            _dx11Context.DomainShader.SetSampler(0, _samplerState);
            _dx11Context.DomainShader.SetShaderResource(0, _textureResourse);
            _dx11Context.DomainShader.SetShaderResource(1, _textureResourse1);
            _dx11Context.DomainShader.SetShaderResource(2, _textureResourse2);

            _dx11Context.InputAssembler.SetVertexBuffers(0, _vertexBinding);
            _dx11Context.InputAssembler.SetIndexBuffer(_indexBuffer, Format.R32_UInt, 0);
            _dx11Context.InputAssembler.InputLayout = _inputLayout;
            _dx11Context.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.PatchListWith3ControlPoints;

            _dx11Context.Rasterizer.State = _rasterizerState;
            _dx11Context.OutputMerger.DepthStencilState = _DState;

            _dx11Context.DrawIndexed(_facesCount, 0, 0);// Draw(_facesCount, 0);
            _dx11Context.VertexShader.Set(null);
            _dx11Context.PixelShader.Set(null);
            _dx11Context.HullShader.Set(null);
            _dx11Context.DomainShader.Set(null);
        }

        private MtlMaterial GetMaterial(string mtlFile)
        {
            CultureInfo infos = CultureInfo.InvariantCulture;
            MtlMaterial material = new MtlMaterial();
            using (StreamReader reader = new StreamReader(mtlFile))
            {
                while (true)
                {
                    string l = reader.ReadLine();
                    if (reader.EndOfStream) break;
                    if (l.Contains("map_Ka ")) break;
                    if (l.Contains("Ns "))
                        material.Ns_SpecularPower = float.Parse(l.Replace("Ns ", "").Trim(), infos);
                    if (l.Contains("Ni "))
                        material.Ni_OpticalDensity = float.Parse(l.Replace("Ni ", "").Trim(), infos);

                    if (l.Contains("\td "))
                        material.d_Transparency = float.Parse(l.Replace("d ", "").Trim(), infos);

                    if (l.Contains("Tr "))
                        material.Tr_Transparency = float.Parse(l.Replace("Tr ", "").Trim(), infos);

                    if (l.Contains("Tf "))
                    {
                        var val = l.Replace("Tf ", "").Trim().Split(' ').Select(s => float.Parse(s, infos)).ToArray();
                        material.Tf_TransmissionFilter = new Vector3(val[0], val[1], val[2]);
                    }
                    if (l.Contains("Ka "))
                    {
                        var val = l.Replace("Ka ", "").Trim().Split(' ').Select(s => float.Parse(s, infos)).ToArray();
                        material.Ka_AmbientColor = new Color4(val[0], val[1], val[2], 1);
                    }
                    if (l.Contains("Kd "))
                    {
                        var val = l.Replace("Kd ", "").Trim().Split(' ').Select(s => float.Parse(s, infos)).ToArray();
                        material.Kd_DiffuseColor = new Color4(val[0], val[1], val[2], 1);
                    }
                    if (l.Contains("Ks "))
                    {
                        var val = l.Replace("Ks ", "").Trim().Split(' ').Select(s => float.Parse(s, infos)).ToArray();
                        material.Ks_SpecularColor = new Color4(val[0], val[1], val[2], 1);
                    }
                    if (l.Contains("Ke "))
                    {
                        var val = l.Replace("Ke ", "").Trim().Split(' ').Select(s => float.Parse(s, infos)).ToArray();
                        material.Ke_EmissiveColor = new Color4(val[0], val[1], val[2], 0);
                    }
                }
            }
            return material;
        }

        private Tuple<List<Face>, List<uint>> GetFaces(string objFile)
        {
            CultureInfo infos = CultureInfo.InvariantCulture;
            List<string> lines = ReadOBJFile(objFile);
            List<Vector3> verteces = GetVectors("v ", lines);
            List<Vector3> normals = GetVectors("vn ", lines);
            List<Vector3> textureUVW = GetVectors("vt ", lines);
            List<Face> faces = new List<Face>();
            List<uint> index = new List<uint>();
            uint Count = 0;
            foreach (string line in lines)
            {
                if (line.Contains("f "))
                {
                    var coords = line.Replace("f ", "").Trim().Split(' ');
                    foreach (var item in coords)
                    {
                        var indeces = item.Split('/').Select(s => int.Parse(s, infos)).ToArray();

                        Vector3 V = verteces[indeces[0] - 1];
                        V.Z = -1f * V.Z;

                        Vector3 Vt = textureUVW[indeces[1] - 1];
                        Vt.Y = -1f * Vt.Y;

                        Vector3 Vn = normals[indeces[2] - 1];
                        Vn.Z = -1f * Vn.Z;

                        Face face = new Face();
                        face.V = V;
                        face.Vt = Vt;
                        face.Vn = Vn;
                        int i = faces.FindIndex(t => (t.V == face.V) && (t.Vn == face.Vn) && (t.Vt == face.Vt));
                        if (i >= 0)
                        {
                            index.Add((uint)i);
                        }
                        else
                        {
                            faces.Add(face);
                            index.Add(Count);
                            ++Count;
                        }

                    }
                }

            }
            return new Tuple<List<Face>, List<uint>>(faces, index);
        }

        private List<string> ReadOBJFile(string obj)
        {
            List<string> lines = new List<string>();
            using (StreamReader reader = new StreamReader(obj))
            {
                while (true)
                {
                    string l = reader.ReadLine();
                    if (reader.EndOfStream)
                        break;

                    if (l.Contains("#") || string.IsNullOrEmpty(l.Trim()))
                        continue;

                    lines.Add(l);
                }
            }
            return lines;
        }

        private List<Vector3> GetVectors(string type, List<string> lines)
        {
            CultureInfo infos = CultureInfo.InvariantCulture;
            List<Vector3> vectors = new List<Vector3>();
            foreach (string line in lines)
            {
                if (line.Contains(type))
                {
                    string[] coords = line.Replace(type, "").Trim().Split(' ');
                    vectors.Add(new Vector3(Convert.ToSingle(coords[0], infos), Convert.ToSingle(coords[1], infos), Convert.ToSingle(coords[2], infos)));
                }
            }
            return vectors;
        }

        public void Dispose()
        {
            Utilities.Dispose(ref _textureResourse2);
            Utilities.Dispose(ref _textureResourse1);
            Utilities.Dispose(ref _vertexBuffer);
            Utilities.Dispose(ref _constantBuffer);
            Utilities.Dispose(ref _materialsBuffer);
            Utilities.Dispose(ref _lightBuffer);
            Utilities.Dispose(ref _samplerState);
            Utilities.Dispose(ref _textureResourse);
            Utilities.Dispose(ref _inputSignature);
            Utilities.Dispose(ref _vertexShader);
            Utilities.Dispose(ref _pixelShader);
            Utilities.Dispose(ref _hShader);
            Utilities.Dispose(ref _dShader);
            Utilities.Dispose(ref _inputLayout);
            Utilities.Dispose(ref _rasterizerState);
            Utilities.Dispose(ref _DState);
            Utilities.Dispose(ref _indexBuffer);
            _vertexBinding.Buffer.Dispose();
        }

        #endregion
    }
}
