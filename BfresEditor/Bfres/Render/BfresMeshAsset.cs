using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using MapStudio.Rendering;
using Toolbox.Core;
using GLFrameworkEngine;
using System.Diagnostics;

namespace BfresEditor
{
    public class BfresMeshAsset : GenericPickableMesh, IPickable
    {
        public GLTransform Transform { get; set; } = new GLTransform();

        public bool IsHovered { get; set; }

        private bool isSelected = false;
        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                this.Shape.MeshSelected?.Invoke(value, EventArgs.Empty);

                isSelected = value;
                for (int f = 0; f < PickableFaces.Count; f++)
                    PickableFaces[f].IsSelected = value;
            }
        }

        public void DragDropped(object droppedItem) {
            Shape.DragDropped(droppedItem);
        }

        public void DragDroppedOnLeave() {
            Shape.DragDroppedOnLeave();
        }
        public void DragDroppedOnEnter() {
            Shape.DragDroppedOnEnter();
        }

        public Dictionary<string, KeyGroup> KeyGroups = new Dictionary<string, KeyGroup>();

        public VertexBufferObject vao;
        public VertexBufferObject defaultVao;

        public FSHP Shape { get; set; }

        public bool IsVisible => Shape.IsVisible && Shape.Material.IsVisible;

        public List<VaoAttribute> Attributes;

        public bool UseColorBufferPass = false;

        public int BoneIndex = 0;

        public int Priority = 0;

        public int Vbo = -1;
        public int Ibo = -1;

        public bool IsSkybox = false;
        public bool IsCubeMap = false;
        public bool IsDepthShadow = false;
        public bool RenderInCubeMap = false;
        public bool IsSealPass = false;

        public int SubMeshLevel = 0;

        public int SkinCount = 0;
        public bool HasVertexColors = false;

        public Vector3 Center = new Vector3();

        public List<IPickable> PickableFaces = new List<IPickable>();

        public BfresMeshAsset(FSHP fshp)
        {
            Shape = fshp;
            MaterialAsset = new BfresMaterialAsset();

            this.Name = fshp.Name;
            this.Material = Shape.Material;
            this.SkinCount = (int)fshp.VertexSkinCount;
            this.BoneIndex = fshp.BoneIndex;
            fshp.UpdateViewBuffer = () => {
                UpdateVertexData = true;
            };
            CalculateBounding();

            //Load vaos
            int[] buffers = new int[2];
            GL.GenBuffers(2, buffers);

            int indexBuffer = buffers[0];
            int vaoBuffer = buffers[1];

            Vbo = vaoBuffer;
            Ibo = indexBuffer;
            vao = new VertexBufferObject(vaoBuffer, indexBuffer);
            defaultVao = new VertexBufferObject(vaoBuffer, indexBuffer);

            KeyGroups = fshp.LoadKeyGroups();
            Attributes = Shape.LoadGLAttributes();

            UpdateVertexBuffer();
            UpdateDefaultVaoAttributes();
        }

        public void UpdateDefaultVaoAttributes()
        {
            defaultVao.Clear();

            var strideTotal = Attributes.Sum(x => x.Stride);
            for (int i = 0; i < Attributes.Count; i++)
            {
                var att = Attributes[i];
                defaultVao.AddAttribute(
                    att.UniformName,
                    att.ElementCount,
                    att.Type,
                    false,
                    strideTotal,
                    att.Offset);
            }
            defaultVao.Initialize();
        }

        public void UpdateVaoAttributes()
        {
            vao.Clear();

            var strideTotal = Attributes.Sum(x => x.Stride);
            for (int i = 0; i < Attributes.Count; i++)
            {
                var att = Attributes[i];
                vao.AddAttribute(
                    att.UniformName,
                    att.ElementCount,
                    att.Type,
                    false,
                    strideTotal,
                    att.Offset);
            }
            vao.Initialize();
        }

        public void UpdateVaoAttributes(Dictionary<string, int> attributeToLocation)
        {
            vao.Clear();

            var strideTotal = Attributes.Sum(x => x.Stride);
            for (int i = 0; i < Attributes.Count; i++)
            {
                var att = Attributes[i];
                if (!attributeToLocation.ContainsKey(att.name))
                {
                    Console.WriteLine($"attributeToLocation does not contain {att.name}. skipping");
                    continue;
                }

                vao.AddAttribute(
                    attributeToLocation[att.name],
                    att.ElementCount,
                    att.Type,
                    false,
                    strideTotal,
                    att.Offset);
            }
            vao.Initialize();
        }

        public void UpdateVaoAttributes(Dictionary<string, string> attributeToUniform)
        {
            vao.Clear();

            var strideTotal = Attributes.Sum(x => x.Stride);
            for (int i = 0; i < Attributes.Count; i++)
            {
                var att = Attributes[i];
                if (!attributeToUniform.ContainsKey(att.name))
                    continue;

                vao.AddAttribute(
                    attributeToUniform[att.name],
                    att.ElementCount,
                    att.Type,
                    false,
                    strideTotal,
                    att.Offset);
            }

            vao.Initialize();
        }

        public void DrawColorPicking(GLContext control)
        {
            if (control.ColorPicker.PickingMode == ColorPicker.SelectionMode.Mesh)
                control.ColorPicker.SetPickingColor(this, control.CurrentShader);

            //Draw the mesh
            vao.Enable(control.CurrentShader);
            vao.Use();
            Draw();
        }

        public override void Render(GLContext control, ShaderProgram shader)
        {
            GL.Enable(EnableCap.PolygonOffsetFill);
            //Seal objects draw ontop of meshes so offset them
            if (IsSealPass)
                GL.PolygonOffset(-1, 1f);
            else
                GL.PolygonOffset(0, 0f);

            vao.Use();

            if (Runtime.RenderSettings.Wireframe)
                DrawModelWireframe(shader);
            else if (IsSelected)
                Draw();
            else
                Draw();

            GL.Disable(EnableCap.PolygonOffsetFill);

            //Reset to default depth settings after draw
            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Lequal);
            GL.DepthRange(0.0, 1.0);
            GL.DepthMask(true);
        }

        private void DrawOverlayWireframe(GLContext control, ShaderProgram shader)
        {
            Draw();

            var previousShader = control.CurrentShader;

            var selectionShader = GlobalShaders.GetShader("PICKING");
            control.CurrentShader = selectionShader;
            selectionShader.SetVector4("color", new Vector4(1, 1, 1, 1));

            GL.Enable(EnableCap.StencilTest);
            GL.PolygonMode(MaterialFace.Front, PolygonMode.Line);
            GL.Enable(EnableCap.LineSmooth);
            GL.LineWidth(1.5f);
            Draw();
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

            GL.Enable(EnableCap.DepthTest);

            control.CurrentShader = previousShader;
        }

        private void DrawModelWireframe(ShaderProgram shader)
        {
            // use vertex color for wireframe color
            GL.PolygonMode(MaterialFace.Front, PolygonMode.Line);
            GL.Enable(EnableCap.LineSmooth);
            GL.LineWidth(1.5f);
            Draw();
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
        }

        public void Draw()
        {
            GL.DrawElements(PrimitiveType.Triangles,
              Shape.PolygonGroups[SubMeshLevel].Faces.Count,
              DrawElementsType.UnsignedInt,
              Shape.PolygonGroups[SubMeshLevel].FaceOffset);
        }

        public bool UpdateVertexData { get; set; } = false;

        public override void UpdateVertexBuffer()
        {
            var indices = Shape.GetIndices();
            var bufferData = GetBufferData();

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, Ibo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

            GL.BindBuffer(BufferTarget.ArrayBuffer, Vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, bufferData.Length, bufferData, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            //Update attribute layout incase the attribute buffer data has been adjusted in size/offsets
            UpdateDefaultVaoAttributes();

            UpdateVertexData = false;
        }

        public Vector3[] MorphPositions = new Vector3[0];

        public byte[] GetBufferData()
        {
            var strideTotal = Attributes.Sum(x => x.Stride);

            var mem = new System.IO.MemoryStream();
            using (var writer = new Toolbox.Core.IO.FileWriter(mem))
            {
                for (int i = 0; i < Shape.Vertices.Count; i++)
                {
                    foreach (var attribute in Attributes)
                    {
                        if (attribute.Assigned)
                            continue;

                        writer.SeekBegin(attribute.Offset + (i * strideTotal));
                        switch (attribute.vertexAttributeName)
                        {
                            case "_p0":
                                {
                                    if (MorphPositions.Length > i)
                                        writer.Write(MorphPositions[i]);
                                    else
                                        writer.Write(Shape.Vertices[i].Position);
                                }
                                break;
                            case "_n0": writer.Write(Shape.Vertices[i].Normal); break;
                            case "_c0": writer.Write(Shape.Vertices[i].Colors[0]); break;
                            case "_c1": writer.Write(Shape.Vertices[i].Colors[1]); break;
                            case "_u0": writer.Write(Shape.Vertices[i].TexCoords[0]); break;
                            case "_u1":
                                if (Shape.Vertices[i].TexCoords.Length > 1)
                                    writer.Write(Shape.Vertices[i].TexCoords[1]);
                                break;
                            case "_u2":
                                if (Shape.Vertices[i].TexCoords.Length > 2)
                                    writer.Write(Shape.Vertices[i].TexCoords[2]); break;
                            case "_u3":
                                if (Shape.Vertices[i].TexCoords.Length > 3)
                                    writer.Write(Shape.Vertices[i].TexCoords[3]); break;
                            case "_u4": writer.Write(Shape.Vertices[i].TexCoords[4]); break;
                            case "_u5": writer.Write(Shape.Vertices[i].TexCoords[5]); break;
                            case "_u6": writer.Write(Shape.Vertices[i].TexCoords[6]); break;
                            case "_u7": writer.Write(Shape.Vertices[i].TexCoords[7]); break;
                            case "_t0": writer.Write(Shape.Vertices[i].Tangent); break;
                            case "_b0": writer.Write(Shape.Vertices[i].Bitangent); break;
                            case "_i0":
                            case "_i1":
                                {
                                    int startIndex = 0;
                                    if (attribute.vertexAttributeName == "_i1")
                                        startIndex = 4;

                                    for (int j = startIndex; j < startIndex + 4; j++)
                                    {
                                        if (j < Shape.Vertices[i].BoneIndices.Count)
                                            writer.Write(Shape.Vertices[i].BoneIndices[j]);
                                        else
                                            writer.Write(0.0f);
                                    }
                                }
                                break;
                            case "_w0":
                            case "_w1":
                                {
                                    int startIndex = 0;
                                    if (attribute.vertexAttributeName == "_w1")
                                        startIndex = 4;

                                    if (Shape.Vertices[i].BoneWeights.Count > 0)
                                    {
                                        for (int j = startIndex; j < startIndex + 4; j++)
                                        {
                                            if (j < Shape.Vertices[i].BoneWeights.Count)
                                                writer.Write(Shape.Vertices[i].BoneWeights[j]);
                                            else
                                                writer.Write(0.0f);
                                        }
                                    }
                                    else
                                        writer.Write(new Vector4(1, 0, 0, 0));
                                }
                                break;
                            //Hardcoded extras
                            case "inst0": writer.Write(new Vector4(1, 0, 0, 0)); break;
                            case "inst1": writer.Write(new Vector4(0, 1, 0, 0)); break;
                            case "inst2": writer.Write(new Vector4(0, 0, 1, 0)); break;
                        }
                    }
                }
            }
            return mem.ToArray();
        }

        public void CalculateBounding()
        {
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float minZ = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;
            float maxZ = float.MinValue;

            for (int i = 0; i < Shape.Vertices.Count; i++)
            {
                Vector3 positon = Shape.Vertices[i].Position;
                minX = Math.Min(minX, positon.X);
                minY = Math.Min(minY, positon.Y);
                minZ = Math.Min(minZ, positon.Z);
                maxX = Math.Max(maxX, positon.X);
                maxY = Math.Max(maxY, positon.Y);
                maxZ = Math.Max(maxZ, positon.Z);
            }

            Vector3 min = new Vector3(minX, minY, minZ);
            Vector3 max = new Vector3(maxX, maxY, maxZ);

            BoundingNode = new BoundingNode()
            {
                Radius = Shape.Shape.RadiusArray.FirstOrDefault(),
                Center = new Vector3(
                          Shape.Shape.SubMeshBoundings[0].Center.X,
                          Shape.Shape.SubMeshBoundings[0].Center.Y,
                          Shape.Shape.SubMeshBoundings[0].Center.Z),
            };
            BoundingNode.Box = BoundingBox.FromMinMax(min, max);
        }

        public override void Dispose()
        {
            vao.Dispose();
            defaultVao.Dispose();
            MaterialAsset?.Dispose();

            GL.DeleteBuffer(Vbo);
            GL.DeleteBuffer(Ibo);
        }
    }

    public class PolygonGroup
    {
        public int Offset;
        public int Count;
    }

    public class VaoAttribute
    {
        public string name;
        public string vertexAttributeName;
        public VertexAttribPointerType Type;
        public int ElementCount;

        public int Offset;

        public bool Assigned;

        public int Stride
        {
            get { return Assigned ? 0 : ElementCount * FormatSize(); }
        }

        //Todo convert via shader program attributes
        public virtual string UniformName
        {
            get
            {
                switch (name)
                {
                    case "_p0": return "vPositon";
                    case "_n0": return "vNormal";
                    case "_w0": return "vBoneWeight";
                    case "_i0": return "vBoneIndex";
                    case "_u0": return "vTexCoord0";
                    case "_u1": return "vTexCoord1";
                    case "_u2": return "vTexCoord2";
                    case "_c0": return "vColor";
                    case "_t0": return "vTangent";
                    case "_b0": return "vBitangent";
                    default:
                        return name;
                }
            }
        }

        private int FormatSize()
        {
            switch (Type)
            {
                case VertexAttribPointerType.Float: return sizeof(float);
                case VertexAttribPointerType.Byte: return sizeof(byte);
                case VertexAttribPointerType.Double: return sizeof(double);
                case VertexAttribPointerType.Int: return sizeof(int);
                case VertexAttribPointerType.Short: return sizeof(short);
                case VertexAttribPointerType.UnsignedShort: return sizeof(ushort);
                default: return 0;
            }
        }
    }
}
