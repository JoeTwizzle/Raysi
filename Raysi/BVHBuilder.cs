//using OpenTK.Mathematics;
//using System;
//using System.Collections.Generic;
//using System.Runtime.CompilerServices;
//using System.Runtime.InteropServices;
//using System.Runtime.Intrinsics;
//using System.Runtime.Intrinsics.X86;
//namespace Raysi
//{
//    [StructLayout(LayoutKind.Explicit, Size = 6 * sizeof(float))]
//    public struct BBox
//    {
//        [FieldOffset(0)]
//        public Vector3 Min;
//        [FieldOffset(3 * sizeof(float))]
//        public Vector3 Max;
//        //
//        // Zusammenfassung:
//        //     Gets or sets a vector describing the size of the Box3 structure.
//        public Vector3 Size
//        {
//            get
//            {
//                return Max - Min;
//            }
//            set
//            {
//                Vector3 center = Center;
//                Min = center - value * 0.5f;
//                Max = center + value * 0.5f;
//            }
//        }

//        //
//        // Zusammenfassung:
//        //     Gets or sets a vector describing half the size of the box.
//        public Vector3 HalfSize
//        {
//            get
//            {
//                return Size / 2f;
//            }
//            set
//            {
//                Size = value * 2f;
//            }
//        }

//        //
//        // Zusammenfassung:
//        //     Gets or sets a vector describing the center of the box.
//        public Vector3 Center
//        {
//            get
//            {
//                return HalfSize + Min;
//            }
//        }

//        public void Inflate(Vector3 point)
//        {
//            Min = Vector3.ComponentMin(Min, point);
//            Max = Vector3.ComponentMax(Max, point);
//        }
//    }
//    class BVHBuilder
//    {
//        static List<BVHNode> m_nodes = new List<BVHNode>();
//        public static List<BVHPackedNode> m_packedNodes = new List<BVHPackedNode>();

//        class BVHNode
//        {
//            public static readonly int LeafMask = unchecked((int)0x80000000);
//            public static readonly int InvalidMask = unchecked((int)0xFFFFFFFF);

//            public Vector3 bboxMin;
//            public int triangleId = InvalidMask;

//            public Vector3 bboxMax;
//            public int next = InvalidMask;

//            public bool isLeaf() { return triangleId != InvalidMask; }
//        }

//        [StructLayout(LayoutKind.Sequential, Pack = 0)]
//        public struct BVHPackedNode
//        {
//            public int a, b, c, d;
//        }

//        class TempNode : BVHNode
//        {
//            public int visitOrder = InvalidMask;
//            public int parent = InvalidMask;

//            public int left;
//            public int right;

//            public Vector3 bboxCenter;

//            public float primArea = 0.0f;

//            public float surfaceAreaLeft = 0.0f;
//            public float surfaceAreaRight = 0.0f;
//        }

//        static float bboxSurfaceArea(Vector3 bboxMin, Vector3 bboxMax)
//        {
//            Vector3 extents = bboxMax - bboxMin;
//            return (extents.X * extents.Y + extents.Y * extents.Z + extents.Z * extents.X) * 2.0f;
//        }

//        static float bboxSurfaceArea(BBox bbox)
//        {
//            return bboxSurfaceArea(bbox.Min, bbox.Max);
//        }

//        static void setBounds(BVHNode node, Vector3 min, Vector3 max)
//        {
//            node.bboxMin.X = min.X;
//            node.bboxMin.Y = min.Y;
//            node.bboxMin.Z = min.Z;

//            node.bboxMax.X = max.X;
//            node.bboxMax.Y = max.Y;
//            node.bboxMax.Z = max.Z;
//        }

//        static Vector3 extractVec3(Vector128<float> v)
//        {
//            return v.AsVector3().ToOpenTK();
//        }

