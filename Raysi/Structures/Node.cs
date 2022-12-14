using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Raysi.Structures
{
    public struct Intersection
    {
        public float TMin;
        public float TMax;
        public static implicit operator bool(Intersection foo)
        {
            return foo.TMin <= foo.TMax;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Node
    {
        public BBox BBox;
        public int PrimCount;
        public int FirstIndex;

        public Node(BBox bbox, int prim_count, int first_index)
        {
            BBox = bbox;
            PrimCount = prim_count;
            FirstIndex = first_index;
        }

        public bool IsLeaf => PrimCount != 0;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        float robust_min(float a, float b) { return a < b ? a : b; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        float robust_max(float a, float b) { return a > b ? a : b; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Intersection Intersect(ref Ray ray)
        {
            var inv_dir = ray.InverseDir();
            var tmin_temp = (BBox.Min - ray.org) * inv_dir;
            var tmax_temp = (BBox.Max - ray.org) * inv_dir;
            var tmin = Vector3.Min(tmin_temp, tmax_temp);
            var tmax = Vector3.Max(tmin_temp, tmax_temp);
            var intersectMin = MathF.Max(tmin.X, MathF.Max(tmin.Y, MathF.Max(tmin.Z, ray.tmin)));
            var intersectMax = MathF.Min(tmax.X, MathF.Min(tmax.Y, MathF.Min(tmax.Z, ray.tmax)));
            return new Intersection()
            {
                TMin = intersectMin,
                TMax = intersectMax
            };
        }
    }
}
