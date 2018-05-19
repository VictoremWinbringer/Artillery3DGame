using SharpDX.Direct3D11;
using SharpDX.Mathematics.Interop;

using Format = SharpDX.DXGI.Format;

using SharpDX;
using SharpDX.Direct3D;

namespace SharpDX11GameByWinbringer.Models
{
    class ShadedCube : Component<VertexN, PerObject>
    {
        float _size = 60;
        Buffer _perFrameBuffer;
        Buffer _perMaterialBuffer;
        public Matrix oWorld=Matrix.Identity;
        public ShadedCube(DeviceContext DeviceContext)
        {
            World = Matrix.Translation(0,100,0);
            _dx11DeviceContext = DeviceContext;
            CreateVertexAndIndeces();
            CreateBuffers();
            _perFrameBuffer = new Buffer(_dx11DeviceContext.Device, Utilities.SizeOf<PerFrame>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            _perMaterialBuffer = new Buffer(_dx11DeviceContext.Device, Utilities.SizeOf<PerMaterial>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            CreateState();
        }

        public override void UpdateConsBufData(Matrix world, Matrix view, Matrix proj)
        {
            oWorld = World* world;
            // Extract camera position from view matrix 
            var camPosition = Matrix.Transpose(Matrix.Invert(view)).Column4;
            // Update the per frame constant buffer
            var perFrame = new PerFrame();
            perFrame.CameraPosition = new Vector3(camPosition.X, camPosition.Y, camPosition.Z);
            perFrame.Light.Color = Color.White;
            var lightDir = new Vector3(1f, -1f, 1f);// Vector3.Transform(new Vector3(1f, -1f, 1f), oWorld);
            perFrame.Light.Direction = new Vector3(lightDir.X, lightDir.Y, lightDir.Z);

            var perObject = new PerObject();
            perObject.World = oWorld;
            perObject.WorldInverseTranspose = Matrix.Transpose(Matrix.Invert(perObject.World));
            perObject.WorldViewProjection = perObject.World * view * proj;
            perObject.Transpose();

            var perMaterial = new PerMaterial();
            perMaterial.Ambient = new Color4(0.2f);
            perMaterial.Diffuse = new Color4(1f,1,1,1);
            perMaterial.Emissive = new Color4(0);
            perMaterial.HasTexture = 1;
            perMaterial.UVTransform = Matrix.Identity;
            //Только для блика
            perMaterial.Specular =new Color4(0.5f,0.5f,0.5f,0);
            perMaterial.SpecularPower = 10f;
          

            _dx11DeviceContext.UpdateSubresource(ref perMaterial, _perMaterialBuffer);
            _dx11DeviceContext.UpdateSubresource(ref perObject, _constantBuffer);
            _dx11DeviceContext.UpdateSubresource(ref perFrame, _perFrameBuffer);
        }

        public void Draw(PrimitiveTopology PrimitiveTopology, bool isBlending = false, RawColor4? BlendFactor = null)
        {
            PreDraw(PrimitiveTopology, isBlending, BlendFactor);
            _dx11DeviceContext.VertexShader.SetConstantBuffer(1, _perFrameBuffer);
            _dx11DeviceContext.VertexShader.SetConstantBuffer(2, _perMaterialBuffer);
            _dx11DeviceContext.PixelShader.SetConstantBuffer(1, _perFrameBuffer);
            _dx11DeviceContext.PixelShader.SetConstantBuffer(2, _perMaterialBuffer);
            Draw();
        }

        protected override void CreateState()
        {
            InputElement[] inputElements = new InputElement[]
         {
             new InputElement("SV_Position",0,Format.R32G32B32_Float,0,0),
             new InputElement("NORMAL", 0, Format.R32G32B32_Float, 12, 0),
             new InputElement("COLOR", 0, Format.R8G8B8A8_UNorm, 24, 0),
             new InputElement("TEXCOORD", 0, Format.R32G32_Float,28, 0)
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
                DepthWriteMask = DepthWriteMask.All,
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

            InitDrawer("Shaders\\ShadedCube.hlsl",
               inputElements,
                "Textures\\lava.jpg",
                description,
                DStateDescripshion,
                rasterizerStateDescription,
                blendDescription
               );
        }

        protected override void CreateVertexAndIndeces()
        {          
            _verteces = new[]
            {

                new VertexN(new Vector3(-_size,_size,_size),  new Vector2(0, 0)),
                new VertexN(new Vector3(_size,_size,_size),   new Vector2(1, 0)),
                new VertexN(new Vector3(_size,_size,-_size),  new Vector2(1, 1)),
                new VertexN(new Vector3(-_size,_size,-_size), new Vector2(0, 1)),

                new VertexN(new Vector3(-_size,-_size,_size), new Vector2(0, 0)),
                new VertexN(new Vector3(_size,-_size,_size),  new Vector2(1, 0)),
                new VertexN(new Vector3(_size,-_size,-_size), new Vector2(1, 1)),
                new VertexN(new Vector3(-_size,-_size,-_size),new Vector2(0, 1)),

                new VertexN(new Vector3(-_size,-_size,_size), new Vector2(0, 0)),
                new VertexN(new Vector3(-_size,_size,_size),  new Vector2(1, 0)),
                new VertexN(new Vector3(-_size,_size,-_size), new Vector2(1, 1)),
                new VertexN(new Vector3(-_size,-_size,-_size),new Vector2(0, 1)),

                new VertexN(new Vector3(_size,-_size,_size), new Vector2(0, 0)),
                new VertexN(new Vector3(_size,_size,_size),  new Vector2(1, 0)),
                new VertexN(new Vector3(_size,_size,-_size), new Vector2(1, 1)),
                new VertexN(new Vector3(_size,-_size,-_size),new Vector2(0, 1)),

                new VertexN(new Vector3(-_size,_size,_size), new Vector2(0, 0)),
                new VertexN(new Vector3(_size,_size,_size),  new Vector2(1, 0)),
                new VertexN(new Vector3(_size,-_size,_size), new Vector2(1, 1)),
                new VertexN(new Vector3(-_size,-_size,_size),new Vector2(0, 1)),

                new VertexN(new Vector3(-_size,_size,-_size), new Vector2(0, 0)),
                new VertexN(new Vector3(_size,_size,-_size),  new Vector2(1, 0)),
                new VertexN(new Vector3(_size,-_size,-_size), new Vector2(1, 1)),
                new VertexN(new Vector3(-_size,-_size,-_size),new Vector2(0, 1)),
            };

            _indeces = new uint[]
            {
                0,1,2,0,2,3,
                4,6,5,4,7,6,
                8,9,10,8,10,11,
                12,14,13,12,15,14,
                16,18,17,16,19,18,
                20,21,22,20,22,23
            };
        }

        public sealed override void Dispose()
        {
            base.Dispose();
            Utilities.Dispose(ref _perFrameBuffer);
            Utilities.Dispose(ref _perMaterialBuffer);
        }

    }  
}

 