//        static BBox calculateBounds(List<TempNode> nodes, int begin, int end)
//        {
//            BBox bounds = new BBox();
//            if (begin == end)
//            {
//                bounds.Min = new Vector3();
//                bounds.Max = new Vector3();
//            }
//            else
//            {
//                Vector128<float> bboxMin = Vector128.Create(float.MaxValue);
//                Vector128<float> bboxMax = Vector128.Create(float.MinValue);
//                for (int i = begin; i < end; ++i)
//                {
//                    Vector128<float> nodeBoundsMin = Vector128.Create(nodes[i].bboxMin.X, nodes[i].bboxMin.Y, nodes[i].bboxMin.Z, float.MaxValue);
//                    Vector128<float> nodeBoundsMax = Vector128.Create(nodes[i].bboxMax.X, nodes[i].bboxMax.Y, nodes[i].bboxMax.Z, float.MinValue);
//                    bboxMin = Sse.Min(bboxMin, nodeBoundsMin);
//                    bboxMax = Sse.Max(bboxMax, nodeBoundsMax);
//                }
//                bounds.Min = extractVec3(bboxMin);
//                bounds.Max = extractVec3(bboxMax);
//            }
//            return bounds;
//        }

//        static void expandInit(ref BBox box)
//        {
//            box.Max = new Vector3(float.MinValue);
//            box.Min = new Vector3(float.MaxValue);
//        }
//        static Comparer<TempNode> Comparer;
//        static Comparer<TempNode> Comparer2;
//        static Comparer<TempNode> Comparer3;
//        static int split(List<TempNode> nodes, int begin, int end, ref BBox nodeBounds)
//        {
//            int count = end - begin;
//            int bestSplit = begin;

//            if (count <= 1000000)
//            {
//                int bestAxis = 0;
//                int globalBestSplit = begin;
//                float globalBestCost = float.MaxValue;

//                for (int axis = 0; axis < 3; axis++)
//                {
//                    int ax = 0;
//                    if (Comparer2 == null)
//                    {
//                        Comparer2 = Comparer<TempNode>.Create((X, Y) => X.bboxCenter[ax] < Y.bboxCenter[ax] ? 1 : 0);
//                    }
//                    // TODO: just sort into N buckets
//                    ax = axis;
//                    nodes.Sort(begin, end - begin, Comparer2);
//                    //    std.sort(nodes.begin() + begin, nodes.begin() + end,


//                    //        [&](TempNode & a, TempNode & b)

//                    //{
//                    //        return a.bboxCenter[axis] < b.bboxCenter[axis];
//                    //    });

//                    BBox boundsLeft = new BBox();
//                    expandInit(ref boundsLeft);

//                    BBox boundsRight = new BBox();
//                    expandInit(ref boundsRight);

//                    for (int indexLeft = 0; indexLeft < count; ++indexLeft)
//                    {
//                        int indexRight = count - indexLeft - 1;

//                        boundsLeft.Inflate(nodes[begin + indexLeft].bboxMin);
//                        boundsLeft.Inflate(nodes[begin + indexLeft].bboxMax);

//                        boundsRight.Inflate(nodes[begin + indexRight].bboxMin);
//                        boundsRight.Inflate(nodes[begin + indexRight].bboxMax);

//                        float surfaceAreaLeft = bboxSurfaceArea(boundsLeft);
//                        float surfaceAreaRight = bboxSurfaceArea(boundsRight);

//                        nodes[begin + indexLeft].surfaceAreaLeft = surfaceAreaLeft;
//                        nodes[begin + indexRight].surfaceAreaRight = surfaceAreaRight;
//                    }

//                    float bestCost = float.MaxValue;
//                    for (int mid = begin + 1; mid < end; ++mid)
//                    {
//                        float surfaceAreaLeft = nodes[mid - 1].surfaceAreaLeft;
//                        float surfaceAreaRight = nodes[mid].surfaceAreaRight;

//                        int countLeft = mid - begin;
//                        int countRight = end - mid;

//                        float costLeft = surfaceAreaLeft * (float)countLeft;
//                        float costRight = surfaceAreaRight * (float)countRight;

//                        float cost = costLeft + costRight;
//                        if (cost < bestCost)
//                        {
//                            bestSplit = mid;
//                            bestCost = cost;
//                        }
//                    }

