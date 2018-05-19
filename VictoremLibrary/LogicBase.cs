using System;
using System.Collections.Generic;
using SharpDX.DirectInput;
using SharpDX;

namespace VictoremLibrary
{
    public abstract class LogicBase : IDisposable
    {
        protected Game game;
        protected Matrix worldMatrix;
        protected Matrix viewMatrix;
        protected Matrix projectionMatrix;

        public LogicBase(Game game)
        {
            this.game = game;
            game.OnDraw += Draw;
            game.OnUpdate += Upadate;
            game.OnKeyPressed += KeyKontroller;
        }

        protected abstract void KeyKontroller(float time, KeyboardState kState);
        protected abstract void Upadate(float time);
        protected abstract void Draw(float time);
        public abstract void Dispose();
    }
}
