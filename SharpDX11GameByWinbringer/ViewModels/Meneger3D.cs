using SharpDX;
using SharpDX.Direct3D11;
using SharpDX11GameByWinbringer.Models;


namespace SharpDX11GameByWinbringer.ViewModels
{
    public abstract class Meneger3D : System.IDisposable
    {
        protected Drawer _drawer;
        protected ViewModel _viewModel = new ViewModel();
        public Matrix World;
        /// <summary>
        /// Устанавливает параметры блендинга, растеризации, Буффера глубины и бледн фактор (влияет на то какой процент из цвета пикселя будет усачтвовать в блендинге)
        /// </summary>
        protected virtual void SetStates()
        {
            //Установка Сампрелар для текстуры.
            SamplerStateDescription description = SamplerStateDescription.Default();
            description.Filter = Filter.MinMagMipLinear;
            description.AddressU = TextureAddressMode.Wrap;
            description.AddressV = TextureAddressMode.Wrap;
            _drawer.Samplerdescription = description;

            //Устанавливаем параметры буффера глубины
            DepthStencilStateDescription DStateDescripshion = new DepthStencilStateDescription()
            {
                IsDepthEnabled = true,
                DepthComparison = Comparison.Less,
                DepthWriteMask = SharpDX.Direct3D11.DepthWriteMask.All,
                IsStencilEnabled = false,
                StencilReadMask = 0xff, // 0xff (no mask) 
                StencilWriteMask = 0xff,// 0xff (no mask) 
                // Configure FrontFace depth/stencil operations   
                FrontFace = new DepthStencilOperationDescription()
                {
                    Comparison = Comparison.Always,
                    PassOperation = StencilOperation.Keep,
                    FailOperation = StencilOperation.Keep,
                    DepthFailOperation = StencilOperation.Increment
                },
                // Configure BackFace depth/stencil operations   
                BackFace = new DepthStencilOperationDescription()
                {
                    Comparison = Comparison.Always,
                    PassOperation = StencilOperation.Keep,
                    FailOperation = StencilOperation.Keep,
                    DepthFailOperation = StencilOperation.Decrement
                }
            };
            _drawer.DepthStencilDescripshion = DStateDescripshion;

            //Устанавливаем параметры растеризации так чтобы обратная сторона объекта не спряталась.
            RasterizerStateDescription rasterizerStateDescription = RasterizerStateDescription.Default();
            rasterizerStateDescription.CullMode = CullMode.None;
            rasterizerStateDescription.FillMode = FillMode.Solid;
            _drawer.RasterizerDescription = rasterizerStateDescription;

            //TODO: донастроить параметры блендинга для прозрачности.
            #region Формула бледнинга
            //(FC) - Final Color
            //(SP) - Source Pixel
            //(DP) - Destination Pixel
            //(SBF) - Source Blend Factor
            //(DBF) - Destination Blend Factor
            //(FA) - Final Alpha
            //(SA) - Source Alpha
            //(DA) - Destination Alpha
            //(+) - Binaray Operator described below
            //(X) - Cross Multiply Matrices
            //Формула для блендинга
            //(FC) = (SP)(X)(SBF)(+)(DP)(X)(DPF)
            //(FA) = (SA)(SBF)(+)(DA)(DBF)
            //ИСПОЛЬЗОВАНИЕ
            //_dx11DeviceContext.OutputMerger.SetBlendState(
            //    _blendState,
            //     new SharpDX.Mathematics.Interop.RawColor4(0.75f, 0.75f, 0.75f, 1f));
            //ЭТО ДЛЯ НЕПРОЗРАЧНЫХ _dx11DeviceContext.OutputMerger.SetBlendState(null, null);
            #endregion

            RenderTargetBlendDescription targetBlendDescription = new RenderTargetBlendDescription()
            {
                IsBlendEnabled = new SharpDX.Mathematics.Interop.RawBool(true),
                SourceBlend = BlendOption.SourceColor,
                DestinationBlend = BlendOption.BlendFactor,
                BlendOperation = BlendOperation.Add,
                SourceAlphaBlend = BlendOption.One,
                DestinationAlphaBlend = BlendOption.Zero,
                AlphaBlendOperation = BlendOperation.Add,
                RenderTargetWriteMask = ColorWriteMaskFlags.All
            };

            BlendStateDescription blendDescription = BlendStateDescription.Default();
            blendDescription.AlphaToCoverageEnable = new SharpDX.Mathematics.Interop.RawBool(false);
            blendDescription.RenderTarget[0] = targetBlendDescription;
            _drawer.BlendDescription = blendDescription;

            SharpDX.Mathematics.Interop.RawColor4 blenF = new SharpDX.Mathematics.Interop.RawColor4(0.3f, 0.3f, 0.3f, 0.3f);
            _drawer.BlendFactor = blenF;
        }

        public abstract void Update(double time);

        public abstract void Draw(Matrix _World, Matrix _View, Matrix _Progection);
        
        public virtual void Dispose()
        {
            Utilities.Dispose(ref _drawer);
            Utilities.Dispose(ref _viewModel);
            _drawer?.Dispose();
        }
    }
}