//                    if (bestCost < globalBestCost)
//                    {
//                        globalBestSplit = bestSplit;
//                        globalBestCost = bestCost;
//                        bestAxis = axis;
//                    }
//                }
//                if (Comparer3 == null)
//                {
//                    Comparer3 = Comparer<TempNode>.Create((X, Y) => X.bboxCenter[bestAxis] < Y.bboxCenter[bestAxis] ? 1 : 0);
//                }
//                nodes.Sort(begin, end - begin, Comparer3);
//                //        std.sort(nodes.begin() + begin, nodes.begin() + end,



//                //            [&](TempNode & a, TempNode & b)


//                //{
//                //            return a.bboxCenter[bestAxis] < b.bboxCenter[bestAxis];
//                //        });

//                return globalBestSplit;
//            }
//            else
//            {
//                Vector3 extents = nodeBounds.Max - nodeBounds.Min;
//                float max = MathF.Max(MathF.Max(extents.X, extents.Y), extents.Z);

//                int majorAxis = -1;
//                if (max == extents.X)
//                {
//                    majorAxis = 0;
//                }
//                else if (max == extents.Y)
//                {
//                    majorAxis = 1;
//                }
//                else if (max == extents.Z)
//                {
//                    majorAxis = 2;
//                }
//                if (Comparer == null)
//                {
//                    Comparer = Comparer<TempNode>.Create((X, Y) => X.bboxCenter[majorAxis] < Y.bboxCenter[majorAxis] ? 1 : 0);
//                }

//                nodes.Sort(begin, end - begin, Comparer);

//                //        std.sort(nodes.begin() + begin, nodes.begin() + end,


//                //                [&](TempNode & a, TempNode & b)


//                //{
//                //            return a.bboxCenter[majorAxis] < b.bboxCenter[majorAxis];
//                //        });

//                float splitPos = (nodeBounds.Min[majorAxis] + nodeBounds.Max[majorAxis]) * 0.5f;
//                for (int mid = begin + 1; mid < end; ++mid)
//                {
//                    if (nodes[mid].bboxCenter[majorAxis] >= splitPos)
//                    {
//                        return mid;
//                    }
//                }

//                return end - 1;
//            };
//        }

//        static int buildInternal(List<TempNode> nodes, int begin, int end)
//        {
//            int count = end - begin;

//            if (count == 1)
//            {
//                return begin;
//            }

//            BBox bounds = calculateBounds(nodes, begin, end);

//            int mid = split(nodes, begin, end, ref bounds);

//            int nodeId = nodes.Count;
//            nodes.Add(new TempNode());

//            TempNode node = new TempNode();

//            node.left = buildInternal(nodes, begin, mid);
//            node.right = buildInternal(nodes, mid, end);

//            float surfaceAreaLeft = bboxSurfaceArea(nodes[(int)node.left].bboxMin, nodes[(int)node.left].bboxMax);
//            float surfaceAreaRight = bboxSurfaceArea(nodes[(int)node.right].bboxMin, nodes[(int)node.right].bboxMax);

//            if (surfaceAreaRight > surfaceAreaLeft)
//            {
//                int temp = node.left;
//                node.left = node.right;
//                node.right = temp;
//                //std.swap(node.left, node.right);
//            }

//            setBounds(node, bounds.Min, bounds.Max);
//            node.bboxCenter = bounds.Center;
//            node.triangleId = BVHNode.InvalidMask;

//            nodes[(int)node.left].parent = (int)nodeId;
//            nodes[(int)node.right].parent = (int)nodeId;

//            nodes[nodeId] = node;

//            return nodeId;
//        }

//        static void setDepthFirstVisitOrder(List<TempNode> nodes, int nodeId, int nextId, ref int order)
//        {
//            TempNode node = nodes[(int)nodeId];

//            node.visitOrder = order++;
//            node.next = nextId;

//            if (node.left != BVHNode.InvalidMask)
//            {
//                setDepthFirstVisitOrder(nodes, node.left, node.right, ref order);
//            }

