using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raysi
{
    public class FlyCamController : GameScript
    {
        public override void Init()
        {
            Transform = GameObject.Transform;
        }
        float angleX, angleY;
        Transform Transform;
        public override void PostUpdate()
        {
            float speedRot = 3f;
            float speed = 3f;
            if (Input.Keyboard.IsKeyDown(Keys.LeftControl))
            {
                speed = 8f;
            }
            var fwd = Transform.LocalForward;
            fwd = new Vector3(fwd.X, 0, fwd.Z).Normalized();
            if (Input.Keyboard.IsKeyDown(Keys.W))
            {
                Transform.LocalPosition -= fwd * speed * Input.DeltaTime;
            }
            if (Input.Keyboard.IsKeyDown(Keys.S))
            {
                Transform.LocalPosition += fwd * speed * Input.DeltaTime;
            }
            if (Input.Keyboard.IsKeyDown(Keys.A))
            {
                Transform.LocalPosition -= Transform.LocalRight * speed * Input.DeltaTime;
            }
            if (Input.Keyboard.IsKeyDown(Keys.D))
            {
                Transform.LocalPosition += Transform.LocalRight * speed * Input.DeltaTime;
            }
            if (Input.Keyboard.IsKeyDown(Keys.Space))
            {
                Transform.LocalPosition += new Vector3(0, Input.DeltaTime, 0) * speed;
            }
            if (Input.Keyboard.IsKeyDown(Keys.LeftShift))
            {
                Transform.LocalPosition -= new Vector3(0, Input.DeltaTime, 0) * speed;
            }

            if (Input.Keyboard.IsKeyDown(Keys.Left))
            {
                angleX += Input.DeltaTime * speedRot;
            }
            if (Input.Keyboard.IsKeyDown(Keys.Right))
            {
                angleX -= Input.DeltaTime * speedRot;
            }
            if (Input.Keyboard.IsKeyDown(Keys.Down))
            {
                angleY -= Input.DeltaTime * speedRot;
            }
            if (Input.Keyboard.IsKeyDown(Keys.Up))
            {
                angleY += Input.DeltaTime * speedRot;
            }
            if (Input.Keyboard.IsKeyDown(Keys.Enter))
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            Transform.Rotation = Quaternion.FromAxisAngle(Vector3.UnitY, angleX) * Quaternion.FromAxisAngle(Vector3.UnitX, angleY);
        }
    }
}
