using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;

namespace Raysi.Structures
{
    public struct BBox
    {
        public static BBox Empty => new BBox(new Vector3(float.MaxValue), new Vector3(float.MinValue));

        public Vector3 Min;
        public Vector3 Max;

        public BBox(Vector3 min, Vector3 max)
        {
            this.Min = min;
            this.Max = max;
        }
        public BBox(Vector3 point)
        {
            Min = point;
            Max = point;
        }
        public void Extend(in Vector3 other)
        {
            Min = Vector3.Min(Min, other);
            Max = Vector3.Max(Max, other);
        }
        public void Extend(in BBox other)
        {
            Min = Vector3.Min(Min, other.Min);
            Max = Vector3.Max(Max, other.Max);
        }

        public Vector3 Diagonal
        {
            get
            {
                return Max - Min;
            }
        }


        public int LargestAxis
        {
            get
            {
                var d = Diagonal;
                int axis = 0;
                if (d.X < d.Y) 
                { 
                    axis = 1;
                    if (d.Y < d.Z) axis = 2;
                }
                else
                {
                    if (d.X < d.Z) axis = 2;
                }              
                return axis;
            }
        }

        public float HalfArea
        {
            get
            {
                var d = Diagonal;
                return (d.X + d.Y) * d.Z + d.X * d.Y;
            }
        }
    }
}

