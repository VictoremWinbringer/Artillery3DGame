using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DirectInput;
using SharpDX11GameByWinbringer.Models;
using System;
using System.Diagnostics;
using Camera = VictoremLibrary.Camera;
namespace SharpDX11GameByWinbringer
{
    public struct Accelerator
    {
        public float Speed;
        public Vector3 Direction;
        public Vector3 Acceleration;
        public Vector3 VBaseSpeed;
        public void SetDefaultBaseSpeed()
        {
            VBaseSpeed = Direction * Speed;
        }
        public Matrix GetTranslation(float time)
        {
            time /= 1000;
            VBaseSpeed = VBaseSpeed + Acceleration * time;
            var S = VBaseSpeed * time;
            return Matrix.Translation(S);
        }
    }
    /// <summary>
    /// Наш презентер. Отвечает за работу с моделями и расчеты.
    /// </summary>
    public sealed class Presenter : IDisposable
    {
        Vector2 na = new Vector2(0,100);
        Vector2 ua= new Vector2(100,0);
        Game _game;
        Camera _camera;
        Matrix _View;
        Matrix _Progection;
        Matrix _View1;
        Accelerator wd = new Accelerator();
        Accelerator cd = new Accelerator();
       // _3DLineMaganer _lineManager;
        TextWirter _text2DWriter;
       _3DWaveManager _waveManager;
        ShadedCube _sCube;
        EarthFromOBJ _earth;
        string _s;
        Stopwatch _sw;
        // Tesselation _ts;
        //Triangle _triangle;
        //   MD5Model _boy;
        public Presenter(Game game)
        {
            _game = game;
            _camera = new Camera();
            _camera.Position = new Vector3(0, 200, -5500f);
            _text2DWriter = new TextWirter(
                game.SwapChain.GetBackBuffer<Texture2D>(0),
                game.Width,
                game.Height);
            game.OnDraw += Draw;
            game.OnUpdate += Update;
            game.OnKeyPressed += ReadKeyboardState;
            _View = Matrix.LookAtLH(new Vector3(0, 200, -5500f), new Vector3(0, 0, 0), Vector3.Up);
            _Progection = Matrix.PerspectiveFovLH(MathUtil.PiOverFour, game.ViewRatio, 1f, 20000f);
            _View1 = Matrix.LookAtLH(new Vector3(0, 1000f, -1f), new Vector3(0, 0, 0), Vector3.Up);
          //  _lineManager = new _3DLineMaganer(game.DeviceContext);
            _waveManager = new _3DWaveManager(game.DeviceContext);
            _sCube = new ShadedCube(game.DeviceContext);
            _sw = new Stopwatch();
            _sw.Start();
            _earth = new EarthFromOBJ(game.DeviceContext);
            _waveManager.World = Matrix.Translation(new Vector3(-250, -4, -250)) * Matrix.Scaling(2f);
            //_triangle = new Triangle(game.DeviceContext);
            //_sCube.World = Matrix.Translation(0, -70, 0);
            //_triangle.World = Matrix.RotationY(MathUtil.PiOverFour) * Matrix.Translation(0, 70, 0); 
            //     _boy = new MD5Model(game.DeviceContext, "3DModelsFiles\\Wm\\", "Female", "Shaders\\Boy.hlsl", false, 3, true);
            //            _boy.World = Matrix.Scaling(2);
            // _ts = new Tesselation(game.DeviceContext.Device,6);
            wd.Speed = 500f;
            wd.Direction = new Vector3(0, 0, 1);
            wd.Acceleration = new Vector3(-1, -50f, -1);
            wd.SetDefaultBaseSpeed();
        }

