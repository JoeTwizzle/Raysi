using System.Numerics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Raysi.Structures
{
    public struct Triangle
    {
        public Vector3 p0, p1, p2;

        public Triangle(Vector3 p0, Vector3 p1, Vector3 p2)
        {
            this.p0 = p0;
            this.p1 = p1;
            this.p2 = p2;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public bool Intersect(Ray ray)
        {
            var e1 = p0 - p1;
            var e2 = p2 - p0;
            var n = Vector3.Cross(e1, e2);

            var c = p0 - ray.org;
            var r = Vector3.Cross(ray.dir, c);
            var inv_det = 1.0f / Vector3.Dot(n, ray.dir);

            var u = Vector3.Dot(r, e2) * inv_det;
            var v = Vector3.Dot(r, e1) * inv_det;
            var w = 1.0f - u - v;

            // These comparisons are designed to return false
            // when one of t, u, or v is a NaN
            if (u >= 0 && v >= 0 && w >= 0)
            {
                var t = Vector3.Dot(n, c) * inv_det;
                if (t >= ray.tmin && t <= ray.tmax)
                {
                    return true;
                }
            }

            return false;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static bool Intersect(Vector3 p0, Vector3 p1, Vector3 p2, Ray ray)
        {
            var e1 = p0 - p1;
            var e2 = p2 - p0;
            var n = Vector3.Cross(e1, e2);

            var c = p0 - ray.org;
            var r = Vector3.Cross(ray.dir, c);
            var inv_det = 1.0f / Vector3.Dot(n, ray.dir);

            var u = Vector3.Dot(r, e2) * inv_det;
            var v = Vector3.Dot(r, e1) * inv_det;
            var w = 1.0f - u - v;

            // These comparisons are designed to return false
            // when one of t, u, or v is a NaN
            if (u >= 0 && v >= 0 && w >= 0)
            {
                var t = Vector3.Dot(n, c) * inv_det;
                if (t >= ray.tmin && t <= ray.tmax)
                {
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public bool Intersect(ref Ray ray, ref RayHit hit)
        {
            var e1 = p0 - p1;
            var e2 = p2 - p0;
            var n = Vector3.Cross(e1, e2);

            var c = p0 - ray.org;
            var r = Vector3.Cross(ray.dir, c);
            var inv_det = 1.0f / Vector3.Dot(n, ray.dir);

            var u = Vector3.Dot(r, e2) * inv_det;
            var v = Vector3.Dot(r, e1) * inv_det;
            var w = 1.0f - u - v;

            // These comparisons are designed to return false
            // when one of t, u, or v is a NaN
            if (u >= 0 && v >= 0 && w >= 0)
            {
                var t = Vector3.Dot(n, c) * inv_det;
                if (t >= ray.tmin && t <= ray.tmax)
                {
                    ray.tmax = t;
                    hit.Dist = t;
                    hit.Position = ray.org + t * ray.dir;
                    hit.BarycentricCoords = new Vector2(u, v);
                    return true;
                }
            }

            return false;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static bool Intersect(Vector3 p0, Vector3 p1, Vector3 p2,ref Ray ray, ref RayHit hit)
        {
            var e1 = p0 - p1;
            var e2 = p2 - p0;
            var n = Vector3.Cross(e1, e2);

            var c = p0 - ray.org;
            var r = Vector3.Cross(ray.dir, c);
            var inv_det = 1.0f / Vector3.Dot(n, ray.dir);

            var u = Vector3.Dot(r, e2) * inv_det;
            var v = Vector3.Dot(r, e1) * inv_det;
            var w = 1.0f - u - v;

            // These comparisons are designed to return false
            // when one of t, u, or v is a NaN
            if (u >= 0 && v >= 0 && w >= 0)
            {
                var t = Vector3.Dot(n, c) * inv_det;
                if (t >= ray.tmin && t <= ray.tmax)
                {
                    ray.tmax = t;
                    hit.Dist = t;
                    hit.Position = ray.org + t * ray.dir;
                    hit.BarycentricCoords = new Vector2(u, v);
                    return true;
                }
            }

            return false;
        }
    }
}
