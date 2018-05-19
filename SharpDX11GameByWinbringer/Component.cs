using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using SharpDX.Mathematics.Interop;
using SharpDX11GameByWinbringer.Models;
using System.Linq;

namespace SharpDX11GameByWinbringer
{
    public abstract class Component<V,B>:System.IDisposable where V : struct where B:struct
    {
        // .............................................///
        protected DeviceContext _dx11DeviceContext;
        private VertexShader _vertexShader;
        private PixelShader _pixelShader;
        private ShaderSignature _inputSignature;
        private InputLayout _inputLayout;
        private ShaderResourceView _textureResourse;
        //Параметры отображения
        private RasterizerState _rasterizerState = null;
        private BlendState _blendState = null;
        private SamplerState _samplerState = null;
        private DepthStencilState _DState = null;       
        //...............................................//       
        protected V[] _verteces;
        protected uint[] _indeces;
        private Matrix _world;
        protected B _constantBufferData;
        private Buffer _indexBuffer;
        protected Buffer _constantBuffer;
        private Buffer _triangleVertexBuffer;
        private VertexBufferBinding _vertexBinging;
        //.................................................//

        public Matrix World { get { return _world; } set { _world = value; } }

        public  virtual void Dispose()
        {
            _vertexBinging.Buffer.Dispose();
            _vertexBinging = new VertexBufferBinding();
            Utilities.Dispose(ref _vertexShader);
            Utilities.Dispose(ref _pixelShader);
            Utilities.Dispose(ref _inputSignature);
            Utilities.Dispose(ref _inputLayout);
            Utilities.Dispose(ref _textureResourse);
            Utilities.Dispose(ref _rasterizerState);
            Utilities.Dispose(ref _blendState);
            Utilities.Dispose(ref _samplerState);
            Utilities.Dispose(ref _DState);
            Utilities.Dispose(ref _indexBuffer);
            Utilities.Dispose(ref _constantBuffer);
            Utilities.Dispose(ref _triangleVertexBuffer);      
        }

        protected abstract void CreateVertexAndIndeces();

        protected abstract void CreateState();

        protected void CreateBuffers()
        {
            //Создаем буфферы для видеокарты
            _triangleVertexBuffer = Buffer.Create<V>(_dx11DeviceContext.Device, BindFlags.VertexBuffer, _verteces);
            _indexBuffer = Buffer.Create(_dx11DeviceContext.Device, BindFlags.IndexBuffer, _indeces);
            _constantBuffer = new Buffer(_dx11DeviceContext.Device, Utilities.SizeOf<B>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            _vertexBinging = new VertexBufferBinding(_triangleVertexBuffer, Utilities.SizeOf<V>(), 0);
        }

        protected void InitDrawer( string shadersFile,
            InputElement[] inputElements,
            string texture,
            SamplerStateDescription description,
            DepthStencilStateDescription DStateDescripshion,
            RasterizerStateDescription rasterizerStateDescription,
            BlendStateDescription blendDescription)
        {

            //Загружаем шейдеры из файлов
            ShaderFlags shaderFlags = ShaderFlags.None;
#if DEBUG
            shaderFlags = ShaderFlags.Debug;
#endif

            using (var vertexShaderByteCode = ShaderBytecode.CompileFromFile(shadersFile, "VS", "vs_5_0", shaderFlags))
            {
                //Синатура храянящая сведения о том какие входные переменные есть у шейдера
                _inputSignature = ShaderSignature.GetInputSignature(vertexShaderByteCode);
                _vertexShader = new VertexShader(_dx11DeviceContext.Device, vertexShaderByteCode);
            }
            using (var pixelShaderByteCode = ShaderBytecode.CompileFromFile(shadersFile, "PS", "ps_5_0", shaderFlags))
            {
                _pixelShader = new PixelShader(_dx11DeviceContext.Device, pixelShaderByteCode);
            }
            //Создаем шаблон ввода данных для шейдера
            _inputLayout = new InputLayout(_dx11DeviceContext.Device, _inputSignature, inputElements);
            //Загружаем текстуру           
            _textureResourse = _dx11DeviceContext.LoadTextureFromFile(texture);
            //Создание самплера для текстуры
            _samplerState = new SamplerState(_dx11DeviceContext.Device, description);
            _DState = new DepthStencilState(_dx11DeviceContext.Device, DStateDescripshion);
            _rasterizerState = new RasterizerState(_dx11DeviceContext.Device, rasterizerStateDescription);
            _blendState = new BlendState(_dx11DeviceContext.Device, blendDescription);
        }

        public abstract void UpdateConsBufData(Matrix world, Matrix view, Matrix proj);       

        protected void PreDraw(SharpDX.Direct3D.PrimitiveTopology PTolology, bool isBlending = false, RawColor4? blendFactor = null)
        {
            //Установка шейдеров
            _dx11DeviceContext.VertexShader.Set(_vertexShader);
            _dx11DeviceContext.PixelShader.Set(_pixelShader);
            //Устанавливаем самплер текстуры для шейдера
            _dx11DeviceContext.PixelShader.SetSampler(0, _samplerState);
            //Задаем тип рисуемых примитивов
            _dx11DeviceContext.InputAssembler.PrimitiveTopology = PTolology;
            //Устанавливаем макет для входных данных видеокарты. В нем указано какие данные ожидает шейдер
            _dx11DeviceContext.InputAssembler.InputLayout = _inputLayout;
            //Перенос данных буферов в видеокарту
            _dx11DeviceContext.InputAssembler.SetVertexBuffers(0, _vertexBinging);
            _dx11DeviceContext.InputAssembler.SetIndexBuffer(_indexBuffer, SharpDX.DXGI.Format.R32_UInt, 0);
            _dx11DeviceContext.VertexShader.SetConstantBuffer(0, _constantBuffer);            
            //Отправляем текстуру в шейдер
            _dx11DeviceContext.PixelShader.SetShaderResource(0, _textureResourse);
            _dx11DeviceContext.Rasterizer.State = _rasterizerState;
            _dx11DeviceContext.OutputMerger.DepthStencilState = _DState;
            _dx11DeviceContext.OutputMerger.SetBlendState(null, null);
            if (isBlending) _dx11DeviceContext.OutputMerger.SetBlendState(_blendState, blendFactor);           
        }

        protected void Draw()
        {
            //Рисуем в буффер нашего свайпчейна
            _dx11DeviceContext.DrawIndexed(_indeces.Count(), 0, 0);
        }

       
               
    }
}
