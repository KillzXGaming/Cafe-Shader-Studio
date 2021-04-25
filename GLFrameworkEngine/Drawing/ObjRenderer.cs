using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using OpenTK;
using System.Globalization;
using OpenTK.Graphics.OpenGL;

namespace GLFrameworkEngine
{
    public class ObjRenderer
    {
        // ---- CONSTANTS ----------------------------------------------------------------------------------------------

        private static readonly char[] _argSeparators = new char[] { ' ' };
        private static readonly char[] _vertexSeparators = new char[] { '/' };

        // ---- PROPERTIES ----------------------------------------------------------------------------------------------

        public List<ObjMesh> Meshes { get; set; }

        public Dictionary<string, ObjMaterial> Materials = new Dictionary<string, ObjMaterial>();
        public Dictionary<string, GLTexture> Textures = new Dictionary<string, GLTexture>();

        private bool initialized = false;
        public void Init()
        {
            if (initialized)
                return;

            foreach (var mesh in Meshes) {
                foreach (var polygon in mesh.Polygons.Values)
                    polygon.Init();
            }
            initialized = true;
        }

        // ---- METHODS (PUBLIC) ----------------------------------------------------------------------------------------------

        public void Draw(GLContext context)
        {
            Init();

            foreach (var mesh in Meshes) {
                foreach (var poly in mesh.Polygons.Values) {
                    if (Materials.ContainsKey(poly.Material)) {
                        var mat = Materials[poly.Material];
                        LoadMaterials(mat);
                    }

                    poly.Vao.Use();
                    GL.DrawElements(PrimitiveType.Triangles, 0, DrawElementsType.UnsignedInt, poly.Indices.Count);
                }
            }
        }

        private void LoadMaterials(ObjMaterial material)
        {

        }

        public class ObjMesh
        {
            public Dictionary<string, ObjPolygon> Polygons = new Dictionary<string, ObjPolygon>();

            public string Name { get; set; }

            public ObjMesh(string name) {
                Name = name;
            }
        }

        public class ObjPolygon
        {
            public VertexBufferObject Vao { get; set; }

            /// <summary>
            /// The material used for this face.
            /// </summary>
            public string Material { get; set; }

            /// <summary>
            /// Gets or sets the list of faces of the mesh.
            /// </summary>
            public List<ObjFace> Faces { get; set; }

            public List<int> Indices = new List<int>();

            public ObjPolygon(string material)
            {
                Material = material;
                Faces = new List<ObjFace>();
            }

            public void Init()
            {
                Vao = new VertexBufferObject();
                Vao.AddAttribute(0, 3, VertexAttribPointerType.Float, false, 32, 0);
                Vao.AddAttribute(1, 3, VertexAttribPointerType.Float, false, 32, 12);
                Vao.AddAttribute(2, 2, VertexAttribPointerType.Float, false, 32, 24);
                Vao.Initialize();

                UpdateVertexData();
            }

            public void UpdateVertexData()
            {
                List<float> list = new List<float>();
                for (int i = 0; i < Faces.Count; i++) {
                    for (int v = 0; v < Faces[i].Vertices.Length; v++) {
                        var vertex = Faces[i].Vertices[v];
                        list.Add(vertex.Position.X);
                        list.Add(vertex.Position.Y);
                        list.Add(vertex.Position.Z);
                        list.Add(vertex.Normal.X);
                        list.Add(vertex.Normal.Y);
                        list.Add(vertex.Normal.Z);
                        list.Add(vertex.TexCoord.X);
                        list.Add(vertex.TexCoord.Y);
                    }
                }

                Vao.Bind();

                float[] data = list.ToArray();
                GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * data.Length, data, BufferUsageHint.StaticDraw);

                int[] indices = Indices.ToArray();
                GL.BufferData(BufferTarget.ElementArrayBuffer, sizeof(int) * indices.Length, indices, BufferUsageHint.StaticDraw);
            }
        }

        public struct ObjFace
        {
            /// <summary>
            /// The three <see cref="ObjVertex"/> vertices which define this triangle.
            /// </summary>
            public ObjVertex[] Vertices;

            public ObjFace(ObjFace face) {
                Vertices = face.Vertices;
            }
        }

        /// <summary>
        /// Represents the indices required to define a vertex of an <see cref="ObjModel"/>.
        /// </summary>
        public struct ObjVertex
        {
            // ---- FIELDS -------------------------------------------------------------------------------------------------

            /// <summary>
            /// The vertex position from the positions array of the owning <see cref="ObjModel"/>.
            /// </summary>
            public Vector3 Position;

            /// <summary>
            /// The vertex texture coordinates from the texture coordinate array of the owning <see cref="ObjModel"/>.
            /// </summary>
            public Vector2 TexCoord;

            /// <summary>
            /// The vertex normal from the normal array of the owning <see cref="ObjModel"/>.
            /// </summary>
            public Vector3 Normal;

            public override string ToString()
            {
                return $"{Position}_{Normal}";
            }
        }

        /// <summary>
        /// Represents a material in an <see cref="ObjModel"/>.
        /// </summary>
        public class ObjMaterial
        {
            /// <summary>
            /// Gets or sets the name of the material.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Gets or sets the diffuse texture.
            /// </summary>
            public string DiffuseTexture { get; set; }

