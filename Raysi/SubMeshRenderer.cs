using OpenTK.Graphics.OpenGL4;
using GLGraphics;
using System;
using System.Collections.Generic;
using OpenTK.Mathematics;

namespace Raysi
{
    public class SubMeshRenderer : NamedObject
    {
        public MeshRenderer MeshRenderer { get; internal set; }
        public SubMeshRefrence Mesh;

        public SubMeshRenderer()
        {

        }

        public void OnEnable()
        {

        }

        public void OnDisable()
        {

        }

        public void Draw()
        {
            //RenderCore.SubmitDrawCall(Mesh);
        }
    }
}
