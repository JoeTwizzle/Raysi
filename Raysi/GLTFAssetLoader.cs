using System;
using System.Collections.Generic;
using System.Linq;
using SharpGLTF.Transforms;
using SharpGLTF.Schema2;
using SharpGLTF.Geometry;
using SharpGLTF.Runtime;
using System.Numerics;
using GLGraphics;
using System.Runtime.CompilerServices;
using System.IO;

namespace Raysi
{
    public static class GLTFAssetLoader
    {
        public static Prefab Load(string path)
        {
            return Load(path, Vector3.One);
        }

        public static Prefab Load(string path, float scale = 1f)
        {
            return Load(path, new Vector3(scale));
        }

        public static Prefab Load(string path, Vector3 scale)
        {
            ModelRoot root = ModelRoot.Load(path);

            //var textures = GenerateTextures(root);
            //var samplers = GenerateSamplers(root);
            //var materials = GenerateMaterials(root, textures, samplers);
            var meshes = GenerateMeshes(root);
            var rootGO = GenerateSceneGraph(root, meshes);
            return Prefab.Create(rootGO);
        }

        static Sampler[] GenerateSamplers(ModelRoot root)
        {
            Sampler[] samplers = new Sampler[root.LogicalTextureSamplers.Count];
            foreach (var sampler in root.LogicalTextureSamplers)
            {
                var samp = new Sampler();
                samp.Filter = GetFilter(sampler.MinFilter);
                samp.WrapModeU = (OpenTK.Graphics.OpenGL4.TextureWrapMode)sampler.WrapS;
                samp.WrapModeV = (OpenTK.Graphics.OpenGL4.TextureWrapMode)sampler.WrapT;
                samplers[sampler.LogicalIndex] = samp;
            }
            return samplers;
        }

        static Texture2D[] GenerateTextures(ModelRoot root)
        {
            Texture2D[] textures = new Texture2D[root.LogicalTextures.Count];
            foreach (var texture in root.LogicalTextures)
            {
                var bytes = texture.PrimaryImage.Content.Content.ToArray();
                //textures[texture.LogicalIndex] = TextureLoader.LoadTexture2D(bytes, false);
                textures[texture.LogicalIndex].SetLabel(texture.Name ?? "Unnamed Texture");
            }
            return textures;
        }


        static OpenTK.Graphics.OpenGL4.PixelFormat GetPixelFormat(byte channels)
        {
            return channels switch
            {
                1 => OpenTK.Graphics.OpenGL4.PixelFormat.Red,
                2 => OpenTK.Graphics.OpenGL4.PixelFormat.Rg,
                3 => OpenTK.Graphics.OpenGL4.PixelFormat.Rgb,
                4 => OpenTK.Graphics.OpenGL4.PixelFormat.Rgba,
                _ => OpenTK.Graphics.OpenGL4.PixelFormat.Rgba
            };
        }

        static TextureFilter GetFilter(TextureMipMapFilter min)
        {
            return min switch
            {
                TextureMipMapFilter.NEAREST => TextureFilter.Nearest,
                TextureMipMapFilter.LINEAR => TextureFilter.Bilinear,
                TextureMipMapFilter.NEAREST_MIPMAP_LINEAR => TextureFilter.Nearest,
                TextureMipMapFilter.NEAREST_MIPMAP_NEAREST => TextureFilter.Nearest,
                TextureMipMapFilter.LINEAR_MIPMAP_NEAREST => TextureFilter.Bilinear,
                TextureMipMapFilter.LINEAR_MIPMAP_LINEAR => TextureFilter.Trilinear,
                TextureMipMapFilter.DEFAULT => TextureFilter.Trilinear,
                _ => TextureFilter.Trilinear
            };
        }




        static Texture2D GetTexture(in MaterialChannel channel, Texture2D[] textures, Sampler[] samplers)
        {
            Texture2D texture;
            if (channel.Texture != null)
            {
                texture = textures[channel.Texture.LogicalIndex];
                var sampler = samplers[channel.TextureSampler.LogicalIndex];
                //return new Texture(texture, sampler);
                return null;
            }
            else
            {
                texture = new Texture2D();
                texture.Init(1, 1);
                if (channel.Key == "Normal")
                {
                    texture.SetPixel(new Vector3(0.5f, 0.5f, 1), 0, 0, GetPixelFormat(3), OpenTK.Graphics.OpenGL4.PixelType.Float);
                }
                else
                {
                    texture.SetPixel(channel.Parameter, 0, 0, GetPixelFormat(4), OpenTK.Graphics.OpenGL4.PixelType.Float);
                }

                return texture;
            }
        }

        static GameObject GenerateSceneGraph(ModelRoot root, Mesh[] meshes)
        {
            var Root = new GameObject(root.DefaultScene.Name);
            foreach (Node node in root.DefaultScene.VisualChildren)
            {
                TraverseNodes(Root, node, meshes);
            }
            return Root;
        }

