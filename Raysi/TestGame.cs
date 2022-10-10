using GLGraphics;
using GLGraphics.Helpers;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Raysi.Structures;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Vector3 = System.Numerics.Vector3;
namespace Raysi
{
    partial class TestGame : GameWindow
    {

        public TestGame(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) : base(gameWindowSettings, nativeWindowSettings)
        {
        }

        static int NextPowerOfTwo(int x)
        {
            x--;
            x |= x >> 1; // handle 2 bit numbers
            x |= x >> 2; // handle 4 bit numbers
            x |= x >> 4; // handle 8 bit numbers
            x |= x >> 8; // handle 16 bit numbers
            x |= x >> 16; // handle 32 bit numbers
            x++;
            return x;
        }

        public static string ReadResourceString(string name)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            using Stream? stream = assembly.GetManifestResourceStream(name);
            if (stream == null)
            {
                throw new FileNotFoundException(name);
            }
            else
            {
                using var s = new StreamReader(stream);
                return s.ReadToEnd();
            }
        }
        Vector3i workGroups;
        VertexArray vertexArray;
        GLProgram RayProgram;
        GLProgram DisplayProgram;
        Texture2D colortex;
        Texture2D depthtex;
        Texture2D triTexture;
        Texture2D blueNoise;
        Texture2D skyTexture;
        FrameBuffer frameBuffer;
        Camera cam;
        Transform TriangleTransform;
        FlyCamController flyCamController;
        GameObject CameraHolder;
        GLBuffer vbo;
        GLBuffer ebo;
        GLBuffer BVHNodeBuffer;
        GLBuffer BVHIndexBuffer;

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            Vector2i size = new Vector2i(ClientSize.X / 2, ClientSize.Y /2);
            cam.AspectRatio = (size.X / (float)size.Y);
            //Create color texture
            if (colortex != null)
            {
                colortex.Dispose();
            }
            colortex = new Texture2D();
            colortex.Init(size.X, size.Y, TextureFormat.Rgba16f);
            colortex.Filter = TextureFilter.Bilinear;
            colortex.WrapModeU = TextureWrapMode.ClampToEdge;
            colortex.WrapModeV = TextureWrapMode.ClampToEdge;
            //Create depth texture
            if (depthtex != null)
            {
                depthtex.Dispose();
            }
            depthtex = new Texture2D();
            depthtex.Init(size.X, size.Y, TextureFormat.R32f);
            depthtex.Filter = TextureFilter.Nearest;
            depthtex.WrapModeU = TextureWrapMode.ClampToEdge;
            depthtex.WrapModeV = TextureWrapMode.ClampToEdge;
            //Create framebuffer
            if (frameBuffer != null)
            {
                frameBuffer.DetachTexture(FramebufferAttachment.ColorAttachment0);
                frameBuffer.DetachTexture(FramebufferAttachment.ColorAttachment1);
                frameBuffer.Dispose();
            }
            frameBuffer = new FrameBuffer();
            frameBuffer.AttachTexture(colortex, FramebufferAttachment.ColorAttachment0);
            frameBuffer.AttachTexture(depthtex, FramebufferAttachment.ColorAttachment1);
            Console.WriteLine(GL.CheckNamedFramebufferStatus(frameBuffer.Handle, FramebufferTarget.Framebuffer));
            OnUpdateFrame(new FrameEventArgs(0d));
            OnRenderFrame(new FrameEventArgs(0d));
        }

        protected unsafe override void OnLoad()
        {
            CultureInfo ci = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentCulture = ci;
            Thread.CurrentThread.CurrentUICulture = ci;

            base.OnLoad();
            CenterWindow();
            _debugProcCallbackHandle = GCHandle.Alloc(_debugProcCallback);
            GL.Enable(EnableCap.DebugOutput);
            GL.Enable(EnableCap.DebugOutputSynchronous);
            GL.DebugMessageCallback(_debugProcCallback, IntPtr.Zero);
            GLShader RayShader = new GLShader(ShaderType.ComputeShader);
            RayShader.SetSource(ReadResourceString("Raysi.Shaders.BasicTriangle.glsl"));
            GLTFAssetLoader.Load(@"D:\Blender\Models\SunTemple.glb");
            CameraHolder = new GameObject();
            cam = new Camera();
            flyCamController = new FlyCamController();
            flyCamController.Enabled = true;
            CameraHolder.AddScript(flyCamController);
            CameraHolder.AddComponent(cam);
            flyCamController.Init();
            TriangleTransform = Transform.Create();
            //TriangleTransform.Position = new Vector3(0, 0, -3);
            blueNoise = TextureLoader.LoadTexture2D(@"D:\Blender\Materials\BlueNoise\512_512\LDR_RGB1_0.png", false);
            triTexture = TextureLoader.LoadTexture2D(@"D:\Blender\Materials\PavingStones#18\PavingStones18_col.jpg", true);
            skyTexture = TextureLoader.LoadTexture2D(@"D:\Blender\HDRI\kloppenheim_02.jpg", true);
            //Create the ray shader
            //Create ray Program
            RayProgram = new GLProgram();
            RayProgram.AddShader(RayShader);
            RayShader.Dispose();
            RayProgram.LinkProgram();

            //Create vertexbuffer
            //vbo = new GLBuffer();
            //vbo.Init(BufferType.ShaderStorageBuffer, vertices);
            //ebo = new GLBuffer();
            //ebo.Init(BufferType.ShaderStorageBuffer, indices);
            //Create the display shader
            using GLShader displayShaderV = new GLShader(ShaderType.VertexShader);
            displayShaderV.SetSource(ReadResourceString("Raysi.Shaders.SimpleVert.glsl"));
            using GLShader displayShaderF = new GLShader(ShaderType.FragmentShader);
            displayShaderF.SetSource(ReadResourceString("Raysi.Shaders.SimpleFrag.glsl"));
            //Create display Program
            DisplayProgram = new GLProgram();
            DisplayProgram.AddShader(displayShaderV);
            DisplayProgram.AddShader(displayShaderF);
            DisplayProgram.LinkProgram();
            GL.GetProgram(RayProgram.Handle, (GetProgramParameterName)All.ComputeWorkGroupSize, out workGroups.X);
            vertexArray = new VertexArray();
            fence = new GLFence();

            var mb = RenderCore.MeshBuffers[(int)value];
            uint[] indices = new uint[mb.IndexBuffer.ElementCount];
            GL.GetNamedBufferSubData(mb.IndexBuffer.Handle, IntPtr.Zero, mb.IndexBuffer.Size, indices);
            Vertex[] vertices = new Vertex[mb.VertexBuffer.ElementCount];
            GL.GetNamedBufferSubData(mb.VertexBuffer.Handle, IntPtr.Zero, mb.VertexBuffer.Size, vertices);
            int triCount = indices.Length / 3;
            BBox[] bboxes = new BBox[triCount];
            Vector3[] centers = new Vector3[triCount];

            Stopwatch sw = new Stopwatch();
            using BinaryWriter writer = new BinaryWriter(new FileStream("dust.tri", FileMode.Create));
            writer.Write(triCount);
            for (int i = 0; i < triCount; ++i)
            {
                Vertex v0 = vertices[indices[i * 3 + 0]];
                Vertex v1 = vertices[indices[i * 3 + 1]];
                Vertex v2 = vertices[indices[i * 3 + 2]];
                var bbox = BBox.Empty;
                bbox.Extend(v0.Pos);
                bbox.Extend(v1.Pos);
                bbox.Extend(v2.Pos);
                bboxes[i] = bbox;
                centers[i] = (v0.Pos + v1.Pos + v2.Pos) * (1.0f / 3.0f);
                writer.Write(v0.Pos.X);
                writer.Write(v0.Pos.Y);
                writer.Write(v0.Pos.Z);
                writer.Write(v1.Pos.X);
                writer.Write(v1.Pos.Y);
                writer.Write(v1.Pos.Z);
                writer.Write(v2.Pos.X);
                writer.Write(v2.Pos.Y);
                writer.Write(v2.Pos.Z);
            }
            writer.Close();
            Vector3 eye = new(0, 2, -3);
            Vector3 dir = new Vector3(0, 0, -1);
            dir = Vector3.Normalize(dir);
            Vector3 up = new(0, 1, 0);
            dir = Vector3.Normalize(dir);
            var right = Vector3.Normalize(Vector3.Cross(dir, up));
            up = Vector3.Cross(right, dir);
            sw.Start();
            var bvh = BVHBuilder.Build(bboxes, centers, triCount);
            sw.Stop();
            bvh.Save("../../../../dust.bvh");

            BVHNodeBuffer = new GLBuffer();
            BVHNodeBuffer.Init<Node>(BufferType.ShaderStorageBuffer, bvh.Nodes.Length);
            BVHNodeBuffer.UpdateData(bvh.Nodes);

            BVHIndexBuffer = new GLBuffer();
            BVHIndexBuffer.Init<int>(BufferType.ShaderStorageBuffer, bvh.PrimIndices.Length);
            BVHIndexBuffer.UpdateData(bvh.PrimIndices);

            Console.WriteLine(sw.ElapsedMilliseconds);
            //Console.WriteLine("Loaded file with " + triCount + " triangles");
            //var bvh = BVH.Load(@"F:\Projekte\C++\BVH\dust.bvh");
            Console.WriteLine("Done");
            Console.WriteLine("Built BVH with " + bvh.Nodes.Length + " node(s), depth " + bvh.GetDepth());
            int intersections = 0;
            int groupsX = 8;
            int groupsY = 8;
            int w = 1024 * 2;
            int h = 1024 * 2;
            const int bounces = 2;
            int width = NextPowerOfTwo(w) / groupsX;
            int height = NextPowerOfTwo(h) / groupsY;
            var image = new byte[w * h * 3];
            sw.Restart();
            //Parallel.For(0, height, y =>
            ////for (int y = 0; y < height; ++y)
            //{
            //    //for (int x = 0; x < width; ++x)
            //    Parallel.For(0, width, x =>
            //    {
            //        for (int localy = 0; localy < groupsY; localy++)
            //        {
            //            int finaly = y * groupsY + localy;
            //            for (int localx = 0; localx < groupsX; localx++)
            //            {
            //                int finalx = x * groupsX + localx;
            //                if (finalx >= w || finaly >= h)
            //                {
            //                    continue;
            //                }
            //                var u = 2.0f * finalx / w - 1.0f;
            //                var v = 2.0f * finaly / h - 1.0f;
            //                Ray ray;
            //                ray.org = eye;
            //                ray.dir = (dir + u * right + v * up);
            //                ray.tmin = 0;
            //                ray.tmax = float.MaxValue;
            //                //var hit = bvh.TraverseAll(ref ray, vertices, indices);
            //                //if (hit.PrimIndex != -1)
            //                //{
            //                //    intersections++;
            //                //    var pixel = 3 * (finaly * w + finalx);
            //                //    image[pixel + 0] = (byte)(hit.PrimIndex * 51);
            //                //    image[pixel + 1] = (byte)(hit.PrimIndex * 91);
            //                //    image[pixel + 2] = (byte)(hit.PrimIndex * 37);
            //                //}
            //                //else
            //                //{
            //                //    var pixel = 3 * (finaly * w + finalx);
            //                //    image[pixel + 0] = 51;
            //                //    image[pixel + 1] = 255;
            //                //    image[pixel + 2] = 37;
            //                //}
            //                Vector3 color = new Vector3();
            //                for (int bounce = 0; bounce < bounces + 1; bounce++)
            //                {
            //                    float strength = (1f / (bounce * 2.5f + 1));
            //                    var hit = bvh.TraverseAll(ref ray, vertices, indices);
            //                    if (hit.PrimIndex != -1)
            //                    {
            //                        intersections++;
            //                        float brightness = (Vector3.Dot(hit.Normal, Vector3.UnitY) + 1) * 0.5f;
            //                        ray.org = hit.Position + hit.Normal * 0.001f;
            //                        ray.dir = Vector3.Normalize(ray.dir - 2f * Vector3.Dot(ray.dir, hit.Normal) * hit.Normal);
            //                        ray.tmin = 0;
            //                        ray.tmax = float.MaxValue;
            //                        Ray ShadowRay = new Ray();
            //                        ShadowRay.org = hit.Position + hit.Normal * 0.001f;
            //                        ShadowRay.dir = Vector3.UnitY;
            //                        ShadowRay.tmin = 0;
            //                        ShadowRay.tmax = float.MaxValue;
            //                        brightness *= bvh.TraverseAny(ShadowRay, vertices, indices) ? 0.5f : 1;
            //                        color += new Vector3(brightness) * strength;
            //                    }
            //                    else
            //                    {
            //                        color += new Vector3(66f / 255f, 135f / 255f, 245f / 255f) * strength;
            //                    }
            //                }
            //                int pixel = 3 * (finaly * w + finalx);
            //                image[pixel + 0] = (byte)(Tonemap_ACES(color.Z) * 255);//B
            //                image[pixel + 1] = (byte)(Tonemap_ACES(color.Y) * 255);//G
            //                image[pixel + 2] = (byte)(Tonemap_ACES(color.X) * 255);//R
            //            }
            //        }
            //    });
            //    //if (y % (height / 10) == 0)
            //    //    Console.Write(".");
            //});
            //sw.Stop();
            //Console.WriteLine(sw.ElapsedMilliseconds);
            //Console.WriteLine(intersections + " intersection(s) found");
            //fixed (byte* ptr = image)
            //{
            //    using var bmp = new Bitmap(w, h, 3 * w, System.Drawing.Imaging.PixelFormat.Format24bppRgb, new IntPtr(ptr));
            //    bmp.Save(@"newImage.jpg");
            //}
            //Console.ReadLine();
            //BVHBuffer = new GLBuffer();
            //BVHBuilder.build(RenderCore.MeshBuffers[(int)value]);
            //var data = BVHBuilder.m_packedNodes.ToArray();
            //BVHBuffer.Init(BufferType.ShaderStorageBuffer, data);
            //BVHBuffer.UpdateData(data);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        static float Tonemap_ACES(float x)
        {
            // Narkowicz 2015, "ACES Filmic Tone Mapping Curve"
            const float a = 2.51f;
            const float b = 0.03f;
            const float c = 2.43f;
            const float d = 0.59f;
            const float e = 0.14f;
            return (x * (a * x + b)) / (x * (c * x + d) + e);
        }
        GLFence fence;

        float time;
        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);
            Input.DeltaTime = (float)args.Time;
            time += Input.DeltaTime;
            Input.Keyboard = KeyboardState;
            Input.Mouse = MouseState;
            if (KeyboardState.IsKeyDown(Keys.KeyPadAdd))
            {
                value += (float)args.Time;
            }
            if (KeyboardState.IsKeyDown(Keys.KeyPadSubtract))
            {
                value -= (float)args.Time;
            }
            value %= RenderCore.MeshBuffers.Count;
            if (value < 0)
            {
                value = RenderCore.MeshBuffers.Count - 1;
            }
            
            //if (KeyboardState.IsKeyDown(Keys.Enter) && !KeyboardState.WasKeyDown(Keys.Enter))
            //{
            //    if (BVHBuffer != null)
            //    {
            //        BVHBuffer.Dispose();
            //    }

            //    BVHBuffer = new GLBuffer();
            //    //BVHBuilder.build(RenderCore.MeshBuffers[(int)value]);
            //    //var data = BVHBuilder.m_packedNodes.ToArray();
            //    //using (var fs = File.Create("bvh.bin"))
            //    //{
            //    //    using (BinaryWriter w = new BinaryWriter(fs))
            //    //    {
            //    //        for (int i = 0; i < data.Length; i++)
            //    //        {
            //    //            w.Write(data[i].a);
            //    //            w.Write(data[i].b);
            //    //            w.Write(data[i].c);
            //    //            w.Write(data[i].d);
            //    //        }
            //    //    }
            //    //}
            //    uint[] indices = new uint[RenderCore.MeshBuffers[(int)value].IndexBuffer.ElementCount];
            //    GL.GetNamedBufferSubData(RenderCore.MeshBuffers[(int)value].IndexBuffer.Handle, IntPtr.Zero, RenderCore.MeshBuffers[(int)value].IndexBuffer.Size, indices);
            //    Vertex[] vertices = new Vertex[RenderCore.MeshBuffers[(int)value].VertexBuffer.ElementCount];
            //    GL.GetNamedBufferSubData(RenderCore.MeshBuffers[(int)value].VertexBuffer.Handle, IntPtr.Zero, RenderCore.MeshBuffers[(int)value].VertexBuffer.Size, vertices);
            //    using (var fs = File.Create("idx.bin"))
            //    {
            //        using (BinaryWriter w = new BinaryWriter(fs))
            //        {
            //            for (int i = 0; i < indices.Length; i++)
            //            {
            //                w.Write(indices[i]);
            //            }
            //        }
            //    }
            //    BVHBuffer.Init(BufferType.ShaderStorageBuffer, data);
            //    BVHBuffer.UpdateData(data);
            //}
            flyCamController.Update();
            flyCamController.PostUpdate();
        }
        float value = 0;
        protected override void OnRenderFrame(FrameEventArgs args)
        {
            Title = "" + 1 / args.Time;
            base.OnRenderFrame(args);

            GLObjectCleaner.Update((float)args.Time);
            GL.DepthFunc(DepthFunction.Always);

            //Render triangle
            triTexture.Bind(0);
            skyTexture.Bind(1);
            colortex.BindImage(TextureAccess.WriteOnly, 0);
            depthtex.BindImage(TextureAccess.WriteOnly, 1);
            RayProgram.Bind();
            //RayProgram.SetUniformMat4(0, (TriangleTransform.WorldMatrix));
            RayProgram.SetUniformMat4(1, (cam.Transform.WorldMatrix));
            RayProgram.SetUniformMat4(2, cam.PerspectiveMatrix.Inverted());
            int worksizeX = NextPowerOfTwo(colortex.Width);
            int worksizeY = NextPowerOfTwo(colortex.Height);

            /* Invoke the compute shader. */
            //GL.Clear(ClearBufferMask.ColorBufferBit);
            //GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, RenderCore.MeshBuffers[0].IndexBuffer.Handle);
            //GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, RenderCore.MeshBuffers[0].IndexBuffer.Handle);
            RenderCore.MeshBuffers[(int)value].IndexBuffer.BindBase(0);
            RenderCore.MeshBuffers[(int)value].VertexBuffer.BindBase(1);
            BVHNodeBuffer.BindBase(2);
            BVHIndexBuffer.BindBase(3); 
            GL.DispatchCompute(worksizeX / workGroups.X, worksizeY / workGroups.Y, 1);

            fence.CreateSync();
            fence.Wait();
            //ebo.BindBase(0);
            //vbo.BindBase(1);
            //Draw fullscreen triangle
            FrameBuffer.BindDefault();
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
            colortex.Bind();
            blueNoise.Bind(1);
            depthtex.Bind(2);
            DisplayProgram.Bind();
            vertexArray.Bind();
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

            //Display image
            SwapBuffers();
        }
        static DebugProc _debugProcCallback = LogDebug;
        static GCHandle _debugProcCallbackHandle;

        static void LogDebug(DebugSource debugSource, DebugType debugType, int Id, DebugSeverity debugSeverity, int length, IntPtr message, IntPtr userParams)
        {
            switch (debugSeverity)
            {
                case DebugSeverity.DontCare:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                case DebugSeverity.DebugSeverityNotification:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case DebugSeverity.DebugSeverityHigh:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case DebugSeverity.DebugSeverityMedium:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case DebugSeverity.DebugSeverityLow:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                default:
                    break;
            }
            Console.WriteLine($"{debugSource} {debugSeverity} {debugType} | {Marshal.PtrToStringAnsi(message, length)} ID:{Id}");
            Console.ForegroundColor = ConsoleColor.Gray;
        }
    }
}
