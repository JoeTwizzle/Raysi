using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace Raysi
{
    public class SubMesh
    {
        public Type VertexType { get; private set; } = typeof(Vertex);
        public Vertex[] Vertices { get; private set; }
        public uint[] Indices { get; private set; }

        public SubMesh(Vertex[] vertices, uint[] indices)
        {
            Vertices = vertices;
            Indices = indices;
        }
    }
}

