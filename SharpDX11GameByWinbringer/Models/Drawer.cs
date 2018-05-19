using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.Mathematics.Interop;
using SharpDX11GameByWinbringer.ViewModels;

namespace SharpDX11GameByWinbringer.Models
{
    /// <summary>
    /// Рисует 3D примитивы на экране
    /// </summary>   
    public sealed class Drawer : System.IDisposable
    {
        #region Поля и свойства
        private HullShader _HShader = null;
        private DomainShader _DShader = null;
        private GeometryShader _GShader = null;

        private DeviceContext _dx11DeviceContext;
        private VertexShader _vertexShader;
        private PixelShader _pixelShader;
        private ShaderSignature _inputSignature;
        private InputLayout _inputLayout;
        //Параметры отображения
        private RasterizerState _rasterizerState = null;
        private BlendState _blendState = null;
        private SamplerState _samplerState = null;
        private DepthStencilState _DState = null;

        public RawColor4? BlendFactor { get; set; } = null;

        public SamplerStateDescription Samplerdescription { set { _samplerState?.Dispose(); _samplerState = new SamplerState(_dx11DeviceContext.Device, value); } }
        public DepthStencilStateDescription DepthStencilDescripshion { set { _DState?.Dispose(); _DState = new DepthStencilState(_dx11DeviceContext.Device, value);  _dx11DeviceContext.OutputMerger.DepthStencilState = _DState;         } }
        public RasterizerStateDescription RasterizerDescription { set { _rasterizerState?.Dispose(); _rasterizerState = new RasterizerState(_dx11DeviceContext.Device, value); _dx11DeviceContext.Rasterizer.State = _rasterizerState; } }
        public BlendStateDescription BlendDescription { set { _blendState?.Dispose(); _blendState = new BlendState(_dx11DeviceContext.Device, value); } }

        #endregion

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="shadersFile">Путь к файлу с шейдерами PS и VS</param>
        /// <param name="inputElements">Какие входные данные ожидает шейдер</param>
        /// <param name="dvContext">Контекст видеокарты</param>
        /// <param name="texture">Путь к текстуре</param>
        public Drawer(string shadersFile,
                      InputElement[] inputElements,
                      DeviceContext dvContext,
                      bool isTesselation = false,
                      bool isGeometri = false)
        {

            _dx11DeviceContext = dvContext;

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

            if (isTesselation)
            {
                using (var pixelShaderByteCode = ShaderBytecode.CompileFromFile(shadersFile, "HS", "hs_5_0", shaderFlags))
                {
                    _HShader = new HullShader(_dx11DeviceContext.Device, pixelShaderByteCode);
                }
                using (var pixelShaderByteCode = ShaderBytecode.CompileFromFile(shadersFile, "DS", "ds_5_0", shaderFlags))
                {
                    _DShader = new DomainShader(_dx11DeviceContext.Device, pixelShaderByteCode);
                }
            }

            if (isGeometri)
            {
                using (var pixelShaderByteCode = ShaderBytecode.CompileFromFile(shadersFile, "GS", "gs_5_0", shaderFlags))
                {
                    _GShader = new GeometryShader(_dx11DeviceContext.Device, pixelShaderByteCode);
                }
            }

            //Создаем шаблон ввода данных для шейдера
            _inputLayout = new InputLayout(_dx11DeviceContext.Device, _inputSignature, inputElements);
            var s = SamplerStateDescription.Default();
            s.AddressU = TextureAddressMode.Wrap;
            s.AddressV = TextureAddressMode.Wrap;
            s.AddressW = TextureAddressMode.Wrap;
            s.MaximumAnisotropy = 16;
            s.MaximumLod = float.MaxValue;
            s.MinimumLod = 0;
            s.Filter = Filter.MinMagMipLinear;
            Samplerdescription = s;
            var d = DepthStencilStateDescription.Default();
            d.IsDepthEnabled = true;
            d.IsStencilEnabled = false;
            DepthStencilDescripshion = d;
            var r = RasterizerStateDescription.Default();
            r.CullMode = CullMode.None;
            r.FillMode = FillMode.Solid;
            RasterizerDescription = r;
            var b = BlendStateDescription.Default();
            b.AlphaToCoverageEnable = new RawBool(true);
            BlendDescription = b;
        }

        #region Методы

