using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.DirectInput;

namespace VictoremLibrary
{
    public class Camera
    {
        #region Поля и Свойства

        Vector3 camPosition = new Vector3(0);
        Vector3 camTarget;
        Vector3 camUp;

        /// <summary>
        /// Направление куда смотрит наша камера.
        /// </summary>
        public Vector3 Normal { get { return camTarget; } }

        /// <summary>
        /// Положение камеры в базовой системе координат. ( центр системы 0,0,0)
        /// </summary>
        public Vector3 Position { get { return camPosition; } set { camPosition = value; } }

        public float moveLeftRight = 0;
        public float moveBackForward = 0;
        public float moveUpDown = 0;

        public float camYaw = 0;
        public float camPitch = 0;
        #endregion

        /// <summary>
        /// Возвращает матрицу камеры в левосторонне системе координат
        /// </summary>
        /// <returns>Матрива вида в Левосторонней системе координат (Z удаляеться от на)</returns>
        public Matrix GetLHView()
        {
            Vector3 DefaultForward = new Vector3(0, 0, 1f);
            Vector3 DefaultRight = new Vector3(1f, 0, 0);
            Vector3 DefaultcamUp = new Vector3(0, 1f, 0);
            Matrix camRotationMatrix;

            Matrix.RotationYawPitchRoll(camPitch, camYaw, 0, out camRotationMatrix);
            Vector3.Transform(ref DefaultForward, ref camRotationMatrix, out camTarget);
            camTarget = Vector3.Normalize(camTarget);

            Vector3 camForward;
            Vector3 camRight;
            Matrix RotateYTempMatrix = Matrix.RotationY(camPitch);
            Vector3.Transform(ref DefaultRight, ref RotateYTempMatrix, out camRight);
            Vector3.Transform(ref DefaultcamUp, ref RotateYTempMatrix, out camUp);
            Vector3.Transform(ref DefaultForward, ref RotateYTempMatrix, out camForward);

            camPosition += moveLeftRight * camRight;
            camPosition += moveBackForward * camForward;
            camPosition += moveUpDown * camUp;

            moveLeftRight = 0.0f;
            moveBackForward = 0.0f;
            moveUpDown = 0.0f;

            camTarget = camPosition + camTarget;
            return Matrix.LookAtLH(camPosition, camTarget, camUp);
        }

        /// <summary>
        /// Вычисляет направление и положение камеры в правосторонней системе координат
        /// </summary>
        /// <returns>Матрица вида в правосторонней системе координта</returns>
        public Matrix GetRHView()
        {
            Vector3 DefaultForward = new Vector3(0, 0, 1f);
            Vector3 DefaultRightRH = new Vector3(-1f, 0, 0);
            Vector3 DefaultcamUp = new Vector3(0, 1f, 0);
            Matrix camRotationMatrix;
            Matrix.RotationYawPitchRoll(camYaw,camPitch, 0, out camRotationMatrix);
            Vector3.Transform(ref DefaultForward, ref camRotationMatrix, out camTarget);
            camTarget = Vector3.Normalize(camTarget);

            Vector3 camForward;
            Vector3 camRight;
            Matrix RotateYTempMatrix = Matrix.RotationY(camPitch);
            Vector3.Transform(ref DefaultRightRH, ref RotateYTempMatrix, out camRight);
            Vector3.Transform(ref DefaultcamUp, ref RotateYTempMatrix, out camUp);
            Vector3.Transform(ref DefaultForward, ref RotateYTempMatrix, out camForward);

            camPosition += moveLeftRight * camRight;
            camPosition += moveBackForward * camForward;
            camPosition += moveUpDown * camUp;

            moveLeftRight = 0.0f;
            moveBackForward = 0.0f;
            moveUpDown = 0.0f;

            camTarget = camPosition + camTarget;
            return Matrix.LookAtRH(camPosition, camTarget, camUp);
        }


    }
}
