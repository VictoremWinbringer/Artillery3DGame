
using SharpDX;
using SharpDX.Direct3D11;

namespace SharpDX11GameByWinbringer.ViewModels
{
    public class ViewModel : System.IDisposable
    {
        public VertexBufferBinding VertexBinging { get; set; }
        public Buffer IndexBuffer { get; set; }
        public Buffer[] ConstantBuffers { get; set; }
        public ShaderResourceView[] Textures { get; set; }
        public int DrawedVertexCount { get; set; }

        public void Dispose()
        {
            if (ConstantBuffers != null)
                foreach (var item in ConstantBuffers)
                {
                    item?.Dispose();
                }
            if (Textures != null)
                foreach (var item in Textures)
                {
                    item?.Dispose();
                }
            IndexBuffer?.Dispose();
            VertexBinging.Buffer?.Dispose();
        }
    }
}
