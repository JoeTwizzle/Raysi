using System;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace Raysi
{
    class Program
    {
        static void Main(string[] args)
        {
            var a = NativeWindowSettings.Default;
            a.Size = new OpenTK.Mathematics.Vector2i(1024);
            a.APIVersion = new Version(4, 6);
            a.Flags |= ContextFlags.Debug;
            var ga = GameWindowSettings.Default;
            ga.IsMultiThreaded = false;
            TestGame g = new TestGame(ga, a);
            g.Run();
        }
    }
}