//            if (node.right != BVHNode.InvalidMask)
//            {
//                setDepthFirstVisitOrder(nodes, node.right, nextId, ref order);
//            }
//        }

//        static void setDepthFirstVisitOrder(List<TempNode> nodes, int root)
//        {
//            int order = 0;
//            setDepthFirstVisitOrder(nodes, root, BVHNode.InvalidMask, ref order);
//        }



//        class BVHPrimitiveNode
//        {
//            public Vector3 edge0;
//            public int triangleId;
//            public Vector3 edge1;
//            public int next;
//        }

//        public static void build(MeshBuffer mb)
//        {
//            int[] indices = new int[mb.IndexBuffer.ElementCount];
//            OpenTK.Graphics.OpenGL4.GL.GetNamedBufferSubData(mb.IndexBuffer.Handle, IntPtr.Zero, mb.IndexBuffer.Size, indices);
//            Vertex[] vertices = new Vertex[mb.VertexBuffer.ElementCount];
//            OpenTK.Graphics.OpenGL4.GL.GetNamedBufferSubData(mb.VertexBuffer.Handle, IntPtr.Zero, mb.VertexBuffer.Size, vertices);
//            Vector3 getVertex(int vertexId)
//            {
//                return vertices[vertexId].Pos;
//            }
//            int primCount = mb.IndexBuffer.ElementCount / 3;
//            //    auto getVertex = [vertices, stride](int vertexId)


//            //{
//            //        return Vector3(vertices + stride * vertexId);
//            //    };

//            m_nodes = new List<BVHNode>(primCount * 2 - 1);
//            for (int i = 0; i < primCount * 2 - 1; i++)
//            {
//                m_nodes.Add(new BVHNode());
//            }
//            List<TempNode> tempNodes = new List<TempNode>(primCount * 2 - 1);

//            for (int i = 0; i < indices.Length / 3; ++i)
//            {
//                TempNode node = new TempNode();
//                BBox box = new BBox();
//                expandInit(ref box);

//                Vector3 v0 = getVertex(indices[i * 3 + 0]);
//                Vector3 v1 = getVertex(indices[i * 3 + 1]);
//                Vector3 v2 = getVertex(indices[i * 3 + 2]);

//                box.Inflate(v0);
//                box.Inflate(v1);
//                box.Inflate(v2);
//                float calculateArea(Vector3 a, Vector3 b, Vector3 c) { return Vector3.Cross(c - a, b - a).Length * 0.5f; }
//                node.primArea = calculateArea(v0, v1, v2);

//                setBounds(node, box.Min, box.Max);

//                node.bboxCenter = box.Center;
//                node.triangleId = i;
//                node.left = BVHNode.InvalidMask;
//                node.right = BVHNode.InvalidMask;
//                tempNodes.Add(node);
//            }

//            int rootIndex = buildInternal(tempNodes, 0, tempNodes.Count);

//            setDepthFirstVisitOrder(tempNodes, rootIndex);

//            //m_nodes.resize(tempNodes.size());
//            while (m_nodes.Count > tempNodes.Count)
//            {
//                m_nodes.RemoveAt(m_nodes.Count - 1);
//            }
//            for (int oldIndex = 0; oldIndex < (int)tempNodes.Count; ++oldIndex)
//            {
//                TempNode oldNode = tempNodes[oldIndex];

//                BVHNode newNode = m_nodes[oldNode.visitOrder];

//                Vector3 bboxMin = oldNode.bboxMin;
//                Vector3 bboxMax = oldNode.bboxMax;
//                setBounds(newNode, bboxMin, bboxMax);

//                newNode.triangleId = oldNode.triangleId;
//                newNode.next = oldNode.next == BVHNode.InvalidMask
//                    ? BVHNode.InvalidMask
//                    : tempNodes[oldNode.next].visitOrder;
//            }

//            //m_packedNodes.reserve(m_nodes.Count + primCount);
//            m_packedNodes = new List<BVHPackedNode>(m_nodes.Count + indices.Length / 3);