        void Update(double time)
        {
            LPS();
           // _lineManager.Update(time);
            _waveManager.Update(time);
            _earth.Update((float)time, wd.GetTranslation((float)time));
            var pos = Vector3.Transform(Vector3.Zero, _earth.World);
            var pos1 = Vector3.Transform(Vector3.Zero, _sCube.oWorld);
            if((pos - pos1).Length() < 120)
            {
                ++hits;
                _earth.isFaced = true;
                wd.SetDefaultBaseSpeed();
                Random rnd = new Random();
                _sCube.World = Matrix.Translation(rnd.Next(-500, 500), 60, rnd.Next(-500, 500));
            }
            if (pos.Y < 0)
            {
                _earth.isFaced = true;
                wd.SetDefaultBaseSpeed();
            }
            //    _boy.Update((float)time);
            //_triangle.UpdateConsBufData(_World, _View, _Progection);
        }

        private void LPS()
        {
            _sw.Stop();
            _s = string.Format("LPS : {0:#####}", 1000.0f / _sw.Elapsed.TotalMilliseconds);
            _sw.Reset();
            _sw.Start();
        }

        void Draw(double time)
        {
            _game.DeviceContext.Rasterizer.SetViewport(0, 0, _game.Width, _game.Height);
            _waveManager.Draw(Matrix.Identity, _View, _Progection);
           // _lineManager.Draw(Matrix.Identity, _View, _Progection);
            _sCube.UpdateConsBufData(Matrix.Identity, _View, _Progection);
            _sCube.Draw(SharpDX.Direct3D.PrimitiveTopology.TriangleList, true,
                      new SharpDX.Mathematics.Interop.RawColor4(0.1f, 0.1f, 0.1f, 0.1f));
            _earth.Draw(Matrix.Identity, _View, _Progection, 1f, 32);

            _game.DeviceContext.Rasterizer.SetViewport(0, 0, _game.Width / 4, _game.Height / 4);
            _waveManager.Draw(Matrix.Identity, _View1, _Progection);
         //   _lineManager.Draw(Matrix.Identity, _View1, _Progection);
            _earth.Draw(Matrix.Identity, _View1, _Progection, 1f, 32);
            _sCube.UpdateConsBufData(Matrix.Identity, _View1, _Progection);
            _sCube.Draw(SharpDX.Direct3D.PrimitiveTopology.TriangleList, true,
                      new SharpDX.Mathematics.Interop.RawColor4(0.1f, 0.1f, 0.1f, 0.1f));
          //  _text2DWriter.DrawText(_s);           
            _text2DWriter.Text($" ПОПАДАНИЙ: {hits}", 300,0);
            _text2DWriter.Text($"Скорость планеты: {wd.Speed}", 100, 400, 200, 500);
            _text2DWriter.Text("Направление атаки", 100, 300, 200, 400);
           _text2DWriter.DrawLine(100, 300, na.X + 100, -na.Y + 300);
            _text2DWriter.Text("Угол атаки", 100, 500, 200, 600);
            _text2DWriter.DrawLine(100, 500, ua.X  + 100, -ua.Y+ 500);

            //_triangle.DrawTriangle(SharpDX.Direct3D.PrimitiveTopology.TriangleList,
            //                        true,
            //                        new SharpDX.Mathematics.Interop.RawColor4(0.1f, 0.1f, 0.1f, 0.1f));

            //_sCube.Draw(SharpDX.Direct3D.PrimitiveTopology.TriangleList, true,
            //          new SharpDX.Mathematics.Interop.RawColor4(0.1f, 0.1f, 0.1f, 0.1f));

            //  _boy.Draw(_World, _View1, _Progection, SharpDX.Direct3D.PrimitiveTopology.TriangleList);
            //  _ts.Draw(_World, _View, _Progection);
            //_triangle.DrawTriangle(SharpDX.Direct3D.PrimitiveTopology.TriangleList,
            //                        true,
            //                        new SharpDX.Mathematics.Interop.RawColor4(0.1f, 0.1f, 0.1f, 0.1f));

            //  _boy.Draw(_World, _View, _Progection, SharpDX.Direct3D.PrimitiveTopology.TriangleList);
            //  _ts.Draw(_World, _View, _Progection);

        }

        #region Вспомогательные методы

