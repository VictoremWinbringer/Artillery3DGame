using SharpDX;
using System;
using VictoremLibrary;
using SharpDX.DirectInput;
using TeximpNet.Compression;
using TeximpNet;
using SharpDX.Direct3D11;
using SharpDX.WIC;
using System.Collections.Generic;

namespace FramevorkTest
{


    class Presenter : IDisposable
    {
        //TextWirter Drawer2d;
        //Bitmap bitmap;
       
        private Assimp3DModel model;
        Game game;
        public Presenter(Game game)
        {
           // var dt = Pfim.Pfim.FromFile("Wm\\CubeMap.dds");
           //var dd= Pfim.Dds.Create(new SharpDX.IO.NativeFileStream("Wm\\CubeMap.dds", SharpDX.IO.NativeFileMode.Open, SharpDX.IO.NativeFileAccess.Read));
           
           // var ddg = new CSharpImageLibrary.General.ImageEngineImage("Wm\\CubeMap.dds");
            //var sf =Surface.LoadFromFile("Wm\\CubeMap.dds",5);
            ////Since we're displaying this to a form, we're using the compressor to generate mipmaps but outputting the data into BGRA format.
            //Compressor compressor = new Compressor();
            //compressor.Input.GenerateMipmaps = true;
            //compressor.Input.SetData(sf);
            //compressor.Compression.Format = CompressionFormat.BGRA;
            //compressor.Compression.SetBGRAPixelFormat(); //If want the output images in RGBA ordering, you get set the pixel layout differently

            //List<CompressedImageData> mips = new List<CompressedImageData>();
            //if (!compressor.Process(mips))
            //    throw new ArgumentException("Unable to process image.");
            
            game.OnDraw += Draw;
            game.OnUpdate += Upadate;
            game.OnKeyPressed += KeyKontroller;
            //var srcTextureSRV = StaticMetods.LoadTextureFromFile(game.DeviceContext, "Village.png");
            //var b = StaticMetods.LoadBytesFormFile(game.DeviceContext, "Village.png");
            //var intt = game.FilterFoTexture.Histogram(srcTextureSRV);
            //game.FilterFoTexture.SobelEdgeColor(ref srcTextureSRV, 0.5f);
            //game.FilterFoTexture.Sepia(ref srcTextureSRV, 0.5f);
            //game.FilterFoTexture.Contrast(ref srcTextureSRV, 2f);
            //Drawer2d = new TextWirter(game.SwapChain.GetBackBuffer<Texture2D>(0), 800, 600);
            //bitmap = StaticMetods.GetBitmapFromSRV(srcTextureSRV, Drawer2d.RenderTarget);
            //Drawer2d.SetTextColor(Color.Red);
            //Drawer2d.SetTextSize(36);
            model = new Assimp3DModel(game, "lara.obj", "Wm\\");
            model._world = Matrix.Scaling(20);// Matrix.RotationX(MathUtil.PiOverTwo);
        }

        private void KeyKontroller(float time, KeyboardState kState)
        {

        }

        private void Upadate(float time)
        {
            model.Update(time, true);
        }


        private void Draw(float time)
        {
            model.Draw(game.DeviceContext);

            //Drawer2d.DrawBitmap(bitmap);
            //Drawer2d.DrawText("ПОЕХАЛИ!");

        }

        public void Dispose()
        {
            //Drawer2d?.Dispose();
            //bitmap?.Dispose();
            model?.Dispose();
        }
    }
}
