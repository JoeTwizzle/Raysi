using System;
using System.Collections.Generic;
using System.Text;

namespace Raysi
{
    public class SubMeshRefrence
    {
        public readonly MeshBuffer MeshBuffer;
        public readonly int VertexStartIndex;
        public readonly int VertexCount;
        public readonly int IndexStartIndex;
        public readonly int IndexCount;
        public readonly MemoryMarker VertexMarker;
        public readonly MemoryMarker IndexMarker;

        internal SubMeshRefrence(MeshBuffer meshBuffer, int vertexStartIndex, int vertexCount, int indexStartIndex, int indexCount, MemoryMarker vertexMarker, MemoryMarker indexMarker)
        {
            MeshBuffer = meshBuffer;
            VertexStartIndex = vertexStartIndex;
            VertexCount = vertexCount;
            IndexStartIndex = indexStartIndex;
            IndexCount = indexCount;
            VertexMarker = vertexMarker;
            IndexMarker = indexMarker;
        }
    }
}
