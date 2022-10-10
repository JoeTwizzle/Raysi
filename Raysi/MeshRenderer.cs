using OpenTK.Mathematics;
using System.Collections.Generic;
using System.Linq;
using GLGraphics;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace Raysi
{
    public class MeshRenderer : GameScript
    {
        public MeshRenderer(SubMeshRenderer[] subMeshRenderers)
        {
            SubMeshRenderers = subMeshRenderers;
            for (int i = 0; i < subMeshRenderers.Length; i++)
            {
                subMeshRenderers[i].MeshRenderer = this;
            }
        }

        public SubMeshRenderer[] SubMeshRenderers { get; private set; }

        public override void OnEnable()
        {
            for (int i = 0; i < SubMeshRenderers.Length; i++)
            {
                SubMeshRenderers[i].OnEnable();
            }
        }

        public override void OnDisable()
        {
            for (int i = 0; i < SubMeshRenderers.Length; i++)
            {
                SubMeshRenderers[i].OnDisable();
            }
        }

        public override void Draw()
        {
            for (int i = 0; i < SubMeshRenderers.Length; i++)
            {
                SubMeshRenderers[i].Draw();
            }
        }
    }
}