            /// <summary>
            /// Gets or sets the diffuse color.
            /// </summary>
            public Vector3 Diffuse { get; set; }

            /// <summary>
            /// Gets or sets the amient color.
            /// </summary>
            public Vector3 Ambient { get; set; }

            /// <summary>
            /// Gets or sets the specular color.
            /// </summary>
            public Vector3 Specular { get; set; }
        }

        public void LoadObj(Stream objStream, Stream mtlStream) {
            ParseMtl(mtlStream);
            ParseObj(objStream);
        }

        private void ParseMtl(Stream stream)
        {
            using (StreamReader reader = new StreamReader(stream, Encoding.Default))
            {
                ObjMaterial currentMaterial = null;
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    // Ignore empty lines and comments.
                    if (String.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

                    string[] args = line.Split(_argSeparators, StringSplitOptions.RemoveEmptyEntries);
                    switch (args[0])
                    {
                        case "newmtl": //New Material
                            currentMaterial = new ObjMaterial();
                            currentMaterial.Name = args[1];
                            Materials.Add(currentMaterial.Name, currentMaterial);
                            break;
                        case "Ka": //Ambient Color
                            currentMaterial.Ambient = new Vector3(
                                float.Parse(args[1]),
                                float.Parse(args[2]),
                                float.Parse(args[3]));
                            break;
                        case "Kd": //Diffuse Color
                            currentMaterial.Diffuse = new Vector3(
                                float.Parse(args[1]),
                                float.Parse(args[2]),
                                float.Parse(args[3]));
                            break;
                        case "Ks": //Specular Color
                            currentMaterial.Specular = new Vector3(
                                float.Parse(args[1]),
                                float.Parse(args[2]),
                                float.Parse(args[3]));
                            break;
                        case "map_Kd ": //Diffuse Map
                            currentMaterial.DiffuseTexture = args[1];
                            break;
                    }
                }
            }
        }

        private void ParseObj(Stream stream)
        {
            ObjMesh currentMesh = null;
            ObjPolygon currentPolygon = null;
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                List<Vector3> Positions = new List<Vector3>();
                List<Vector2> TexCoords = new List<Vector2>();
                List<Vector3> Normals = new List<Vector3>();

                var enusculture = new CultureInfo("en-US");
                string currentMaterial = null;
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    line = line.Replace(",", ".");

                    // Ignore empty lines and comments.
                    if (String.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

                    string[] args = line.Split(_argSeparators, StringSplitOptions.RemoveEmptyEntries);
                    if (args.Length == 1)
                        continue;

                    switch (args[0])
                    {
                        case "o":
                        case "g":
                            currentMesh = new ObjMesh(args.Length > 1 ? args[1] : $"Mesh{Meshes.Count}");
                            Meshes.Add(currentMesh);
                            continue;
                        case "v":
                            Positions.Add(new Vector3(
                                Single.Parse(args[1], enusculture),
                                Single.Parse(args[2], enusculture),
                                Single.Parse(args[3], enusculture)));
                            continue;
                        case "vt":
                            TexCoords.Add(new Vector2(
                                Single.Parse(args[1], enusculture),
                                Single.Parse(args[2], enusculture)));
                            continue;
                        case "vn":
                            Normals.Add(new Vector3(
                                Single.Parse(args[1], enusculture),
                                Single.Parse(args[2], enusculture),
                                Single.Parse(args[3], enusculture)));
                            continue;
                        case "f":
                            // Only support triangles for now.
                            if (args.Length != 4)
                                throw new Exception("Obj must be trianglulated!");

                            int[] indices = new int[3 * 2]; //3 faces, position and normal indices

                            if (!currentMesh.Polygons.ContainsKey(currentMaterial)) {
                                currentPolygon = new ObjPolygon(currentMaterial);
                                currentMesh.Polygons.Add(currentMaterial, currentPolygon);
                            }

                            ObjFace face = new ObjFace() { Vertices = new ObjVertex[3] };
                            for (int i = 0; i < 3; i++)
                            {
                                string[] vertexArgs = args[i + 1].Split(_vertexSeparators, StringSplitOptions.None);
                                int positionIndex = Int32.Parse(vertexArgs[0]) - 1;

                                face.Vertices[i].Position = Positions[positionIndex];

                                currentPolygon.Indices.Add(positionIndex);

                                if (float.IsNaN(face.Vertices[i].Position.X) ||
                                    float.IsNaN(face.Vertices[i].Position.Y) ||
                                    float.IsNaN(face.Vertices[i].Position.Z))
                                {
                                    face.Vertices = null;
                                    break;
                                }

                                if (vertexArgs.Length > 1 && vertexArgs[1] != String.Empty) {
                                    face.Vertices[i].TexCoord = TexCoords[Int32.Parse(vertexArgs[1]) - 1];
                                }
                                if (vertexArgs.Length > 2 && vertexArgs[2] != String.Empty) {
                                    face.Vertices[i].Normal = Normals[Int32.Parse(vertexArgs[2]) - 1];
                                }
                            }
                            currentPolygon.Faces.Add(face);
                            continue;
                        case "usemtl":
                            {
                                if (args.Length < 2) continue;
                                currentMaterial = args[1];
                                continue;
                            }
                    }
                }
            }
        }
    }
}
