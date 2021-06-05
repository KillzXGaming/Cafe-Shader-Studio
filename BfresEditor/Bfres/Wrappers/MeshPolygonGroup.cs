using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;
using Toolbox.Core.IO;
using BfresLibrary;
using BfresLibrary.GX2;
using OpenTK.Graphics.OpenGL;

namespace BfresEditor
{
    /// <summary>
    /// Represents a wrapper for a mesh type to draw indices.
    /// </summary>
    public class MeshPolygonGroup : STPolygonGroup
    {
        public Shape ParentShape { get; set; }
        public Mesh Mesh { get; set; }

        public List<Bounding> Boundings = new List<Bounding>();
        public List<SubMesh> SubMeshes = new List<SubMesh>();

        public int Stride
        {
            get
            {
                return Mesh.IndexFormat == GX2IndexFormat.UInt16 ||
                       Mesh.IndexFormat == GX2IndexFormat.UInt16LittleEndian ? 2 : 4;
            }
        }

        //GL variables. Todo add type handling to toolbox core, gl converter to helper class in gl framework
        public DrawElementsType DrawElementsType = DrawElementsType.UnsignedInt;

        public MeshPolygonGroup(Shape shape, Mesh mesh, int boundingStartIndex) {
            ParentShape = shape;
            Mesh = mesh;
            Reload(shape, mesh);

            for (int i = 0; i < mesh.SubMeshes.Count; i++) {
                SubMeshes.Add(mesh.SubMeshes[i]);
                Boundings.Add(shape.SubMeshBoundings[boundingStartIndex + i]);
            }
        }

        public void Reload(Shape shape, Mesh mesh)
        {
            //Load indices into the group
            var indices = mesh.GetIndices().ToArray();

            Faces.Clear();
            for (int i = 0; i < indices.Length; i++)
                Faces.Add(indices[i]);

            if (!PrimitiveTypes.ContainsKey(mesh.PrimitiveType))
                throw new Exception($"Unsupported primitive type! {mesh.PrimitiveType}");

            //Set the primitive type
            PrimitiveType = PrimitiveTypes[mesh.PrimitiveType];;

            switch (Stride)
            {
                case 2: DrawElementsType = DrawElementsType.UnsignedShort; break;
                case 4: DrawElementsType = DrawElementsType.UnsignedInt; break;
                case 1: DrawElementsType = DrawElementsType.UnsignedByte; break;
            }
        }

        public byte[] GetIndices()
        {
            var mem = new System.IO.MemoryStream();
            using (var writer = new FileWriter(mem))
            {
                var faces = Mesh.GetIndices().ToArray();
                for (int i = 0; i < faces.Length; i++)
                {
                    uint index = Mesh.FirstVertex + faces[i];
                    switch (Stride)
                    {
                        case 1: writer.Write((byte)index); break;
                        case 2: writer.Write((ushort)index); break;
                        default: writer.Write((uint)index); break;
                    }
                }
            }
            return mem.ToArray();
        }


        //Converts bfres primitive types to generic types used for rendering.
        Dictionary<GX2PrimitiveType, STPrimitiveType> PrimitiveTypes = new Dictionary<GX2PrimitiveType, STPrimitiveType>()
        {
            { GX2PrimitiveType.Triangles, STPrimitiveType.Triangles },
            { GX2PrimitiveType.LineLoop, STPrimitiveType.LineLoop },
            { GX2PrimitiveType.Lines, STPrimitiveType.Lines },
            { GX2PrimitiveType.TriangleFan, STPrimitiveType.TriangleFans },
            { GX2PrimitiveType.Quads, STPrimitiveType.Quad },
            { GX2PrimitiveType.QuadStrip, STPrimitiveType.QuadStrips },
            { GX2PrimitiveType.TriangleStrip, STPrimitiveType.TriangleStrips },
        };
    }
}
