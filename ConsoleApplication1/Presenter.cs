using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VictoremLibrary;
using System.Diagnostics;
using SharpDX.DirectInput;

namespace ConsoleApplication1
{
    class Presenter : IDisposable
    {
        private ParticleRenderer particleSystem;
        private int totalParticles;
        private Stopwatch simTime;
        private Matrix worldMatrix;
        private Matrix viewMatrix;
        private Matrix projectionMatrix;

        public Presenter(Game game)
        {
            game.OnDraw += Draw;
            game.OnUpdate += Upadate;
            game.OnKeyPressed += KeyKontroller;
            game.Color = Color.Black;
            // Initialize the matrix
             worldMatrix = Matrix.Identity;
             viewMatrix = Matrix.LookAtRH(new Vector3(0, 1, 50 ), Vector3.Zero, Vector3.UnitY);
            viewMatrix.TranslationVector += new Vector3(0, -0.98f, 0);
             projectionMatrix = Matrix.PerspectiveFovRH(MathUtil.PiOverFour, game.ViewRatio, 0.1f, 100f);

            particleSystem = new ParticleRenderer(game);
            particleSystem.UseLightenBlend = false;
            particleSystem.Object.WVP = worldMatrix * viewMatrix * projectionMatrix;
            particleSystem.Object.WVP.Transpose();
            particleSystem.Object.CP = Matrix.Transpose(Matrix.Invert(viewMatrix)).Column4;
        
            totalParticles = 100000;
            particleSystem.Constants.DomainBoundsMax = new Vector3(20, 20, 20);
            particleSystem.Constants.DomainBoundsMin = new Vector3(-20, 0, -20);
            particleSystem.Constants.ForceDirection = -Vector3.UnitY;
            particleSystem.Constants.ForceStrength = 1.8f;
            particleSystem.Constants.Radius = 0.1f;       
            particleSystem.InitializeParticles(totalParticles, 13f);
        
            simTime = new Stopwatch();
            simTime.Start();
           
        }

        private void KeyKontroller(float time, KeyboardState kState)
        {
        }

        private void Upadate(float time)
        {
        }

        private void Draw(float time)
        {
            // 1. Update the particle simulation
            if (simTime.IsRunning)
            {
                particleSystem.Frame.FrameTime = (float)simTime.Elapsed.TotalSeconds - particleSystem.Frame.Time;
                particleSystem.Frame.Time = (float)simTime.Elapsed.TotalSeconds;
                // Run the compute shaders (compiles if necessary)  
                particleSystem.Update("Generator", "Snowfall");
            }
            // 2. Render the particles 
            particleSystem.Render();
        }

        public void Dispose()
        {
            particleSystem?.Dispose();
        }
    }
}
