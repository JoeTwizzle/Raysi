using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raysi
{
    static class RenderCore
    {
        public static List<SubMeshRefrence> SubMeshRefrences { get; private set; }
        public static List<MeshBuffer> MeshBuffers { get; private set; }
        static RenderCore()
        {
            MeshBuffers = new List<MeshBuffer>();
            SubMeshRefrences = new List<SubMeshRefrence>();
        }
        public static SubMeshRefrence AddMesh(SubMesh mesh)
        {
            for (int i = 0; i < MeshBuffers.Count; i++)
            {
                if (MeshBuffers[i].TryAddMesh(mesh, out var meshRefrence))
                {
                    SubMeshRefrences.Add(meshRefrence!);
                    return meshRefrence!;
                }
            }
            var mb = new MeshBuffer(mesh.Vertices.Length, mesh.Indices.Length);
            mb.TryAddMesh(mesh, out var m);
            MeshBuffers.Add(mb);
            SubMeshRefrences.Add(m!);
            return m!;
        }

        public static void RemoveMesh(SubMeshRefrence mesh)
        {
            SubMeshRefrences.Remove(mesh);
            for (int i = 0; i < MeshBuffers.Count; i++)
            {
                if (MeshBuffers[i].TryRemoveMesh(mesh))
                {
                    return;
                }
            }
        }
    }
}
