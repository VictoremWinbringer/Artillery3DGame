using System;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.Mathematics.Interop;
using SharpDX11GameByWinbringer.Models;


namespace SharpDX11GameByWinbringer
{
    sealed class Triangle : Component<ColoredVertex,Data>
    {
        public Triangle(DeviceContext dc)
        {
            World = Matrix.Identity;
            _dx11DeviceContext = dc;
            CreateVertexAndIndeces();
            CreateBuffers();
            CreateState();
        }

        public void DrawTriangle(PrimitiveTopology PrimitiveTopology, bool isBlending =false, RawColor4? BlendFactor = null)
        {
            PreDraw(PrimitiveTopology, isBlending, BlendFactor);
            Draw();
        }

        public override void UpdateConsBufData(Matrix world, Matrix view, Matrix proj)
        {
            _constantBufferData.WVP = World * world * view * proj;
            _constantBufferData.WVP.Transpose();
            _dx11DeviceContext.UpdateSubresource(ref _constantBufferData, _constantBuffer);
        }

        protected override void CreateState()
        {
            InputElement[] inputElements = new InputElement[]
          {
                new InputElement("POSITION", 0, SharpDX.DXGI.Format.R32G32B32_Float,0, 0),
                new InputElement("COLOR",0,SharpDX.DXGI.Format.R32G32B32A32_Float,12,0)
          };
            //Установка Сампрелар для текстуры.
            SamplerStateDescription description = SamplerStateDescription.Default();
            description.Filter = Filter.MinMagMipLinear;
            description.AddressU = TextureAddressMode.Wrap;
            description.AddressV = TextureAddressMode.Wrap;
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
            //Устанавливаем параметры растеризации так чтобы обратная сторона объекта не спряталась.
            RasterizerStateDescription rasterizerStateDescription = RasterizerStateDescription.Default();
            rasterizerStateDescription.CullMode = CullMode.None;
            rasterizerStateDescription.FillMode = FillMode.Solid;
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
                IsBlendEnabled = new RawBool(true),
                SourceBlend = BlendOption.SourceColor,
                DestinationBlend = BlendOption.BlendFactor,
                BlendOperation = BlendOperation.Add,
                SourceAlphaBlend = BlendOption.SourceAlpha,
                DestinationAlphaBlend = BlendOption.DestinationAlpha,
                AlphaBlendOperation = BlendOperation.Add,
                RenderTargetWriteMask = ColorWriteMaskFlags.All
            };
            BlendStateDescription blendDescription = BlendStateDescription.Default();
            blendDescription.AlphaToCoverageEnable = new RawBool(true);
            blendDescription.IndependentBlendEnable = new RawBool(true);
            //  blendDescription.RenderTarget[0] = targetBlendDescription;

            InitDrawer("Shaders\\ColoredVertex.hlsl",
               inputElements,
                "Textures\\grass.jpg",
                description,
                DStateDescripshion,
                rasterizerStateDescription,
                blendDescription
               );
        }

        protected override void CreateVertexAndIndeces()
        {
            _verteces = new ColoredVertex[]
            {
               new ColoredVertex(new Vector3(-100,0,0), new Vector4(1,0,0,0.5f)),
               new ColoredVertex(new Vector3(0,100,0), new Vector4(0,1,0,0.5f)),
               new ColoredVertex(new Vector3(100,0,0), new Vector4(0,0,1,0.5f))
           };
            _indeces = new uint[]
            {
                0,1,2
            };
        }
    }
}