        private void ReadKeyboardState(KeyboardState KeyState, float time)
        {
            float speed = 1.5f * time;
            float rSpeed = 0.001f * time;
            if (KeyState.IsPressed(Key.A))
            {
                _camera.moveLeftRight -= speed;
            }
            if (KeyState.IsPressed(Key.D))
            {
                _camera.moveLeftRight += speed;
            }
            if (KeyState.IsPressed(Key.W))
            {
                _camera.moveBackForward += speed;
            }
            if (KeyState.IsPressed(Key.S))
            {
                _camera.moveBackForward -= speed;
            }
            if (KeyState.IsPressed(Key.Up))
            {
                if (_camera.camYaw > -1f) _camera.camYaw -= rSpeed;
            }
            if (KeyState.IsPressed(Key.Down))
            {
                if (_camera.camYaw < 0) _camera.camYaw += rSpeed;
            }
            if (KeyState.IsPressed(Key.Right))
            {
                _camera.camPitch += rSpeed;

            }
            if (KeyState.IsPressed(Key.Left))
            {
                _camera.camPitch -= rSpeed;

            }
            if (KeyState.IsPressed(Key.Z))
            {
                _camera.moveUpDown += speed;
            }
            if (KeyState.IsPressed(Key.X))
            {
                _camera.moveUpDown -= speed;

            }

            _View = _camera.GetLHView();
            if (KeyState.IsPressed(Key.Space)) { _earth.Moving = true; _earth.isFaced = false; }
            if (KeyState.IsPressed(Key.U))
            {
                _earth.isFaced = true;
                wd.SetDefaultBaseSpeed();
                Random rnd = new Random();
            }
            if (!_earth.Moving)
            {
                float sp = rSpeed / 10;
                if (KeyState.IsPressed(Key.D1))
                {
                    wd.Direction = Vector3.Transform(wd.Direction, Matrix3x3.RotationY(-sp));
                    wd.SetDefaultBaseSpeed();
                    var q = Quaternion.RotationAxis(new Vector3(0, 0, 1), sp);
                    na = Vector2.Transform(na, q);
                }
                if (KeyState.IsPressed(Key.D2))
                {
                    wd.Direction = Vector3.Transform(wd.Direction, Matrix3x3.RotationY(sp));
                    wd.SetDefaultBaseSpeed();
                    var q = Quaternion.RotationAxis(new Vector3(0, 0, 1), -sp);
                    na = Vector2.Transform(na, q);
                }
                if (KeyState.IsPressed(Key.D3))
                {
                    wd.Direction = Vector3.Transform(wd.Direction, Matrix3x3.RotationX(sp));
                    wd.SetDefaultBaseSpeed();
                    var q = Quaternion.RotationAxis(new Vector3(0, 0, 1), -sp);
                    ua = Vector2.Transform(ua, q);
                }
                if (KeyState.IsPressed(Key.D4))
                {
                    wd.Direction = Vector3.Transform(wd.Direction, Matrix3x3.RotationX(-sp));
                    wd.SetDefaultBaseSpeed();
                    var q = Quaternion.RotationAxis(new Vector3(0, 0, 1), sp);
                    ua = Vector2.Transform(ua, q);
                }
                if (KeyState.IsPressed(Key.D6))
                {
                    wd.Speed +=rSpeed*100 ;
                    if (wd.Speed > 1000) wd.Speed = 1000;
                    wd.SetDefaultBaseSpeed();
                }
                if (KeyState.IsPressed(Key.D5))
                {
                    wd.Speed -= rSpeed*100;
                    if (wd.Speed < 1) wd.Speed = 1;
                    wd.SetDefaultBaseSpeed();
                }
            }
        }

        #endregion

        #region IDisposable Support

        private bool disposedValue = false;
        private int hits;

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: освободить управляемое состояние (управляемые объекты). 
                    Utilities.Dispose(ref _earth);
                 //   Utilities.Dispose(ref _lineManager);
                    Utilities.Dispose(ref _waveManager);
                    Utilities.Dispose(ref _text2DWriter);
                    Utilities.Dispose(ref _sCube);
                    //Utilities.Dispose(ref _triangle);
                    //    Utilities.Dispose(ref _boy);
                    // Utilities.Dispose(ref _ts);
                }
                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

    }
}
