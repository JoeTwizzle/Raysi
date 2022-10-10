using System.Numerics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Raysi.Structures
{
    public struct BuildConfig
    {
        public static BuildConfig Default => new BuildConfig(2, 8, 1.0f);
        public int MinPrims;
        public int MaxPrims;     // Other constant, used later
        public float TraversalCost; // ditto

        public BuildConfig(int min_prims, int max_prims, float traversal_cost)
        {
            this.MinPrims = min_prims;
            this.MaxPrims = max_prims;
            this.TraversalCost = traversal_cost;
        }
    }

    public class BVH
    {
        public unsafe bool Save(string file)
        {
            try
            {
                using BinaryWriter writer = new BinaryWriter(new FileStream(file, FileMode.Create));
                writer.Write(Nodes.Length);
                writer.Write(PrimIndices.Length);
                int nodeSize = Unsafe.SizeOf<Node>();

                for (int i = 0; i < Nodes.Length; i++)
                {
                    Node n = Nodes[i];
                    byte* a = (byte*)&n;
                    for (int j = 0; j < nodeSize; j++)
                    {
                        writer.Write(a[j]);
                    }
                }

                for (int i = 0; i < PrimIndices.Length; i++)
                {
                    writer.Write(PrimIndices[i]);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
        public static unsafe BVH Load(string file)
        {
            var bvh = new BVH();
            using BinaryReader reader = new BinaryReader(new FileStream(file, FileMode.Open));
            uint nodeCount = reader.ReadUInt32();
            uint primCount = reader.ReadUInt32();
            bvh.Nodes = new Node[nodeCount];
            bvh.PrimIndices = new int[primCount];
            int nodeSize = Unsafe.SizeOf<Node>();
            for (int i = 0; i < nodeCount; i++)
            {
                fixed (void* a = &reader.ReadBytes(nodeSize)[0])
                {
                    bvh.Nodes[i] = Unsafe.Read<Node>(a);
                }
            }
            for (int i = 0; i < primCount; i++)
            {
                bvh.PrimIndices[i] = reader.ReadInt32();
            }
            bvh.Refresh();
            return bvh;
        }

        public Node[] Nodes;
        public int[] PrimIndices;

        public int MaxDepth;

        public void Refresh()
        {
            MaxDepth = GetDepth();
        }

        public int GetDepth(int node_index = 0)
        {
            var node = Nodes[node_index];
            return node.IsLeaf ? 1 : 1 + Math.Max(GetDepth(node.FirstIndex), GetDepth(node.FirstIndex + 1));
        }

        Vector4 InterpolateVec4(in Vector2 barycentricCoords, in Vector4 Vector1, in Vector4 Vector2, in Vector4 Vector3)
        {
            return barycentricCoords.X * Vector2 + barycentricCoords.Y * Vector3 + (1 - barycentricCoords.X - barycentricCoords.Y) * Vector1;
        }

        Vector3 InterpolateVec3(in Vector2 barycentricCoords, in Vector3 Vector1, in Vector3 Vector2, in Vector3 Vector3)
        {
            return barycentricCoords.X * Vector2 + barycentricCoords.Y * Vector3 + (1 - barycentricCoords.X - barycentricCoords.Y) * Vector1;
        }

        Vector2 InterpolateVec2(in Vector2 barycentricCoords, in Vector2 Vector1, in Vector2 Vector2, in Vector2 Vector3)
        {
            return barycentricCoords.X * Vector2 + barycentricCoords.Y * Vector3 + (1 - barycentricCoords.X - barycentricCoords.Y) * Vector1;
        }

        float InterpolateFloat(in Vector2 barycentricCoords, in float Vector1, in float Vector2, in float Vector3)
        {
            return barycentricCoords.X * Vector2 + barycentricCoords.Y * Vector3 + (1 - barycentricCoords.X - barycentricCoords.Y) * Vector1;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public RayHit TraverseAll(ref Ray ray, Vertex[] verts, uint[] indices)
        {
            var hit = RayHit.None;
            Span<int> stack = stackalloc int[MaxDepth];
            int head = 0;
            //Stack<int> stack = new Stack<int>();
            //stack.Push(0);
            stack[head++] = 0;
            while (head > 0)
            {
                ref var node = ref Nodes[stack[--head]];
                if (!node.Intersect(ref ray))
                    continue;

                if (node.IsLeaf)
                {
                    for (uint i = 0; i < node.PrimCount; ++i)
                    {
                        var prim_index = PrimIndices[node.FirstIndex + i];
                        ref Vertex v1 = ref verts[indices[prim_index * 3 + 0]];
                        ref Vertex v2 = ref verts[indices[prim_index * 3 + 1]];
                        ref Vertex v3 = ref verts[indices[prim_index * 3 + 2]];
                        if (Triangle.Intersect(v1.Pos, v2.Pos, v3.Pos, ref ray, ref hit))
                        {
                            hit.Normal = Vector3.Normalize(InterpolateVec3(hit.BarycentricCoords, v1.Normal, v2.Normal, v3.Normal));
                            hit.PrimIndex = prim_index;
                        }
                    }
                }
                else
                {
                    stack[head++] = node.FirstIndex;
                    stack[head++] = node.FirstIndex + 1;
                }
            }
            return hit;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public bool TraverseAny(Ray ray, Vertex[] verts, uint[] indices)
        {
            Span<int> stack = stackalloc int[MaxDepth];
            int head = 0;
            //Stack<int> stack = new Stack<int>();
            //stack.Push(0);
            stack[head++] = 0;

            while (head > 0)
            {
                ref var node = ref Nodes[stack[--head]];
                if (!node.Intersect(ref ray))
                    continue;

                if (node.IsLeaf)
                {
                    for (int i = 0; i < node.PrimCount; ++i)
                    {
                        var prim_index = PrimIndices[node.FirstIndex + i];
                        ref Vertex v1 = ref verts[indices[prim_index * 3 + 0]];
                        ref Vertex v2 = ref verts[indices[prim_index * 3 + 1]];
                        ref Vertex v3 = ref verts[indices[prim_index * 3 + 2]];
                        if (Triangle.Intersect(v1.Pos, v2.Pos, v3.Pos, ray))
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    stack[head++] = node.FirstIndex;
                    stack[head++] = node.FirstIndex + 1;
                }
            }
            return false;
        }
    }
}
