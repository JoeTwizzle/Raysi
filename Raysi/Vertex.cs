using System.Numerics;
using System.Runtime.InteropServices;

namespace Raysi
{
    [StructLayout(LayoutKind.Explicit, Size = (sizeof(float) * 4) * 3)]
    public struct Vertex
    {
        [FieldOffset((sizeof(float) * 4) * 0)]
        public Vector3 Pos;
        [FieldOffset((sizeof(float) * 4) * 1)]
        public Vector3 Normal;
        [FieldOffset((sizeof(float) * 4) * 2)]
        public Vector2 UV;

        public Vertex(Vector3 pos, Vector3 normal, Vector2 uV)
        {
            Pos = pos;
            Normal = normal;
            UV = uV;
        }
    }
}
