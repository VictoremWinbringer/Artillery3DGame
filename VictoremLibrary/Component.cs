
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using SharpDX;
using System.Linq;

namespace VictoremLibrary
{
    /// <summary>
    /// Базовый класс для 3D объектов. Перед рисованием обязательно вызвать метод InitBuffers.
    /// </summary>
    /// <typeparam name="V"> Тип Вертексов для буффера вершин</typeparam>
    abstract public class Component<V> : System.IDisposable where V : struct
    {
        public Matrix ObjWorld = Matrix.Identity;
        protected Buffer _indexBuffer;
        protected Buffer _vertexBuffer;
        protected VertexBufferBinding _vertexBinding;
        protected V[] _veteces;
        protected uint[] _indeces;

        /// <summary>
        /// Буффер индексов. Для заполнения его данными вызовите метод InitBuffers.
        /// </summary>
        public Buffer IndexBuffer { get { return _indexBuffer; } }

        /// <summary>
        /// Привязка буффера вершин. Для заполнения его данными вызовите метод InitBuffers.
        /// </summary>
        public VertexBufferBinding VertexBinding { get { return _vertexBinding; } }

        /// <summary>
        /// Количество индексов
        /// </summary>
        public int IndexCount { get { return _indeces.Length; } }

        /// <summary>
        /// Количество вершин.
        /// </summary>
        public int VertexCount { get { return _veteces.Count(); } }

        /// <summary>
        /// Создает буфферы Вершин и индексов.
        /// </summary>
        /// <param name="dv">Устройстов в контексте которого происходит рендеринг</param>
        protected virtual void InitBuffers(Device dv)
        {
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
