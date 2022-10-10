using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Raysi
{
    [StructLayout(LayoutKind.Explicit)]
    internal struct ColorUnion
    {
        [FieldOffset(0)]
        public System.Numerics.Vector4 StructA;
        [FieldOffset(0)]
        public OpenTK.Mathematics.Vector4 StructB;
        [FieldOffset(0)]
        public OpenTK.Mathematics.Color4 StructC;

        public static ColorUnion StaticRef = new ColorUnion();

        public static System.Numerics.Vector4 ToNumerics(OpenTK.Mathematics.Vector4 pStructB)
        {
            StaticRef.StructB = pStructB;
            return StaticRef.StructA;
        }
        public static OpenTK.Mathematics.Vector4 ToOpenTK(System.Numerics.Vector4 pStructA)
        {
            StaticRef.StructA = pStructA;
            return StaticRef.StructB;
        }
        public static System.Numerics.Vector4 ToNumerics(OpenTK.Mathematics.Color4 pStructC)
        {
            StaticRef.StructC = pStructC;
            return StaticRef.StructA;
        }
        public static OpenTK.Mathematics.Color4 ToColor(System.Numerics.Vector4 pStructA)
        {
            StaticRef.StructA = pStructA;
            return StaticRef.StructC;
        }
    }
    [StructLayout(LayoutKind.Explicit)]
    internal struct Vector3Union
    {
        [FieldOffset(0)]
        public System.Numerics.Vector3 StructA;
        [FieldOffset(0)]
        public OpenTK.Mathematics.Vector3 StructB;

        public static Vector3Union StaticRef = new Vector3Union();

        public static System.Numerics.Vector3 ToNumerics(OpenTK.Mathematics.Vector3 pStructB)
        {
            StaticRef.StructB = pStructB;
            return StaticRef.StructA;
        }
        public static OpenTK.Mathematics.Vector3 ToOpenTK(System.Numerics.Vector3 pStructA)
        {
            StaticRef.StructA = pStructA;
            return StaticRef.StructB;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct Vector2Union
    {
        [FieldOffset(0)]
        public System.Numerics.Vector2 StructA;
        [FieldOffset(0)]
        public OpenTK.Mathematics.Vector2 StructB;

        public static Vector2Union StaticRef = new Vector2Union();

        public static System.Numerics.Vector2 ToNumerics(OpenTK.Mathematics.Vector2 pStructB)
        {
            StaticRef.StructB = pStructB;
            return StaticRef.StructA;
        }
        public static OpenTK.Mathematics.Vector2 ToOpenTK(System.Numerics.Vector2 pStructA)
        {
            StaticRef.StructA = pStructA;
            return StaticRef.StructB;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct Matrix4Union
    {
        [FieldOffset(0)]
        public System.Numerics.Matrix4x4 StructA;
        [FieldOffset(0)]
        public OpenTK.Mathematics.Matrix4 StructB;

        public static Matrix4Union StaticRef = new Matrix4Union();

        public static System.Numerics.Matrix4x4 ToNumerics(OpenTK.Mathematics.Matrix4 pStructB)
        {
            StaticRef.StructB = pStructB;
            return StaticRef.StructA;
        }
        public static OpenTK.Mathematics.Matrix4 ToOpenTK(System.Numerics.Matrix4x4 pStructA)
        {
            StaticRef.StructA = pStructA;
            return StaticRef.StructB;
        }
    }

    public static class MathConversions
    {
        public static System.Numerics.Matrix4x4 ToNumerics(this OpenTK.Mathematics.Matrix4 vector)
        {
            return Matrix4Union.ToNumerics(vector);
        }

        public static OpenTK.Mathematics.Matrix4 ToOpenTK(this System.Numerics.Matrix4x4 vector)
        {
            return Matrix4Union.ToOpenTK(vector);
        }

        public static System.Numerics.Vector2 ToNumerics(this OpenTK.Mathematics.Vector2 vector)
        {
            return Vector2Union.ToNumerics(vector);
        }

        public static OpenTK.Mathematics.Vector2 ToOpenTK(this System.Numerics.Vector2 vector)
        {
            return Vector2Union.ToOpenTK(vector);
        }

        public static System.Numerics.Vector3 ToNumerics(this OpenTK.Mathematics.Vector3 vector)
        {
            return Vector3Union.ToNumerics(vector);
        }

        public static OpenTK.Mathematics.Vector3 ToOpenTK(this System.Numerics.Vector3 vector)
        {
            return Vector3Union.ToOpenTK(vector);
        }

        public static System.Numerics.Vector4 ToNumerics(this OpenTK.Mathematics.Vector4 vector)
        {
            return ColorUnion.ToNumerics(vector);
        }

        public static OpenTK.Mathematics.Vector4 ToOpenTK(this System.Numerics.Vector4 vector)
        {
            return ColorUnion.ToOpenTK(vector);
        }

        public static System.Numerics.Vector4 ToNumerics(this OpenTK.Mathematics.Color4 vector)
        {
            return ColorUnion.ToNumerics(vector);
        }

        public static OpenTK.Mathematics.Color4 ToColor(this System.Numerics.Vector4 vector)
        {
            return ColorUnion.ToColor(vector);
        }

        public static System.Numerics.Quaternion ToNumerics(this OpenTK.Mathematics.Quaternion rotation)
        {
            return new System.Numerics.Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W);
        }

        public static OpenTK.Mathematics.Quaternion ToOpenTK(this System.Numerics.Quaternion rotation)
        {
            return new OpenTK.Mathematics.Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W);
        }
    }
}
