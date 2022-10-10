using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raysi.Structures
{
    public struct Bin
    {
        public BBox BBox;
        public int PrimCount;

        public static Bin Create()
        {
            return new Bin { BBox = BBox.Empty, PrimCount = 0 };
        }

        public void Extend(in Bin other)
        {
            BBox.Extend(other.BBox);
            PrimCount += other.PrimCount;
        }

        public float Cost => BBox.HalfArea * PrimCount;
    }
}