        static void TraverseNodes(GameObject parent, Node node, Mesh[] meshes)
        {
            var nodeGO = new GameObject(node.Name);
            nodeGO.Transform.Parent = parent.Transform;
            nodeGO.Transform.WorldMatrix = node.WorldMatrix.ToOpenTK();
            if (node.Mesh != null)
            {
                var mesh = meshes[node.Mesh.LogicalIndex];
                SubMeshRenderer[] subMeshRenderers = new SubMeshRenderer[mesh.SubMeshes.Length];
                for (int i = 0; i < mesh.SubMeshes.Length; i++)
                {
                    SubMeshRenderer subRenderer = new SubMeshRenderer();
                    subRenderer.Mesh = RenderCore.AddMesh(mesh.SubMeshes[i]);
                    subMeshRenderers[i] = subRenderer;
                }
                MeshRenderer renderer = new MeshRenderer(subMeshRenderers);
                nodeGO.AddScript(renderer);
            }
            foreach (Node subNode in node.VisualChildren)
            {
                TraverseNodes(nodeGO, subNode, meshes);
            }
        }

        static Mesh[] GenerateMeshes(ModelRoot root)
        {
            Mesh[] meshes = new Mesh[root.LogicalMeshes.Count];
            int i = 0;
            foreach (var mesh in root.LogicalMeshes)
            {
                SubMesh[] subMeshes = new SubMesh[mesh.Primitives.Count];
                int j = 0;
                foreach (var primitive in mesh.Primitives)
                {
                    subMeshes[j] = GenerateSubMeshes(primitive);
                    j++;
                }
                meshes[i] = new Mesh(subMeshes);
                i++;
            }
            return meshes;
        }

        static SubMesh GenerateSubMeshes(MeshPrimitive primitive)
        {
            IList<Vector3> positions;
            IList<Vector3>? normals = null;
            IList<Vector4>? tangents = null;
            IList<Vector2>? texCoord0 = null;
            IList<Vector2>? texCoord1 = null;
            IList<Vector2>? texCoord2 = null;
            IList<Vector2>? texCoord3 = null;
            primitive.VertexAccessors.TryGetValue("POSITION", out var pos);
            positions = pos!.AsVector3Array();
            if (primitive.VertexAccessors.TryGetValue("NORMAL", out var norm))
            {
                normals = norm.AsVector3Array();
            }
            if (primitive.VertexAccessors.TryGetValue("TANGENT", out var tan))
            {
                tangents = tan.AsVector4Array();
            }
            if (primitive.VertexAccessors.TryGetValue("TEXCOORD_0", out var tex0))
            {
                texCoord0 = tex0.AsVector2Array();
            }
            if (primitive.VertexAccessors.TryGetValue("TEXCOORD_1", out var tex1))
            {
                texCoord1 = tex1.AsVector2Array();
            }
            if (primitive.VertexAccessors.TryGetValue("TEXCOORD_2", out var tex2))
            {
                texCoord2 = tex2.AsVector2Array();
            }
            if (primitive.VertexAccessors.TryGetValue("TEXCOORD_3", out var tex3))
            {
                texCoord3 = tex3.AsVector2Array();
            }
            var verts = new Vertex[positions.Count];
            bool hasNormals = normals != null;
            bool hasTangents = tangents != null;
            bool hasTexCoord0 = texCoord0 != null;
            bool hasTexCoord1 = texCoord1 != null;
            bool hasTexCoord2 = texCoord2 != null;
            bool hasTexCoord3 = texCoord3 != null;

            Vector3 fakePos = Vector3.Zero;
            Vector2 fakeUV = Vector2.Zero;
            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 normal = hasNormals ? normals![i] : fakePos;
                Vector3 tang = hasTangents ? new Vector3(tangents![i].X, tangents![i].Y, tangents![i].Z) : fakePos;
                Vector3 biTan = hasTangents && hasNormals ? (Vector3.Cross(normal, tang) * tangents![i].W) : fakePos;
                Vector2 uv0 = hasTexCoord0 ? texCoord0![i] : fakeUV;
                Vector2 uv1 = hasTexCoord1 ? texCoord1![i] : fakeUV;
                Vector2 uv2 = hasTexCoord2 ? texCoord2![i] : fakeUV;
                Vector2 uv3 = hasTexCoord3 ? texCoord3![i] : fakeUV;
                verts[i] = new Vertex(positions[i], normal, uv0);
            }

            return new SubMesh(verts, primitive.IndexAccessor.AsIndicesArray().ToArray());
        }

        //static MeshVertex EvaluateVertex(IVertexBuilder vertexBuilder)
        //{
        //    var vertexGeometry = vertexBuilder.GetGeometry();
        //    vertexGeometry.TryGetNormal(out Vector3 normal);
        //    vertexGeometry.TryGetTangent(out Vector4 tangent);
        //    OpenTK.Mathematics.Vector3 pos = vertexGeometry.GetPosition().ToOpenTK();
        //    OpenTK.Mathematics.Vector3 tan = tangent.ToOpenTK().Xyz;
        //    OpenTK.Mathematics.Vector3 norm = normal.ToOpenTK();
        //    OpenTK.Mathematics.Vector3 biTan = OpenTK.Mathematics.Vector3.Cross(norm, tan) * tangent.W;

        //    var vertexMaterial = vertexBuilder.GetMaterial();
        //    int maxUVs = vertexMaterial.MaxTextCoords;
        //    Span<OpenTK.Mathematics.Vector2> uvs = stackalloc OpenTK.Mathematics.Vector2[4];
        //    for (int i = 0; i < maxUVs && i < 4; i++)
        //    {
        //        uvs[i] = vertexMaterial.GetTexCoord(i).ToOpenTK();
        //    }

        //    return new MeshVertex(pos, norm, tan, biTan, uvs[0], uvs[1], uvs[2], uvs[3]);
        //}
    }
}
