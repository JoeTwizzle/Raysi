using OpenTK.Mathematics;
using GLGraphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace Raysi
{
    public class Camera : ComponentData
    {
        public bool KeepAspect { get; set; }
        public bool IsPerspective { get; set; } = true;
        public int Priority { get; set; }
        public Matrix4 ViewMatrix { get { return Matrix4.LookAt(Transform.LocalPosition, Transform.LocalPosition + Transform.LocalForward, Transform.LocalUp); } }
        public Matrix4 PerspectiveMatrix { get { return ComputePerspective(); } }

        public Matrix4 OrthographicMatrix { get { return Matrix4.CreateOrthographicOffCenter(-64, 64, -64, 64, far, near); } }
        public Matrix4 ProjectionMatrix { get { return IsPerspective ? (PerspectiveMatrix * ViewMatrix) : (ViewMatrix * OrthographicMatrix); } }

        private TextureFormat colorFormat = TextureFormat.Rgba16f;
        private TextureFormat depthFormat = TextureFormat.Depth32fStencil8;

        private int msaa = 2;
        public int MSAA
        {
            get => msaa; set
            {
                msaa = Math.Max(value, 1);
            }
        }

        private float fov = 90;
        public float AspectRatio { get; set; } = 1;
        private float near;
        private float far;

        Matrix4 ComputePerspective()
        {
            return Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(FOV), AspectRatio, Near, Far);
            ////float f = 1.0f / MathF.Tan(MathHelper.DegreesToRadians(FOV) / 2.0f);
            //try
            //{
            //    float f = 1 / MathF.Tan(MathHelper.DegreesToRadians(FOV) / 2f);
            //    var result = new Matrix4(f / (viewport.Size.X / (float)viewport.Size.Y), 0, 0, 0,
            //        0, f, 0, 0,
            //        0, 0, 0, -1,
            //        0, 0, near, 0);
            //    //var result = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(FOV), RenderTexture.Width / (float)RenderTexture.Height, Near, Far);
            //    //result.Row2.X = 0;
            //    //result.Row2.Y = 0;
            //    //result.Row2.Z = (Near / (Far - Near));
            //    //result.Row3.Z = (Far * Near) / (Far - Near);
            //    return result;
            //}
            //catch
            //{
            //    return Matrix4.Identity;
            //}

        }

        Transform transform;
        public Transform Transform
        {
            get
            {
                if (GameObject == null)
                {
                    if (transform == null)
                    {
                        transform = Transform.Create();
                    }
                    return transform;
                }
                else
                {
                    return GameObject.Transform;
                }
            }
        }

        public float Near
        {
            get => near; set
            {
                if (value <= 0)
                {
                    return;
                }
                near = value;
            }
        }
        public float Far
        {
            get => far; set
            {
                if (value <= near)
                {
                    return;
                }
                far = value;
            }
        }
        public float FOV
        {
            get => fov; set
            {
                fov = MathHelper.Clamp(value, 0.1f, 179.9f);
            }
        }

        public TextureFormat ColorFormat
        {
            get => colorFormat; set
            {
                colorFormat = value;
            }
        }
        public TextureFormat DepthFormat
        {
            get => depthFormat; set
            {
                depthFormat = value;
            }
        }

        public Camera()
        {
            near = 0.06f;
            far = 10000;
            FOV = 90f;
        }
    }
}
