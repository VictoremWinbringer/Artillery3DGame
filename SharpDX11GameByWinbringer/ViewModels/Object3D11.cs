using SharpDX;
using SharpDX.Direct3D11;

namespace SharpDX11GameByWinbringer.ViewModels
{
   abstract public class Object3D11<V>:System.IDisposable where V :struct
    {
        public Matrix ObjWorld;
        protected Buffer _indexBuffer;
        protected Buffer _vertexBuffer;        
        protected VertexBufferBinding _vertexBinding;       
        protected V[] _veteces;
        protected uint[] _indeces;
        public virtual void FillVM(ref ViewModel vm)
        {
            vm.IndexBuffer = _indexBuffer;
            vm.VertexBinging = _vertexBinding;
            vm.DrawedVertexCount = _indeces.Length;
        }

        protected virtual void InitBuffers(Device dv)
        {
            ObjWorld = Matrix.Identity;
            _indexBuffer = Buffer.Create(dv, BindFlags.IndexBuffer, _indeces);
            _vertexBuffer = Buffer.Create(dv, BindFlags.VertexBuffer, _veteces);
            _vertexBinding = new VertexBufferBinding(_vertexBuffer, Utilities.SizeOf<V>(), 0);
        }
        public virtual void Dispose()
        {
            Utilities.Dispose(ref _indexBuffer);
            Utilities.Dispose(ref _vertexBuffer);
        }
    }
}