//            for (int i = 0; i < (int)tempNodes.Count; ++i)
//            {
//                BVHNode node = m_nodes[i];

//                if (node.isLeaf())
//                {
//                    BVHPrimitiveNode packedNode = new BVHPrimitiveNode();

//                    Vector3 v0 = getVertex(indices[node.triangleId * 3 + 0]);
//                    Vector3 v1 = getVertex(indices[node.triangleId * 3 + 1]);
//                    Vector3 v2 = getVertex(indices[node.triangleId * 3 + 2]);

//                    packedNode.edge0 = v1 - v0;
//                    packedNode.triangleId = node.triangleId;

//                    packedNode.edge1 = v2 - v0;
//                    packedNode.next = node.next;

//                    BVHPackedNode data0 = new BVHPackedNode(), data1 = new BVHPackedNode();
//                    //memcpy(&data0, &packedNode.edge0, sizeof(BVHPackedNode));
//                    //memcpy(&data1, &packedNode.edge1, sizeof(BVHPackedNode));
//                    unsafe
//                    {
//                        Vector3 x = packedNode.edge0;
//                        data0.a = Unsafe.AsRef<int>(&x + sizeof(int) * 0);
//                        data0.b = Unsafe.AsRef<int>(&x + sizeof(int) * 1);
//                        data0.c = Unsafe.AsRef<int>(&x + sizeof(int) * 2);
//                        data0.d = packedNode.triangleId;


//                        Vector3 y = packedNode.edge1;
//                        data1.a = Unsafe.AsRef<int>(&y + sizeof(int) * 0);
//                        data1.b = Unsafe.AsRef<int>(&y + sizeof(int) * 1);
//                        data1.c = Unsafe.AsRef<int>(&y + sizeof(int) * 2);
//                        data1.d = packedNode.next;

//                    }
//                    m_packedNodes.Add(data0);
//                    m_packedNodes.Add(data1);
//                }

//                else
//                {
//                    BVHNode packedNode = new BVHNode();

//                    packedNode.bboxMin = node.bboxMin;
//                    packedNode.triangleId = node.triangleId;
//                    packedNode.bboxMax = node.bboxMax;
//                    packedNode.next = node.next;

//                    BVHPackedNode data0 = new BVHPackedNode(), data1 = new BVHPackedNode();
//                    unsafe
//                    {
//                        Vector3 x = packedNode.bboxMin;
//                        data0.a = Unsafe.AsRef<int>(&x + sizeof(int) * 0);
//                        data0.b = Unsafe.AsRef<int>(&x + sizeof(int) * 1);
//                        data0.c = Unsafe.AsRef<int>(&x + sizeof(int) * 2);
//                        data0.d = packedNode.triangleId;


//                        Vector3 y = packedNode.bboxMax;
//                        data1.a = Unsafe.AsRef<int>(&y + sizeof(int) * 0);
//                        data1.b = Unsafe.AsRef<int>(&y + sizeof(int) * 1);
//                        data1.c = Unsafe.AsRef<int>(&y + sizeof(int) * 2);
//                        data1.d = packedNode.next;

//                    }

//                    m_packedNodes.Add(data0);
//                    m_packedNodes.Add(data1);
//                }
//            }

//            for (int i = 0; i < indices.Length / 3; ++i)
//            {
//                Vector3 v0 = getVertex(indices[i * 3 + 0]);
//                BVHPackedNode data = new BVHPackedNode();
//                unsafe
//                {
//                    data.a = Unsafe.AsRef<int>(&v0 + sizeof(int) * 0);
//                    data.b = Unsafe.AsRef<int>(&v0 + sizeof(int) * 1);
//                    data.c = Unsafe.AsRef<int>(&v0 + sizeof(int) * 2);
//                    data.d = i;
//                }
//                //memcpy(&data, &v0, sizeof(BVHPackedNode));
//                m_packedNodes.Add(data);
//            }
//        }
//    }
//}