        public void Draw(ViewModel VM, PrimitiveTopology primitiveTopology = PrimitiveTopology.TriangleList, bool isBlending = false, int startIndex = 0, int baseVetex = 0)
        {
            //Установка шейдеров
            _dx11DeviceContext.VertexShader.Set(_vertexShader);
            _dx11DeviceContext.PixelShader.Set(_pixelShader);
            _dx11DeviceContext.GeometryShader.Set(_GShader);
            _dx11DeviceContext.HullShader.Set(_HShader);
            _dx11DeviceContext.DomainShader.Set(_DShader);
            //Устанавливаем самплер текстуры для шейдера
            _dx11DeviceContext.PixelShader.SetSampler(0, _samplerState);

            //Задаем тип рисуемых примитивов
            _dx11DeviceContext.InputAssembler.PrimitiveTopology = primitiveTopology;

            //Устанавливаем макет для входных данных видеокарты. В нем указано какие данные ожидает шейдер
            _dx11DeviceContext.InputAssembler.InputLayout = _inputLayout;

            //Перенос данных буферов в видеокарту
            _dx11DeviceContext.InputAssembler.SetVertexBuffers(0, VM.VertexBinging);
            _dx11DeviceContext.InputAssembler.SetIndexBuffer(VM.IndexBuffer, SharpDX.DXGI.Format.R32_UInt, 0);

            if (VM.ConstantBuffers != null)
                for (int i = 0; i < VM.ConstantBuffers.Length; ++i)
                {
                    _dx11DeviceContext.VertexShader.SetConstantBuffer(i, VM.ConstantBuffers?[i]);
                    _dx11DeviceContext.PixelShader.SetConstantBuffer(i, VM.ConstantBuffers?[i]);
                    _dx11DeviceContext.GeometryShader.SetConstantBuffer(i, VM.ConstantBuffers?[i]);
                    _dx11DeviceContext.DomainShader.SetConstantBuffer(i, VM.ConstantBuffers?[i]);
                    _dx11DeviceContext.HullShader.SetConstantBuffer(i, VM.ConstantBuffers?[i]);
                }
            if (VM.Textures != null)
                for (int i = 0; i < VM.Textures.Length; ++i)
                {
                    //Отправляем текстуру в шейдер
                    _dx11DeviceContext.PixelShader.SetShaderResource(i, VM.Textures?[i]);
                }
            // _dx11DeviceContext.PixelShader.SetShaderResources(0, 1, VM.Textures);

            _dx11DeviceContext.OutputMerger.SetBlendState(null, null);
            if (isBlending) _dx11DeviceContext.OutputMerger.SetBlendState(_blendState, BlendFactor);

            //Рисуем в буффер нашего свайпчейна
            _dx11DeviceContext.DrawIndexed(VM.DrawedVertexCount, startIndex, baseVetex);

            _dx11DeviceContext.VertexShader.Set(null);
            _dx11DeviceContext.PixelShader.Set(null);
            _dx11DeviceContext.GeometryShader.Set(null);
            _dx11DeviceContext.HullShader.Set(null);
            _dx11DeviceContext.DomainShader.Set(null);
        }

        public void DrawNoIndex(ViewModel VM, int baseVetex = 0, PrimitiveTopology primitiveTopology = PrimitiveTopology.TriangleList, bool isBlending = false)
        {
            //Установка шейдеров
            _dx11DeviceContext.VertexShader.Set(_vertexShader);
            _dx11DeviceContext.PixelShader.Set(_pixelShader);

            //Устанавливаем самплер текстуры для шейдера
            _dx11DeviceContext.PixelShader.SetSampler(0, _samplerState);

            //Задаем тип рисуемых примитивов
            _dx11DeviceContext.InputAssembler.PrimitiveTopology = primitiveTopology;

            //Устанавливаем макет для входных данных видеокарты. В нем указано какие данные ожидает шейдер
            _dx11DeviceContext.InputAssembler.InputLayout = _inputLayout;

            //Перенос данных буферов в видеокарту
            _dx11DeviceContext.InputAssembler.SetVertexBuffers(0, VM.VertexBinging);

            if (VM.ConstantBuffers != null)
                for (int i = 0; i < VM.ConstantBuffers.Length; ++i)
                {
                    _dx11DeviceContext.VertexShader.SetConstantBuffer(i, VM.ConstantBuffers?[i]);
                    _dx11DeviceContext.PixelShader.SetConstantBuffer(i, VM.ConstantBuffers?[i]);
                }
            if (VM.Textures != null)
                for (int i = 0; i < VM.Textures.Length; ++i)
                {
                    //Отправляем текстуру в шейдер
                    _dx11DeviceContext.PixelShader.SetShaderResource(i, VM.Textures?[i]);
                }

            _dx11DeviceContext.Rasterizer.State = _rasterizerState;
            _dx11DeviceContext.OutputMerger.DepthStencilState = _DState;

            _dx11DeviceContext.OutputMerger.SetBlendState(null, null);
            if (isBlending) _dx11DeviceContext.OutputMerger.SetBlendState(_blendState, BlendFactor);

            //Рисуем в буффер нашего свайпчейна
            _dx11DeviceContext.Draw(VM.DrawedVertexCount, baseVetex);
        }


        #region IDisposable Support
        private bool disposedValue = false;
        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: освободить управляемое состояние (управляемые объекты).                   
                    Utilities.Dispose(ref _rasterizerState);
                    Utilities.Dispose(ref _blendState);
                    Utilities.Dispose(ref _DState);
                    Utilities.Dispose(ref _samplerState);
                    Utilities.Dispose(ref _vertexShader);
                    Utilities.Dispose(ref _pixelShader);
                    Utilities.Dispose(ref _inputLayout);
                    Utilities.Dispose(ref _inputSignature);
                    Utilities.Dispose(ref _HShader);
                    Utilities.Dispose(ref _GShader);
                    Utilities.Dispose(ref _DShader);
                }

                // TODO: освободить неуправляемые ресурсы (неуправляемые объекты) и переопределить ниже метод завершения.
                // TODO: задать большим полям значение NULL.
                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

        #endregion
    }
}
