using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raysi
{
    internal class Mesh
    {
        public SubMesh[] SubMeshes;

        public Mesh(SubMesh[] subMeshes)
        {
            SubMeshes = subMeshes;
        }
    }
}
