using GLGraphics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;

namespace Raysi
{
    public class MeshBuffer : IDisposable
    {
        //public PersistentBuffer<MeshVertex> VertexBuffer;
        //public PersistentBuffer<int> IndexBuffer;
        public GLBuffer VertexBuffer;
        public GLBuffer IndexBuffer;
        List<SubMeshRefrence> meshRefrences;
        List<MemoryMarker> vertexCodes;
        List<MemoryMarker> indexCodes;
        const int triCount = 0;



        int freeVertexSpace;
        int freeIndexSpace;
        public MeshBuffer(int minVerts = 0, int minIndices = 0)
        {
            vertexCodes = new List<MemoryMarker>();
            indexCodes = new List<MemoryMarker>();
            meshRefrences = new List<SubMeshRefrence>();
            //VertexBuffer = new PersistentBuffer<MeshVertex>();
            //IndexBuffer = new PersistentBuffer<int>();
            VertexBuffer = new GLBuffer();
            IndexBuffer = new GLBuffer();
            int v = Math.Max(minVerts, triCount * 3);
            int i = Math.Max(minIndices, triCount * 3);
            VertexBuffer.Init<Vertex>(BufferType.ShaderStorageBuffer, v);
            IndexBuffer.Init<uint>(BufferType.ShaderStorageBuffer, i);
            vertexCodes.Add(new MemoryMarker(true, v));
            indexCodes.Add(new MemoryMarker(true, i));
            freeVertexSpace = v;
            freeIndexSpace = i;
            //VertexBuffer.Map();
            //IndexBuffer.Map();
        }

        public int GetVertexCount()
        {
            int sum = 0;
            for (int i = 0; i < vertexCodes.Count; i++)
            {
                if (!vertexCodes[i].Free)
                {
                    sum += vertexCodes[i].Length;
                }
            }
            return sum;
        }

        public int GetIndexCount()
        {
            int sum = 0;
            for (int i = 0; i < indexCodes.Count; i++)
            {
                if (!indexCodes[i].Free)
                {
                    sum += indexCodes[i].Length;
                }
            }
            return sum;
        }

        public bool TryAddMesh(SubMesh mesh, out SubMeshRefrence? refrence)
        {
            if (freeVertexSpace < mesh.Vertices.Length || freeIndexSpace < mesh.Indices.Length)
            {
                refrence = null;
                return false;
            }

            if (TryGetMeshIndices(mesh.Vertices.Length, mesh.Indices.Length, out int vertexOffset, out int indexOffset, out MemoryMarker vm, out MemoryMarker im))
            {

                VertexBuffer.UpdateData(mesh.Vertices, vertexOffset * VertexBuffer.DataSize);
                IndexBuffer.UpdateData(mesh.Indices, (indexOffset) * IndexBuffer.DataSize);

                refrence = new SubMeshRefrence(this, vertexOffset, mesh.Vertices.Length, indexOffset, mesh.Indices.Length, vm, im);
                meshRefrences.Add(refrence);

                freeVertexSpace -= mesh.Vertices.Length;
                freeIndexSpace -= mesh.Indices.Length;
                return true;
            }
            refrence = null;
            return false;
        }

        bool TryGetMeshIndices(int v, int i, out int vtx, out int idx, out MemoryMarker vm, out MemoryMarker im)
        {
            if (TryGetFreeVertexIndex(v, out vtx, out int vi) && TryGetFreeIndexIndex(i, out idx, out int ii))
            {
                var vc = vertexCodes[vi];
                vertexCodes.RemoveAt(vi);

                vm = new MemoryMarker(false, v);
                vertexCodes.Insert(vi, vm);
                if (vc.Length - v > 0)
                {
                    vertexCodes.Insert(vi + 1, new MemoryMarker(true, vc.Length - v));
                }

                var ic = indexCodes[ii];
                indexCodes.RemoveAt(ii);

                im = new MemoryMarker(false, i);
                indexCodes.Insert(ii, im);
                if (ic.Length - i > 0)
                {
                    indexCodes.Insert(ii + 1, new MemoryMarker(true, ic.Length - i));
                }
                return true;
            }
            idx = 0;
            vm = new MemoryMarker();
            im = new MemoryMarker();
            return false;
        }

        bool TryGetFreeVertexIndex(int verts, out int idx, out int i)
        {
            int offset = 0;
            for (i = 0; i < vertexCodes.Count; i++)
            {
                var code = vertexCodes[i];
                if (code.Free && code.Length >= verts)
                {
                    idx = offset;
                    return true;
                }
                offset += code.Length;
            }
            i = 0;
            idx = 0;
            return false;
        }

        bool TryGetFreeIndexIndex(int verts, out int idx, out int i)
        {
            int offset = 0;
            for (i = 0; i < indexCodes.Count; i++)
            {
                var code = indexCodes[i];
                if (code.Free && code.Length >= verts)
                {
                    idx = offset;
                    return true;
                }
                offset += code.Length;
            }
            i = 0;
            idx = 0;
            return false;
        }

        public bool ContainsMesh(SubMeshRefrence meshRefrence, out int index)
        {
            index = meshRefrences.IndexOf(meshRefrence);
            return index != -1;
        }

        public bool TryRemoveMesh(SubMeshRefrence meshRefrence)
        {
            if (!meshRefrences.Remove(meshRefrence))
            {
                return false;
            }

            FreeVerts(meshRefrence);
            FreeIndices(meshRefrence);
            return true;
        }

        void FreeVerts(SubMeshRefrence meshRefrence)
        {
            int vi = vertexCodes.IndexOf(meshRefrence.VertexMarker);
            int size = meshRefrence.VertexMarker.Length;
            //Free space on the right
            if (vi + 1 < vertexCodes.Count)
            {
                var va = vertexCodes[vi + 1];
                if (va.Free)
                {
                    vertexCodes.RemoveAt(vi + 1);
                    size += va.Length;
                }
            }
            vertexCodes.RemoveAt(vi);
            //Free space on the left
            if (vi - 1 >= 0)
            {
                var va = vertexCodes[vi - 1];
                if (va.Free)
                {
                    vertexCodes.RemoveAt(vi - 1);
                    vi -= 1;
                    size += va.Length;
                }
            }
            vertexCodes.Insert(vi, new MemoryMarker(true, size));
        }

        void FreeIndices(SubMeshRefrence meshRefrence)
        {
            int vi = indexCodes.IndexOf(meshRefrence.IndexMarker);
            int size = meshRefrence.IndexMarker.Length;
            //Free space on the right
            if (vi + 1 < indexCodes.Count)
            {
                var va = indexCodes[vi + 1];
                if (va.Free)
                {
                    indexCodes.RemoveAt(vi + 1);
                    size += va.Length;
                }
            }
            indexCodes.RemoveAt(vi);
            //Free space on the left
            if (vi - 1 >= 0)
            {
                var va = indexCodes[vi - 1];
                if (va.Free)
                {
                    indexCodes.RemoveAt(vi - 1);
                    vi -= 1;
                    size += va.Length;
                }
            }
            indexCodes.Insert(vi, new MemoryMarker(true, size));
        }

        public void Dispose()
        {
            VertexBuffer.Dispose();
            IndexBuffer.Dispose();
        }
    }
}
