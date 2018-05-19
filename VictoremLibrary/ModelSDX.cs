using Assimp;
using Assimp.Configs;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace VictoremLibrary
{
    public struct Frame
    {
        public Assimp.Quaternion rot;
        public Vector3D pos;
        public Vector3D scal;
    }

    struct Bone
    {
        public string Name;
        public string Parent;
        public Matrix Transform;
        public Matrix GlobalTransform;
        public Matrix Offset;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct AssimpVertex
    {
        public Vector3 position;
        public Vector3 normal;
        public Vector3 uv;
        public Vector3 tangent;
        public Vector3 biTangent;
        public Vector4 BoneID;
        public Vector4 BoneWheight;
        public Color4 Color;
    }

    public class AnimationSDX
    {
        public float FramePerSecond { get; private set; } = 25;
        public float FrameDuration { get; private set; } = 0;
        public float CurrentFrame { get; set; } = 0;
        public int DurationInTicks { get; } = 0;
        public float DurationInMiliseconds { get; } = 0;
        public Dictionary<string, Frame[]> Frames { get { return frames; } }
        Dictionary<string, Frame[]> frames = new Dictionary<string, Frame[]>();

        public AnimationSDX(Assimp.Animation animation)
        {
            FramePerSecond = (float)(animation.TicksPerSecond > 24 ? animation.TicksPerSecond : 25);
            FrameDuration = 1000 / FramePerSecond;
            foreach (var n in animation.NodeAnimationChannels)
            {
                frames.Add(n.NodeName, GetFrames(n).ToArray());
            }
            DurationInTicks = frames.Values.Max(x => x.Length);
            DurationInMiliseconds = DurationInTicks * FrameDuration;
        }

        public void SetFramesPerSecontRate(int framePerSecond)
        {
            FramePerSecond = framePerSecond;
            FrameDuration = 1000 / FramePerSecond;
        }

        IEnumerable<Frame> GetFrames(NodeAnimationChannel nch)
        {
            var m = new[] { nch.PositionKeyCount, nch.RotationKeyCount, nch.ScalingKeyCount }.Max();
            for (int i = 0; i < m; i++)
            {
                var pos = new Vector3D();
                var scale = new Vector3D(1);
                var rot = new Assimp.Quaternion(1, 0, 0, 0);
                if (nch.HasPositionKeys)
                {
                    if (i < nch.PositionKeyCount)
                    {
                        pos = nch.PositionKeys[i].Value;

                    }
                    else
                    {
                        pos = nch.PositionKeys.Last().Value;
                    }
                }
                if (nch.HasRotationKeys)
                {
                    if (i < nch.RotationKeyCount)
                    {
                        rot = nch.RotationKeys[i].Value;
                    }
                    else
                    {
                        rot = nch.RotationKeys.Last().Value;
                    }
                }
                if (nch.HasScalingKeys)
                {
                    if (i < nch.ScalingKeyCount)
                    {
                        scale = nch.ScalingKeys[i].Value;
                    }
                    else
                    {
                        scale = nch.ScalingKeys.Last().Value;
                    }
                }

                yield return new Frame()
                {
                    pos = pos,
                    rot = rot,
                    scal = scale
                };
            }
            yield break;
        }
    }

    public class AssimpMesh
    {
        public Vector3 Center { get; set; }
        public Color4 Dif { get; set; }
        public bool HasBones { get; set; }
        public string Texture { get; set; } = null;
        public AssimpVertex[] Veteces { get; set; } = null;
        public uint[] Indeces { get; set; } = null;
        public string NormalMap { get; set; } = null;
        public string DiplacementMap { get; set; } = null;
        public string SpecularMap { get; set; } = null;
        public string ReflectionMap { get; set; } = null;
        public string LightMap { get; set; } = null;
        public string EmissiveMap { get; set; } = null;
        public string HeightMap { get; set; } = null;
        public string OpacityMap { get; set; } = null;
        public bool Show { get; set; }
        public bool HasReflection { get; set; }
        public float ReflectionAmoutn { get; set; }
        public PrimitiveTopology primitiveType { get; set; }
    }

    public class Mesh3D : Component<AssimpVertex>
    {
        public Vector3 Center { get; private set; }
        public Color4 Diff { get; set; }
        ShaderResourceView _textures;
        public ShaderResourceView Texture { get { return _textures; } set { Utilities.Dispose(ref _textures); _textures = value; } }
        ShaderResourceView _normalMap;
        public ShaderResourceView NormalMap { get { return _normalMap; } set { Utilities.Dispose(ref _normalMap); _normalMap = value; } }
        ShaderResourceView _specularMap;
        public ShaderResourceView SpecularMap { get { return _specularMap; } set { Utilities.Dispose(ref _specularMap); _specularMap = value; } }
        ShaderResourceView _displacementMap;
        public ShaderResourceView DisplacementMap { get { return _displacementMap; } set { Utilities.Dispose(ref _displacementMap); _displacementMap = value; } }
        public bool Show { get; set; }
        public bool HasReflection { get; set; }
        public float ReflectionAmoutn { get; set; }
        public PrimitiveTopology primitiveType { get; set; }
        public Texture2D EnvMap { get { return _EnvMap; } }
        public RenderTargetView EnvMapRTV { get { return _EnvMapRTV; } }
        public DepthStencilView EnvMapDSV { get { return _EnvMapDSV; } }
        public ShaderResourceView EnvMapSRV { get { return _EnvMapSRV; } }
        Texture2D _EnvMap;
        RenderTargetView _EnvMapRTV;
        DepthStencilView _EnvMapDSV;
        ShaderResourceView _EnvMapSRV;

        public Mesh3D(SharpDX.Direct3D11.Device device, AssimpMesh mesh, string texturFolder)
        {
            Center = mesh.Center;
            primitiveType = mesh.primitiveType;
            Diff = mesh.Dif;
            this._indeces = mesh.Indeces;
            this._veteces = mesh.Veteces;
            InitBuffers(device);
            if (!string.IsNullOrEmpty(mesh.Texture))
                _textures = StaticMetods.LoadTextureFromFile(device.ImmediateContext, Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, texturFolder + mesh.Texture)));
            //if (!string.IsNullOrEmpty(mesh.NormalMap))
            //    _normalMap = StaticMetods.LoadTextureFromFile(device.ImmediateContext, Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, texturFolder + mesh.NormalMap)));
            //if (!string.IsNullOrEmpty(mesh.SpecularMap))
            //    _specularMap = StaticMetods.LoadTextureFromFile(device.ImmediateContext, Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, texturFolder + mesh.SpecularMap)));
            //if (!string.IsNullOrEmpty(mesh.DiplacementMap))
            //    _displacementMap = StaticMetods.LoadTextureFromFile(device.ImmediateContext, Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, texturFolder + mesh.DiplacementMap)));
        }

        public override void Dispose()
        {
            base.Dispose();
            Utilities.Dispose(ref _textures);
            Utilities.Dispose(ref _normalMap);
            Utilities.Dispose(ref _displacementMap);
            Utilities.Dispose(ref _specularMap);
        }
    }

    public class ModelSDX : IDisposable
    {
        #region Fields
        public readonly InputElement[] SkinnedPosNormalTexTanBiCol = {
                new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, InputClassification.PerVertexData, 0),
                new InputElement("NORMAL", 0, Format.R32G32B32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData, 0),
                new InputElement("TEXCOORD", 0, Format.R32G32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData, 0),
                new InputElement("TANGENT", 0, Format.R32G32B32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData,0 ),
                 new InputElement("BINORMAL", 0, Format.R32G32B32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData,0 ),
                  new InputElement("BLENDINDICES", 0, Format.R32G32B32A32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData, 0),
                new InputElement("BLENDWEIGHT", 0, Format.R32G32B32A32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData, 0),
                 new InputElement("COLOR", 0, Format.R32G32B32A32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData, 0)
            };
        List<AnimationSDX> _animations = new List<AnimationSDX>();
        List<Bone> _bones;
        Mesh3D[] _3dMeshes;
        private Matrix[] _baseTransform;
        #endregion

        #region Propertis 
        public List<AnimationSDX> Animations { get { return _animations; } }
        public int AnimationsCount { get { return _animations.Count; } }
        public Mesh3D[] Meshes3D { get { return _3dMeshes; } }
        public bool HasAnimation { get; private set; } = false;
        public Matrix[] BaseBones { get { return _baseTransform; } }
        public Vector3 Center { get; private set; }
        public Matrix World { get; set; } = Matrix.Identity;
        #endregion

        public ModelSDX(SharpDX.Direct3D11.Device device, string Folder, string File)
        {
            string fileName = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), Folder + File);
            using (AssimpContext importer = new AssimpContext())
            {
                NormalSmoothingAngleConfig config = new NormalSmoothingAngleConfig(66.0f);
                importer.SetConfig(config);
                var Model = importer
                    .ImportFile(fileName,
                    PostProcessPreset.ConvertToLeftHanded |
                    PostProcessSteps.CalculateTangentSpace |
                    PostProcessSteps.FindInvalidData |
                    PostProcessSteps.GenerateSmoothNormals |
                    PostProcessSteps.GenerateUVCoords |
                    PostProcessSteps.JoinIdenticalVertices |
                    PostProcessSteps.LimitBoneWeights |
                    PostProcessSteps.SortByPrimitiveType |
                    PostProcessSteps.TransformUVCoords |
                    PostProcessSteps.Triangulate);

                if (Model.HasAnimations && Model.Animations.Any(x => x.HasNodeAnimations))
                {
                    HasAnimation = true;

                    _bones = GetBones(Model).ToList();
                    for (int i = 0; i < _bones.Count; i++)
                    {
                        if (!_bones.Any(x => x.Name.Equals(_bones[i].Parent)))
                        {
                            var b = _bones[i];
                            b.Parent = null;
                            _bones[i] = b;
                        }

                    }
                    BildBone(ref _bones);

                    _baseTransform = GetNodeTransforms().ToArray();
                    foreach (var anim in Model.Animations)
                    {
                        _animations.Add(new AnimationSDX(anim));
                    }
                }
                else
                {
                    Model.Clear();
                    Model = importer
                    .ImportFile(fileName,
                    PostProcessPreset.ConvertToLeftHanded |
                    PostProcessSteps.CalculateTangentSpace |
                   PostProcessSteps.FindInvalidData |
                    PostProcessSteps.GenerateSmoothNormals |
                   PostProcessSteps.GenerateUVCoords |
                   PostProcessSteps.JoinIdenticalVertices |
                   PostProcessSteps.SortByPrimitiveType |
                   PostProcessSteps.TransformUVCoords |
                    PostProcessSteps.Triangulate |
                   PostProcessSteps.PreTransformVertices
                 );
                }
                var verts = Model.Meshes.SelectMany(x => x.Vertices).ToArray();
                var X = verts.Sum(x => x.X) / verts.Length;
                var Y = verts.Sum(x => x.Y) / verts.Length;
                var Z = verts.Sum(x => x.Z) / verts.Length;
                Center = new Vector3(X, Y, Z);
                var _meshes = GetMeshes(Model).ToArray();
                _3dMeshes = Create3DMeshes(_meshes, device, Folder).ToArray();
                Model.Clear();
            }
        }

        #region Metods

        public Matrix[] Animate(float time, int animation)
        {
            if (!HasAnimation || _animations.Count == 0) throw new ArgumentOutOfRangeException("У этой модели нет анимации");

            if (animation >= AnimationsCount) throw new ArgumentOutOfRangeException($"Номер анимации за приделами максимального, максимальный номер : {AnimationsCount - 1}");

            if (_animations[animation].CurrentFrame > 90000f) _animations[animation].CurrentFrame -= 90000f;

            if (_animations[animation].CurrentFrame < 0) _animations[animation].CurrentFrame = 0;

            _animations[animation].CurrentFrame += time / _animations[animation].FrameDuration;

            var frame = _animations[animation].CurrentFrame;

            float factor = frame - (float)Math.Floor(frame);

            int frInt = (int)Math.Floor(frame);

            for (int i = 0; i < _bones.Count; i++)
            {
                var b = _bones[i];

                if (!_animations[animation].Frames.ContainsKey(b.Name))
                    continue;

                var a = _animations[animation].Frames[b.Name];

                int frame0 = frInt;

                while (frame0 >= a.Length)
                {
                    frame0 -= a.Length;
                }

                int frame1 = frame0 + 1;

                if (frame1 >= a.Length) frame1 = 0;

                var fr = LerpFrame(a[frame0], a[frame1], factor);

                b.Transform = GetMatrix(fr.scal, fr.pos, fr.rot);
                _bones[i] = b;
            }
            BildBone(ref _bones);
            return GetNodeTransforms().ToArray();

        }

        Matrix GetMatrix(Vector3D pscale, Vector3D pPosition, Assimp.Quaternion pRot)
        {
            // create the combined transformation matrix
            var mat = new Matrix4x4(pRot.GetMatrix());
            mat.A1 *= pscale.X; mat.B1 *= pscale.X; mat.C1 *= pscale.X;
            mat.A2 *= pscale.Y; mat.B2 *= pscale.Y; mat.C2 *= pscale.Y;
            mat.A3 *= pscale.Z; mat.B3 *= pscale.Z; mat.C3 *= pscale.Z;
            mat.A4 = pPosition.X; mat.B4 = pPosition.Y; mat.C4 = pPosition.Z;
            return mat.ToMatrix();
        }

        Frame LerpFrame(Frame a, Frame b, float c)
        {
            var f = new Frame();
            f.pos = a.pos + (b.pos - a.pos) * c;
            f.scal = a.scal + (b.scal - a.scal) * c;
            f.rot = Assimp.Quaternion.Slerp(a.rot, b.rot, c);
            f.rot.Normalize();
            return f;
        }

        IEnumerable<Matrix> GetTransforms()
        {
            foreach (var b in _bones)
            {
                yield return b.GlobalTransform;
            }
            yield break;
        }

        IEnumerable<Matrix> GetNodeTransforms()
        {
            foreach (var b in _bones)
            {
                yield return b.Offset * b.GlobalTransform;
            }
            yield break;
        }

        void BildBone(ref List<Bone> b)
        {
            for (int i = 0; i < b.Count; i++)
            {
                string p = b[i].Parent;
                var bb = b[i];
                bb.GlobalTransform = string.IsNullOrWhiteSpace(p) ? bb.Transform : (bb.Transform * b.First(x => x.Name.Equals(p)).GlobalTransform);
                b[i] = bb;
            }
        }


        #region Загрузка данных из Ассимп модели

        IEnumerable<AssimpVertex> GetVertex(Assimp.Mesh m)
        {
            for (int i = 0; i < m.VertexCount; ++i)
            {
                Vector3 cc = m.Vertices[i].ToVector3();
                var bw = GetWAndB(m, i);
                yield return new AssimpVertex()
                {
                    position = cc,
                    uv = m.HasTextureCoords(0) ? m.TextureCoordinateChannels[0][i].ToVector3() : new Vector3(),
                    tangent = m.HasTangentBasis ? m.Tangents[i].ToVector3() : new Vector3(),
                    biTangent = m.HasTangentBasis ? m.BiTangents[i].ToVector3() : new Vector3(),
                    normal = m.HasNormals ? m.Normals[i].ToVector3() : new Vector3(),
                    BoneID = HasAnimation && m.HasBones ? GetBoneID(bw) : new Vector4(),
                    BoneWheight = HasAnimation && m.HasBones ? GetWheight(bw) : new Vector4(),
                    Color = m.HasVertexColors(0) ? m.VertexColorChannels[0][i].ToColor4() : new Color4(0.5f, 0.5f, 0.5f, 1)
                };
            }
            yield break;
        }

        dynamic[] GetWAndB(Assimp.Mesh m, int i)
        {
            var b = m.Bones.Where(bb => bb.HasVertexWeights && bb.VertexWeights.Any(tt => tt.VertexID == i))
                .Select(x => new
                {
                    Id = _bones.FindIndex(y => y.Name.Equals(x.Name)),
                    Wt = x.VertexWeights.First(y => y.VertexID == i).Weight
                }).ToList();
            return b.ToArray();
        }

        Vector4 GetBoneID(dynamic[] my)
        {
            Vector4 ret = new Vector4();
            ret.X = my[0].Id;
            ret.Y = my.Length > 1 ? my[1].Id : 0;
            ret.Z = my.Length > 2 ? my[2].Id : 0;
            ret.W = my.Length > 3 ? my[3].Id : 0;
            return ret;
        }

        Vector4 GetWheight(dynamic[] my)
        {
            Vector4 ret = new Vector4();

            ret.X = my[0].Wt;
            ret.Y = my.Length > 1 ? my[1].Wt : 0;
            ret.Z = my.Length > 2 ? my[2].Wt : 0;
            ret.W = my.Length > 3 ? my[3].Wt : 0;
            return ret;
        }

        public IEnumerable<Mesh3D> Create3DMeshes(AssimpMesh[] m, SharpDX.Direct3D11.Device d, string textureDir)
        {
            foreach (var i in m)
            {
                yield return new Mesh3D(d, i, textureDir);
            }
            yield break;
        }

        IEnumerable<AssimpMesh> GetMeshes(Scene model)
        {
            var m = new List<AssimpMesh>();
            foreach (var mesh in model.Meshes)
            {
                var verts = mesh.Vertices.ToArray();
                var X = verts.Sum(x => x.X) / verts.Length;
                var Y = verts.Sum(x => x.Y) / verts.Length;
                var Z = verts.Sum(x => x.Z) / verts.Length;
                var c = new Vector3(X, Y, Z);
                TextureSlot t;
                var s = model.Materials[mesh.MaterialIndex].GetMaterialTexture(TextureType.Diffuse, 0, out t) ? t.FilePath : null;
                yield return new AssimpMesh()
                {
                    Indeces = mesh.GetIndices().Select(i => (uint)i).ToArray(),
                    Veteces = GetVertex(mesh).ToArray(),
                    HasBones = mesh.HasBones,
                    Center = c,
                    Dif = model.Materials[mesh.MaterialIndex].ColorDiffuse.ToColor4(),
                    Texture = Path.GetFileName(s),
                    NormalMap = Path.GetFileName(model.Materials[mesh.MaterialIndex].TextureNormal.FilePath),
                    SpecularMap = Path.GetFileName(model.Materials[mesh.MaterialIndex].TextureSpecular.FilePath),
                    DiplacementMap = Path.GetFileName(model.Materials[mesh.MaterialIndex].TextureDisplacement.FilePath),
                    ReflectionMap = Path.GetFileName(model.Materials[mesh.MaterialIndex].TextureReflection.FilePath),
                    LightMap = Path.GetFileName(model.Materials[mesh.MaterialIndex].TextureLightMap.FilePath),
                    EmissiveMap = Path.GetFileName(model.Materials[mesh.MaterialIndex].TextureEmissive.FilePath),
                    HeightMap = Path.GetFileName(model.Materials[mesh.MaterialIndex].TextureHeight.FilePath),
                    OpacityMap = Path.GetFileName(model.Materials[mesh.MaterialIndex].TextureOpacity.FilePath),
                    primitiveType = mesh.PrimitiveType == PrimitiveType.Triangle ? PrimitiveTopology.TriangleList : mesh.PrimitiveType == PrimitiveType.Line ? PrimitiveTopology.LineList : PrimitiveTopology.PointList
                };
            }
            yield break;
        }

        IEnumerable<Bone> GetBones(Scene scene)
        {
            int i = 0;
            var m = scene.Meshes.SelectMany(x => x.Bones).Distinct(new Comp()).ToList();
            List<Node> l = new List<Node>();
            GetChidlren(scene.RootNode, ref l);
            foreach (var item in l)
            {
                var of = m.SingleOrDefault(x => x.Name.Equals(item.Name));
                yield return new Bone()
                {
                    Name = string.IsNullOrWhiteSpace(item.Name) ? "foo_" + i++ : item.Name,
                    Transform = item.Transform.ToMatrix(),
                    Parent = item.Parent?.Name,
                    Offset = of?.OffsetMatrix.ToMatrix() ?? new Matrix()
                };
            }
            yield break;
        }


        void GetChidlren(Node node, ref List<Node> l)
        {
            l.Add(node);
            foreach (var n in node.Children)
            {
                GetChidlren(n, ref l);
            }
        }

        #endregion

        public void Dispose()
        {
            for (int i = 0; i < _3dMeshes.Length; i++)
            {
                _3dMeshes?[i]?.Dispose();
            }
        }
        #endregion
    }

    class Comp : IEqualityComparer<Assimp.Bone>
    {
        public bool Equals(Assimp.Bone x, Assimp.Bone y)
        {
            return x.Name == y.Name;
        }

        public int GetHashCode(Assimp.Bone obj)
        {
            return 1;
        }

    }
}
