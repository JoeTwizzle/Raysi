using System.Numerics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raysi.Structures
{
    public static class BVHBuilder
    {
        static int BinCount = 16;

        public static BuildConfig BuildConfig = new BuildConfig(2, 8, 0.0f);

        public static BVH Build(BBox[] bboxes, Vector3[] centers, int PrimCount)
        {
            BVH bvh = new BVH();

            bvh.PrimIndices = new int[PrimCount];
            for (int i = 0; i < bvh.PrimIndices.Length; i++)
            {
                bvh.PrimIndices[i] = i;
            }

            bvh.Nodes = new Node[2 * PrimCount - 1];
            bvh.Nodes[0].PrimCount = PrimCount;
            bvh.Nodes[0].FirstIndex = 0;

            int node_count = 1;
            BuildRecursive(bvh, 0, ref node_count, bboxes, centers);
            Array.Resize(ref bvh.Nodes, node_count);
            bvh.Refresh();
            return bvh;
        }

        static void BuildRecursive(BVH bvh, int node_index, ref int node_count, BBox[] bboxes, Vector3[] centers)
        {
            ref Node node = ref bvh.Nodes[node_index];
            Debug.Assert(node.IsLeaf);

            node.BBox = BBox.Empty;
            for (int i = 0; i < node.PrimCount; ++i)
            {
                node.BBox.Extend(bboxes[bvh.PrimIndices[node.FirstIndex + i]]);
            }
            if (node.PrimCount <= BuildConfig.MinPrims)
            {
                return;
            }
            Split min_split = Split.Create();
            for (int axis = 0; axis < 3; ++axis)
            {
                min_split = Split.Min(min_split, FindBestSplit(axis, bvh, ref node, bboxes, centers));
            }
            //Console.WriteLine($"Split axis: {min_split.Axis}, cost = {min_split.Cost}, bin = {min_split.RightBin} ");
            float leaf_cost = node.BBox.HalfArea * (node.PrimCount - BuildConfig.TraversalCost);
            int first_right; // Index of the first primitive in the right child
            if (!min_split || min_split.Cost >= leaf_cost)
            {
                if (node.PrimCount > BuildConfig.MaxPrims)
                {
                    // Fall back solution: The node has too many primitives, we use the median split
                    int axis = node.BBox.LargestAxis;
                    Array.Sort(bvh.PrimIndices, node.FirstIndex, node.PrimCount,
                        Comparer<int>.Create((int i, int j) =>
                        {
                            float a;
                            float b;
                            switch (axis)
                            {
                                case 0:
                                    a = centers[i].X;
                                    b = centers[j].X;
                                    a.CompareTo(b);
                                    return a.CompareTo(b);
                                case 1:
                                    a = centers[i].Y;
                                    b = centers[j].Y;
                                    return a.CompareTo(b);
                                case 2:
                                    a = centers[i].Z;
                                    b = centers[j].Z;
                                    return a.CompareTo(b);
                            }
                            return -1;
                        })
                        );

                    first_right = node.FirstIndex + node.PrimCount / 2;
                }
                else
                    // Terminate with a leaf
                    return;
            }
            else
            {
                //Console.WriteLine("First = " + node.FirstIndex);
                //Console.WriteLine("Count = " + node.PrimCount);
                first_right = Partition(bvh.PrimIndices, node.FirstIndex, node.FirstIndex + node.PrimCount, ref node.BBox, ref min_split, centers);
                //Console.WriteLine("Paritition first right = " + first_right);
                //for (int i = 0; i < bvh.PrimIndices.Length; i++)
                //{
                //    Console.WriteLine(bvh.PrimIndices[i]);
                //}
            }
            var first_child = node_count;
            ref var left = ref bvh.Nodes[first_child];
            ref var right = ref bvh.Nodes[first_child + 1];
            node_count += 2;

            left.PrimCount = first_right - node.FirstIndex;
            right.PrimCount = node.PrimCount - left.PrimCount;
            left.FirstIndex = node.FirstIndex;
            right.FirstIndex = first_right;

            node.FirstIndex = first_child;
            node.PrimCount = 0;

            BuildRecursive(bvh, first_child, ref node_count, bboxes, centers);
            BuildRecursive(bvh, first_child + 1, ref node_count, bboxes, centers);
        }

        #region std::Partition
        static int Partition(int[] array, int first, int last, ref BBox bbox, ref Split min_split, Vector3[] center)
        {
            int ufirst = first;
            int ulast = last;
            //int first = FindIfNot(array, start, end, ref bbox, ref min_split, center);
            while (true)
            {
                while (true)
                {
                    if (ufirst == ulast)
                    {
                        first = ufirst;
                        return first;
                    }

                    if (!Predicate(array[ufirst], ref bbox, ref min_split, center))
                        break;

                    ++ufirst;
                }

                do
                {
                    --ulast;
                    if (ufirst == ulast)
                    {
                        first = ufirst;
                        return first;
                    }
                } while (!Predicate(array[ulast], ref bbox, ref min_split, center));


                int temp = array[ufirst];
                array[ufirst] = array[ulast];
                array[ulast] = temp;

                ++ufirst;
            }
        }
        static bool Predicate(int i, ref BBox bbox, ref Split min_split, Vector3[] centers)
        {
            //Console.WriteLine("Part index = " + i);
            return BinIndex(min_split.Axis, bbox, centers[i]) < min_split.RightBin;
        }
        #endregion

        static int BinIndex(int axis, in BBox bbox, in Vector3 center)
        {
            int index = -1;
            switch (axis)
            {
                case 0:
                    index = (int)((center.X - bbox.Min.X) * (BinCount / (bbox.Max.X - bbox.Min.X)));
                    return Math.Min(BinCount - 1, (Math.Max(0, index)));
                case 1:
                    index = (int)((center.Y - bbox.Min.Y) * (BinCount / (bbox.Max.Y - bbox.Min.Y)));
                    return Math.Min(BinCount - 1, (Math.Max(0, index)));
                case 2:
                    index = (int)((center.Z - bbox.Min.Z) * (BinCount / (bbox.Max.Z - bbox.Min.Z)));
                    return Math.Min(BinCount - 1, (Math.Max(0, index)));
            }
            return -1;
        }

        static Split FindBestSplit(
                int axis,
                BVH bvh,
                ref Node node,
                BBox[] bboxes,
                Vector3[] centers)
        {
            Bin[] bins = new Bin[BinCount];
            for (int i = 0; i < BinCount; i++)
            {
                bins[i] = Bin.Create();
            }
            for (int i = 0; i < node.PrimCount; ++i)
            {
                var prim_index = bvh.PrimIndices[node.FirstIndex + i];
                ref var bin = ref bins[BinIndex(axis, node.BBox, centers[prim_index])];
                bin.BBox.Extend(bboxes[prim_index]);
                bin.PrimCount++;
            }
            float[] right_cost = new float[BinCount];
            right_cost[0] = float.MinValue;
            Bin leftAccum = Bin.Create();
            Bin rightAccum = Bin.Create();
            for (int i = BinCount - 1; i > 0; --i)
            {
                rightAccum.Extend(bins[i]);
                // Due to the definition of an empty bounding box, the cost of an empty bin is -NaN
                right_cost[i] = rightAccum.Cost;
            }
            Split split = Split.Create();
            split.Axis = axis;
            for (int i = 0; i < BinCount - 1; ++i)
            {
                leftAccum.Extend(bins[i]);
                float cost = leftAccum.Cost + right_cost[i + 1];
                // This test is defined such that NaNs are automatically ignored.
                // Thus, only valid combinations with non-empty bins are considered.
                if (cost < split.Cost)
                {
                    split.Cost = cost;
                    split.RightBin = i + 1;
                }
            }
            return split;
        }
    }
}
