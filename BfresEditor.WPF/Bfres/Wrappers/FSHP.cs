using System;
using System.Collections.Generic;
using System.Linq;
using Toolbox.Core;
using BfresLibrary;
using BfresLibrary.GX2;
using BfresLibrary.Helpers;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Toolbox.Core.ViewModels;
using CafeStudio.UI;

namespace BfresEditor
{
    public class FSHP : STGenericMesh, IRenamableNode, IPropertyUI,
        ICheckableNode, IContextMenu
    {
        /// <summary>
        /// Determines if the mesh is visible in the viewer.
        /// </summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// The shape section storing face indices and bounding data.
        /// </summary>
        public Shape Shape { get; set; }

        /// <summary>
        /// The vertex buffer storing vertex data and attributes.
        /// </summary>
        public VertexBuffer VertexBuffer { get; set; }

        /// <summary>
        /// The model in which the data in this section is parented to.
        /// </summary>
        public FMDL ParentModel { get; set; }

        /// <summary>
        /// The material data mapped to the mesh.
        /// </summary>
        public FMAT Material { get; set; }

        /// <summary>
        /// The skeleton used in the parent model.
        /// </summary>
        public FSKL ParentSkeleton { get; set; }

        /// <summary>
        /// The file in which the data in this section is parented to.
        /// </summary>
        public ResFile ParentFile { get; set; }

        /// <summary>
        /// The type of VaoAttribute to create to render attribute uniforms.
        /// </summary>
        public Type VaoAttributeType { get; set; } = typeof(VaoAttribute);

        public Action UpdateViewBuffer;

        /// <summary>
        /// The index of the LOD mesh to display.
        /// </summary>
        public int DisplayLOD { get; set; }

        public override string Name
        {
            get { return Shape.Name; }
            set { Shape.Name = value; }
        }

        public void Renamed(string text) {
            this.Name = text;
        }

        public void OnChecked(bool visible) {
            this.IsVisible = visible;
        }

        public bool HasValidShader = true;

        public EventHandler ShaderReload;

        public void ReloadShader() {
            ShaderReload?.Invoke(this, EventArgs.Empty);
        }

        public Type GetTypeUI() => typeof(BfresMeshEditor);

        public void OnLoadUI(object uiInstance)
        {

        }

        public void OnRenderUI(object uiInstance) {
            var editor = (BfresMeshEditor)uiInstance;
            editor.LoadEditor(this);
        }

        private FMAT beforeDroppedMaterial = null;

        public void DragDroppedOnLeave() {
            if (beforeDroppedMaterial != null)
                AssignMaterial(beforeDroppedMaterial);
        }

        public void DragDroppedOnEnter() {
            beforeDroppedMaterial = this.Material;
        }

        public void DragDropped(object droppedItem) {
            if (droppedItem is FMAT)
                AssignMaterial((FMAT)droppedItem);
        }

        private void AssignMaterial(int index)
        {
            Shape.MaterialIndex = (ushort)index;
            Material = (FMAT)ParentModel.Materials[index];
        }

        private void AssignMaterial(FMAT material)
        {
            int index = ParentModel.Materials.IndexOf(material);
            Shape.MaterialIndex = (ushort)index;
            Material = material;
        }

        public string GetRenameText() => this.Name;

        public MenuItemModel[] GetContextMenuItems()
        {
            List<MenuItemModel> items = new List<MenuItemModel>();
            var lodMenu = new MenuItemModel("Level Of Detail");
            items.Add(lodMenu);
            var uvMenu = new MenuItemModel("UVs");
            uvMenu.MenuItems.Add(new MenuItemModel("Flip (Vertical)", FlipUVsVerticalAction));
            uvMenu.MenuItems.Add(new MenuItemModel("Flip (Horizontal)", FlipUVsHorizontalAction));
            items.Add(uvMenu);
            var normalsMenu = new MenuItemModel("Normals");
            items.Add(normalsMenu);
            var colorsMenu = new MenuItemModel("Colors");
            items.Add(colorsMenu);
            items.Add(new MenuItemModel("RecalculateTangents", RecalculateTangentsAction));
            return items.ToArray();
        }

        #region Events

        public FileFilter[] ImportFilter { get; }
        public FileFilter[] ExportFilter { get; }

        public void Import(string fileName)
        {

        }

        public void Export(string fileName)
        {

        }

        private void ExportAction(object sender, EventArgs e)
        {

        }

        private void ReplaceAction(object sender, EventArgs e) {

        }

        private void FlipUVsVerticalAction(object sender, EventArgs e) {
            this.FlipUvsVertical();
            UpdateRender();
        }

        private void FlipUVsHorizontalAction(object sender, EventArgs e) {
            this.FlipUvsHorizontal();
            UpdateRender();
        }

        private void RecalculateTangentsAction(object sender, EventArgs e)
        {

        }

        private void UpdateRender() {
            //Update the render buffer
            UpdateViewBuffer();
            //Update the bfres data buffer
            UpdateBfresVertexBuffer();
            //Show updates to viewport
            UIHelper.UpdateViewport();
        }

        #endregion

        public FSHP(ResFile resFile, FSKL skeleton, FMDL model, Shape shape) {
            ParentFile = resFile;
            ParentModel = model;
            Shape = shape;
            ParentSkeleton = skeleton;
            BoneIndex = shape.BoneIndex;
            VertexBuffer = model.Model.VertexBuffers[shape.VertexBufferIndex];
            VertexSkinCount = shape.VertexSkinCount;

            Name = shape.Name;

            UpdateVertexBuffer();
            UpdatePolygonGroups();
        }

        public  void UpdateTransform()
        {
            int boneIndex = Shape.BoneIndex;
            var bone = ParentSkeleton.Bones[boneIndex];
            this.Transform = new ModelTransform()
            {
                Position = bone.AnimationController.Position,
                Rotation = bone.AnimationController.Rotation,
                Scale = bone.AnimationController.Scale,
            };
            this.Transform.Update();
        }

        public uint[] GetIndices()
        {
            List<uint> faces = new List<uint>();
            foreach (var mesh in Shape.Meshes)
            {
                var lodFaces = mesh.GetIndices().ToArray();
                for (int i = 0; i < lodFaces.Length; i++)
                    faces.Add(lodFaces[i] + mesh.FirstVertex);
            }
            return faces.ToArray();
        }

        public Dictionary<string, KeyGroup> LoadKeyGroups()
        {
            //Load the vertex buffer into the helper to easily access the data.
            VertexBufferHelper helper = new VertexBufferHelper(VertexBuffer, ParentFile.ByteOrder);

            int index = 0;

            Dictionary<string, KeyGroup> keyGroups = new Dictionary<string, KeyGroup>();
            foreach (var keyShape in Shape.KeyShapes)
            {
                var group = new KeyGroup();

                //Loop through all the vertex data and load it into our vertex data
                group.Vertices = new List<STVertex>();

                //Get all the necessary attributes
                var positions = TryGetValues(helper, $"_p{index}");
                for (int i = 0; i < positions.Length; i++)
                {
                    var vertex = new STVertex();
                    vertex.Position = new Vector3(positions[i].X, positions[i].Y, positions[i].Z);
                    group.Vertices.Add(vertex);
                }
                if (group.Vertices.Count > 0)
                    keyGroups.Add(keyShape.Key, group);

                index++;
            }
            return keyGroups;
        }

        /// <summary>
        /// Updates the current buffer from the shapes vertex buffer data.
        /// </summary>
        public void UpdateVertexBuffer(bool weighPositions = false)
        {
            //Load the vertex buffer into the helper to easily access the data.
            VertexBufferHelper helper = new VertexBufferHelper(VertexBuffer, ParentFile.ByteOrder);

            //Loop through all the vertex data and load it into our vertex data
            Vertices = new List<STVertex>();

            //Get all the necessary attributes
            var positions = TryGetValues(helper, "_p0");
            var normals = TryGetValues(helper, "_n0");
            var texCoords = TryGetChannelValues(helper, "_u");
            var colors = TryGetChannelValues(helper, "_c");
            var tangents = TryGetValues(helper, "_t0");
            var bitangents = TryGetValues(helper, "_b0");
            var weights0 = TryGetValues(helper, "_w0");
            var indices0 = TryGetValues(helper, "_i0");
            var weights1 = TryGetValues(helper, "_w1");
            var indices1 = TryGetValues(helper, "_i1");

            var boneIndexList = ParentSkeleton.Skeleton.MatrixToBoneList;

            float scale = GLFrameworkEngine.GLContext.PreviewScale;

            //Get the position attribute and use the length for the vertex count
            for (int v = 0; v < positions.Length; v++)
            {
                STVertex vertex = new STVertex();
                Vertices.Add(vertex);

                vertex.Position = new Vector3(positions[v].X,positions[v].Y,positions[v].Z) * scale;
                if (normals.Length > 0)
                    vertex.Normal = new Vector3(normals[v].X,normals[v].Y,normals[v].Z);

                if (texCoords.Length > 0)
                {
                    vertex.TexCoords = new Vector2[texCoords.Length];
                    for (int i = 0; i < texCoords.Length; i++)
                        vertex.TexCoords[i] = new Vector2(texCoords[i][v].X, texCoords[i][v].Y);
                }
                if (colors.Length > 0)
                {
                    vertex.Colors = new Vector4[colors.Length];
                    for (int i = 0; i < colors.Length; i++)
                        vertex.Colors[i] = new Vector4(
                            colors[i][v].X, colors[i][v].Y,
                            colors[i][v].Z, colors[i][v].W);
                }

                if (tangents.Length > 0)
                    vertex.Tangent = new Vector4(tangents[v].X, tangents[v].Y, tangents[v].Z, tangents[v].W);
                if (bitangents.Length > 0)
                    vertex.Bitangent = new Vector4(bitangents[v].X, bitangents[v].Y, bitangents[v].Z, bitangents[v].W);

                if (VertexSkinCount == 0 && weighPositions)
                {
                    var bone = ParentSkeleton.Bones[Shape.BoneIndex];
                    vertex.Position = Vector3.TransformPosition(vertex.Position, bone.Transform);
                    vertex.Normal = Vector3.TransformNormal(vertex.Normal, bone.Transform);
                }

                for (int i = 0; i < VertexBuffer.VertexSkinCount; i++)
                {
                    int index = -1;
                    float weight = 0.0f;

                    if (i > 3 && indices1.Length > 0)
                    {
                        index = boneIndexList[(int)indices1[v][i - 4]];
                        weight = weights1.Length > 0 ? weights1[v][i - 4] : 1.0f;
                    }
                    else if (i <= 3)
                    {
                        index = boneIndexList[(int)indices0[v][i]];
                        weight = weights0.Length > 0 ? weights0[v][i] : 1.0f;
                    }
                    else
                        break;

                    vertex.BoneIndices.Add(index);
                    vertex.BoneWeights.Add(weight);

                    if (VertexSkinCount == 1 && weighPositions)
                    {
                        var bone = ParentSkeleton.Bones[index];
                        vertex.Position = Vector3.TransformPosition(vertex.Position, bone.Transform);
                        vertex.Normal = Vector3.TransformNormal(vertex.Normal, bone.Transform);
                    }
                }
            }
        }

        /// <summary>
        /// Updates the vertex data in the bfres of the currently set attributes and vertex data
        /// </summary>
        public void UpdateBfresVertexBuffer(bool weighPositions = true)
        {
            //Load the vertex buffer into the helper to easily access the data.
            VertexBufferHelper helper = new VertexBufferHelper(VertexBuffer, ParentFile.ByteOrder);
            foreach (var attribute in helper.Attributes)
            {
                for (int v = 0; v < Vertices.Count; v++)
                {
                    switch (attribute.Name)
                    {
                        case "_p0": attribute.Data[v] = ToVector4(ConvertLocal(Vertices[v].Position,v, false, weighPositions)); break;
                        case "_n0": attribute.Data[v] = ToVector4(ConvertLocal(Vertices[v].Normal, v, true, weighPositions)); break;
                        case "_u0": attribute.Data[v] = ToVector4(Vertices[v].TexCoords[0]); break;
                        case "_t0": attribute.Data[v] = ToVector4(Vertices[v].Tangent); break;
                        case "_b0": attribute.Data[v] = ToVector4(Vertices[v].Bitangent); break;
                        case "_w0": attribute.Data[v] = ConvertWeights(Vertices[v].BoneWeights); break;
                        case "_i0": attribute.Data[v] = ConvertIndices(Vertices[v].BoneIndices); break;
                    }
                }
            }
            this.VertexBuffer = helper.ToVertexBuffer();
        }

        private Vector3 ConvertLocal(Vector3 position, int vIndex, bool isNormal, bool weighed)
        {
            if (!weighed || VertexSkinCount > 1) return position;

            if (VertexSkinCount == 1) {
                //Get rigid bone index
                var boneIndex = Vertices[vIndex].BoneIndices[0];
                //Transform by the inverse
                var transform = ParentModel.Skeleton.Bones[boneIndex].Inverse;
                if (transform.Determinant == 0) return position;

                if (isNormal)
                    return Vector3.TransformNormal(position, transform);
                else
                    return Vector3.TransformPosition(position, transform);
            }
            else
            {
                //Get mesh bone index
                //Transform by the inverse
                var transform = ParentModel.Skeleton.Bones[this.BoneIndex].Inverse;
                if (transform.Determinant == 0) return position;

                if (isNormal)
                    return Vector3.TransformNormal(position, transform);
                else
                    return Vector3.TransformPosition(position, transform);
            }
        }

        private Syroot.Maths.Vector4F ToVector4(Vector2 vector) {
            return new Syroot.Maths.Vector4F(vector.X, vector.Y, 0.0f, 0.0f);
        }

        private Syroot.Maths.Vector4F ToVector4(Vector3 vector) {
            return new Syroot.Maths.Vector4F(vector.X, vector.Y, vector.Z, 0.0f);
        }

        private Syroot.Maths.Vector4F ToVector4(Vector4 vector) {
            return new Syroot.Maths.Vector4F(vector.X, vector.Y, vector.Z, vector.W);
        }

        private Syroot.Maths.Vector4F ConvertIndices(List<int> vector) {
            Syroot.Maths.Vector4F buffer = new Syroot.Maths.Vector4F();
            for (int i = 0; i < Math.Min(VertexSkinCount, vector.Count); i++) {
                buffer[i] = vector[i];
            }
            return buffer;
        }

        private Syroot.Maths.Vector4F ConvertWeights(List<float> weights)
        {
            //The total should be 1.0
            float total = weights.Sum(x => x);

            //Find the difference of the total and 1.0
            float dif = 1.0f - total;
            if (dif != 0) {
                //Divide the difference to force values as 1.0
                for (int i = 0; i < weights.Count; i++)
                    weights[i] = weights[i] / dif;
            }

            Syroot.Maths.Vector4F buffer = new Syroot.Maths.Vector4F();
            for (int i = 0; i < Math.Min(VertexSkinCount, 4); i++) {
                buffer[i] = weights.Count < i ? weights[i] : 0;
            }
            return buffer;
        }

        //Gets attributes with more than one channel
        private Syroot.Maths.Vector4F[][] TryGetChannelValues(VertexBufferHelper helper, string attribute)
        {
            List<Syroot.Maths.Vector4F[]> channels = new List<Syroot.Maths.Vector4F[]>();
            for (int i = 0; i < 10; i++)
            {
                if (helper.Contains($"{attribute}{i}"))
                    channels.Add(helper[$"{attribute}{i}"].Data);
                else
                    break;
            }
            return channels.ToArray();
        }

        //Gets the attribute data given the attribute key.
        private Syroot.Maths.Vector4F[] TryGetValues(VertexBufferHelper helper, string attribute)
        {
            if (helper.Contains(attribute))
                return helper[attribute].Data;
            else
                return new Syroot.Maths.Vector4F[0];
        }

        /// <summary>
        /// Updates the current polygon groups from the shape data.
        /// </summary>
        public void UpdatePolygonGroups()
        {
            PolygonGroups = new List<STPolygonGroup>();
            foreach (var mesh in Shape.Meshes)
            {
                //Set group as a level of detail
                var group = new STPolygonGroup();
                group.Material = Material;
                group.GroupType = STPolygonGroupType.LevelOfDetail;
                //Load indices into the group
                var indices = mesh.GetIndices().ToArray();
                for (int i = 0; i < indices.Length; i++)
                    group.Faces.Add(indices[i]);

                if (!PrimitiveTypes.ContainsKey(mesh.PrimitiveType))
                    throw new Exception($"Unsupported primitive type! {mesh.PrimitiveType}");

                //Set the primitive type
                group.PrimitiveType = PrimitiveTypes[mesh.PrimitiveType];
                //Set the face offset (used for level of detail meshes)
                group.FaceOffset = (int)mesh.SubMeshes[0].Offset;
                PolygonGroups.Add(group);
                break;
            }
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

        public List<VaoAttribute> LoadGLAttributes()
        {
            var vertexBuffer = ParentModel.Model.VertexBuffers[Shape.VertexBufferIndex];

            List<VaoAttribute> attributes = new List<VaoAttribute>();

            int offset = 0;
            foreach (VertexAttrib att in vertexBuffer.Attributes.Values)
            {
                if (!ElementCountLookup.ContainsKey(att.Name.Remove(2)))
                    continue;

                bool assigned = false;
                int stride = 0;

                var assign = Material.Material.ShaderAssign;
                foreach (var matAttribute in assign.AttribAssigns)
                {
                    if (matAttribute.Value == att.Name)
                    {
                        //Get the translated attribute that is passed to the fragment shader
                        //Models can assign the same attribute to multiple uniforms (ie u0 to u1, u2)
                        string translated = matAttribute.Key;

                        VaoAttribute vaoAtt = (VaoAttribute)Activator.CreateInstance(VaoAttributeType);
                        vaoAtt.vertexAttributeName = att.Name;
                        vaoAtt.name = translated;
                        vaoAtt.ElementCount = ElementCountLookup[att.Name.Remove(2)];
                        vaoAtt.Assigned = assigned;
                        vaoAtt.Offset = offset;

                        if (att.Name.Contains("_i"))
                            vaoAtt.Type = VertexAttribPointerType.Int;
                        else
                            vaoAtt.Type = VertexAttribPointerType.Float;

                        attributes.Add(vaoAtt);

                        if (!assigned)
                        {
                            stride = vaoAtt.Stride;
                            assigned = true;
                        }
                    }
                }

                offset += stride;
            }
            return attributes;
        }

        static Dictionary<string, int> ElementCountLookup = new Dictionary<string, int>()
        {
            { "_u", 2 },
            { "_p", 3 },
            { "_n", 3 },
            { "_t", 4 },
            { "_b", 4 },
            { "_c", 4 },
            { "_w", 4 },
            { "_i", 4 },
        };
    }

    public class KeyGroup
    {
        public List<STVertex> Vertices = new List<STVertex>();
    }
}
