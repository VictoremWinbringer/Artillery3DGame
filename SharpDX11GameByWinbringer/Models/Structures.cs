using SharpDX;
using System.Runtime.InteropServices;

namespace SharpDX11GameByWinbringer.Models
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PerMaterial
    {
        public Color4 Ambient;
        public Color4 Diffuse;
        public Color4 Specular;
        public float SpecularPower;
        /// <summary>
        /// Has texture 0 false, 1 true
        /// </summary>
        public uint HasTexture; 
        Vector2 _padding0;
        public Color4 Emissive;
        public Matrix UVTransform; // Support UV transforms 
    } 
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DirectionalLight
    {
        public SharpDX.Color4 Color;
        public SharpDX.Vector3 Direction;
        float _padding0;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PerFrame
    {
        public DirectionalLight Light;
        public Vector3 CameraPosition;
        float _padding0;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PerObject
    {
        public Matrix WorldViewProjection;
        // World matrix to calculate lighting in world space  
        public Matrix World;
        // Inverse transpose of World (for normals)  
        public Matrix WorldInverseTranspose;
        // Transpose the matrices so that they are in column  
        // major order for HLSL    
        internal void Transpose()
        {
            this.World.Transpose();
            this.WorldInverseTranspose.Transpose();
            this.WorldViewProjection.Transpose();
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VertexN
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Color Color;
        public Vector2 UV;

        public VertexN(Vector3 position, Vector3 normal, Color color, Vector2 uv)
        {
            Position = position;
            Normal = normal;
            Color = color;
            UV = uv;
        }
        public VertexN(Vector3 position, Color color, Vector2 uv) : this(position, Vector3.Normalize(position), color, uv)
        {
        }
        public VertexN(Vector3 position, Vector2 uv) : this(position, Color.Gray, uv)
        {
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 80)]
    public struct DataT
    {
        [FieldOffset(0)]
        public float Time;
        [FieldOffset(16)]
        public Matrix WVP;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Data
    {
        public Matrix WVP;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Vertex
    {
        public Vertex(Vector3 position, Vector2 textureUV)
        {
            Position = position;
            TextureUV = textureUV;
        }
        public Vector3 Position;
        public Vector2 TextureUV;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct ColoredVertex
    {
        public ColoredVertex(Vector3 position, Vector4 color)
        {
            Position = position;
            Color = color;
        }
        public Vector3 Position;
        public Vector4 Color;
    }

}
