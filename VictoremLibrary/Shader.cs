using SharpDX;
using SharpDX.Direct3D11;
using System;
using Buffer = SharpDX.Direct3D11.Buffer;
using SharpDX.D3DCompiler;

namespace VictoremLibrary
{

    /// <summary>
    /// Класс для работы с шейдерами
    /// </summary>
    public class Shader : IDisposable
    {
        private DomainShader _DShader = null;
        private DeviceContext _dx11DeviceContext;
        private GeometryShader _GShader = null;
        private HullShader _HShader = null;
        private ShaderSignature _inputSignature = null;
        private PixelShader _pixelShader = null;
        private VertexShader _vertexShader = null;
        private InputLayout _inputLayout = null;

        /// <summary>
        /// Конструктор класса
        /// </summary>
        /// <param name="dC">Контекст Директ икс 11</param>
        /// <param name="shadersFile">Путь к файлу в которм описанный шейдеры. Назвалине функций шейредов должно быть VS, PS, GS, HS и DS соответственно.</param>
        ///<param name="inputElements">Входные элементы для Вертексного шейдера</param>
        /// <param name="hasGeom">Используеться ли Геометри шейдер GS</param>
        /// <param name="hasTes">Использовать ли Хулл HS и Домейн DS шейдеры необходимые для тесселяции</param>       
        public Shader(DeviceContext dC, string shadersFile, SharpDX.Direct3D11.InputElement[] inputElements, bool hasGeom = false, bool hasTes = false)
        {
            _dx11DeviceContext = dC;
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

            if (hasTes)
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

            if (hasGeom)
            {
                using (var pixelShaderByteCode = ShaderBytecode.CompileFromFile(shadersFile, "GS", "gs_5_0", shaderFlags))
                {
                    _GShader = new GeometryShader(_dx11DeviceContext.Device, pixelShaderByteCode);
                }
            }

            _inputLayout = new InputLayout(_dx11DeviceContext.Device, _inputSignature, inputElements);
        }

        public static Effect GetEffect(Device device, string file)
        {
            ShaderFlags shaderFlags = ShaderFlags.None;
#if DEBUG
            shaderFlags = ShaderFlags.Debug;
#endif
            using (var effectByteCode = ShaderBytecode.CompileFromFile(file, "fx_5_0", shaderFlags, EffectFlags.None))
            {
                var effect = new Effect(device, effectByteCode);
                return effect;
            }
        }

        /// <summary>
        /// Устанавливает шейдеры и входные данные для них.
        /// </summary>
        /// <param name="sDesc">Самплеры для текстур</param>
        /// <param name="sResource">Текстуры шейдера</param>
        /// <param name="constBuffer">Буффер констант шейдера</param>
        public void Begin(DeviceContext context,SamplerState[] sDesc = null, ShaderResourceView[] sResource = null, Buffer[] constBuffer = null)
        {
            context.VertexShader.Set(_vertexShader);
            context.PixelShader.Set(_pixelShader);
            context.GeometryShader.Set(_GShader);
            context.HullShader.Set(_HShader);
            context.DomainShader.Set(_DShader);

            context.InputAssembler.InputLayout = _inputLayout;

            if (sDesc != null)
                for (int i = 0; i < sDesc.Length; ++i)
                {
                    context.VertexShader.SetSampler(i, sDesc[i]);
                    context.PixelShader.SetSampler(i, sDesc[i]);
                    context.GeometryShader.SetSampler(i, sDesc[i]);
                    context.HullShader.SetSampler(i, sDesc[i]);
                    context.DomainShader.SetSampler(i, sDesc[i]);
                }

            if (constBuffer != null)
                for (int i = 0; i < constBuffer.Length; ++i)
                {
                   context.VertexShader.SetConstantBuffer(i, constBuffer[i]);
                   context.PixelShader.SetConstantBuffer(i, constBuffer[i]);
                   context.GeometryShader.SetConstantBuffer(i, constBuffer[i]);
                   context.DomainShader.SetConstantBuffer(i, constBuffer[i]);
                    context.HullShader.SetConstantBuffer(i, constBuffer[i]);
                }
            if (sResource != null)
                for (int i = 0; i < sResource.Length; ++i)
                {
                   context.VertexShader.SetShaderResources(0, sResource);
                   context.PixelShader.SetShaderResources(0, sResource);
                   context.GeometryShader.SetShaderResources(0, sResource);
                   context.DomainShader.SetShaderResources(0, sResource);
                    context.HullShader.SetShaderResources(0, sResource);
                }
        }

        /// <summary>
        /// Отключает шейдер.
        /// </summary>
        public void End(DeviceContext context)
        {
           context.VertexShader.Set(null);
           context.PixelShader.Set(null);
           context.GeometryShader.Set(null);
           context.HullShader.Set(null);
            context.DomainShader.Set(null);
        }

        /// <summary>
        /// Создает буффер констант для шейдера
        /// </summary>
        /// <typeparam name="T">Тип данных передаваемый в шейдер</typeparam>
        /// <returns>Буффер который можно заполнить данными для шейдера</returns>
        public Buffer CreateConstantBuffer<T>() where T : struct
        {
            return new Buffer(_dx11DeviceContext.Device,
                Utilities.SizeOf<T>(),
                ResourceUsage.Default,
                BindFlags.ConstantBuffer,
                CpuAccessFlags.None,
                ResourceOptionFlags.None,
                0);
        }

        public Buffer CreateStructuredBuffer<T>(int structureCount) where T : struct
        {
            return new Buffer(_dx11DeviceContext.Device,
               Utilities.SizeOf<T>() * structureCount,
               ResourceUsage.Default,
               BindFlags.ShaderResource | BindFlags.UnorderedAccess,
               CpuAccessFlags.None,
               ResourceOptionFlags.BufferStructured,
               Utilities.SizeOf<T>());
        }
        /// <summary>
        /// Освобождает ресурсы
        /// </summary>
        public void Dispose()
        {
            Utilities.Dispose(ref _DShader);
            Utilities.Dispose(ref _GShader);
            Utilities.Dispose(ref _DShader);
            Utilities.Dispose(ref _HShader);
            Utilities.Dispose(ref _inputSignature);
            Utilities.Dispose(ref _pixelShader);
            Utilities.Dispose(ref _vertexShader);
            Utilities.Dispose(ref _inputLayout);

        }

    }

}
