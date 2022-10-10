using System.Numerics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raysi.Structures
{
    public struct RayHit
    {
        public static RayHit None => new RayHit() { PrimIndex = -1, Dist = float.MaxValue };
        public int PrimIndex;
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 BarycentricCoords;
        public float Dist;
    };
    //public struct Hit
    //{
    //    public int PrimIndex;

    //    public static Hit None => new Hit() { PrimIndex = -1 };
    //}
}
