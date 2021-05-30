using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using MapStudio.Rendering;
using GLFrameworkEngine;

namespace BfresEditor
{
    public class BfresModelAsset : ModelAsset, IPickable
    {
        public GLTransform Transform { get; set; } = new GLTransform();

        public override bool IsVisible
        {
            get { return ResModel.IsVisible; }
            set { ResModel.IsVisible = value; }
        }

        public List<BfresMeshAsset> Meshes = new List<BfresMeshAsset>();
        public override IEnumerable<GenericPickableMesh> MeshList => Meshes;

        public FMDL ResModel => ModelData as FMDL;

        static Type[] BfshaRenders = new Type[]
        {
            typeof(RedPro2NXRender), typeof(TurboNXRender),
            typeof(BlitzNXRender), typeof(KSANXRender),
            typeof(SMORenderer), typeof(RedCarpetNXRender),
            typeof(ACNHNXRender), typeof(UKingNXRender),
            typeof(SMM2Render)
        };

        static Type[] SharcfbRenders = new Type[]
        {
           typeof(WWHDRender),  typeof(RedPro2URender)
        };

        /// <summary>
        /// A list of uniform blocks, typically used to store skeleton information.
        /// </summary>
        public Dictionary<string, UniformBlock> UniformBlocks = new Dictionary<string, UniformBlock>();

        private bool isSelected = false;
        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                isSelected = value;
                foreach (BfresMeshAsset mesh in Meshes)
                    mesh.IsSelected = value;
            }
        }

        public bool IsHovered { get; set; }

        public void DragDropped(object droppedItem) { }
        public void DragDroppedOnLeave() { }
        public void DragDroppedOnEnter() { }

        private bool disposed = false;

        private BfresRender ParentRender;
        public SkeletonRenderer SkeletonRenderer;

        public BfresModelAsset(BFRES bfres, BfresRender render, FMDL model) {
            ParentRender = render;
            ModelData = model;
            Name = model.Name;

            List<Vector3> positons = new List<Vector3>();
            foreach (FSHP mesh in model.Meshes) {
                //Create a mesh asset for rendering onto
                var meshAsset = new BfresMeshAsset(mesh);
                Meshes.Add(meshAsset);

                foreach (var customRender in BfshaRenders)
                {
                    var materialRender = (ShaderRenderBase)Activator.CreateInstance(customRender);
                    if (materialRender.UseRenderer(
                        mesh.Material,
                       mesh.Material.ShaderArchive,
                       mesh.Material.ShaderModel))
                    {
                        materialRender.TryLoadShader(bfres, ResModel, mesh, meshAsset);
                        break;
                    }
                }

                //Check for the GX2 shader decompiler as needed for Wii U games
                if (System.IO.File.Exists("GFD\\gx2shader-decompiler.exe"))
                {
                    foreach (var customRender in SharcfbRenders)
                    {
                        var materialRender = (SharcFBRenderer)Activator.CreateInstance(customRender);
                        if (materialRender.UseRenderer(
                           mesh.Material,
                           mesh.Material.ShaderArchive,
                           mesh.Material.ShaderModel))
                        {
                            materialRender.TryLoadShader(bfres, ResModel, mesh, meshAsset);
                            break;
                        }
                    }
                }

                mesh.Material.MaterialAsset = (BfresMaterialAsset)meshAsset.MaterialAsset;

                ((BfresMaterialAsset)meshAsset.MaterialAsset).ParentRenderer = render;
                ((BfresMaterialAsset)meshAsset.MaterialAsset).ParentModel = this.ResModel;

                //Calculate total boundings
                for (int i = 0; i < mesh.Vertices.Count; i++)
                    positons.Add(mesh.Vertices[i].Position);
            }
            SkeletonRenderer = new SkeletonRenderer(model.Skeleton);

            //Calculate the total bounding sphere
            BoundingSphere = GLFrameworkEngine.Utils.BoundingSphereGenerator.GenerateBoundingSphere(positons);
            positons.Clear();
        }

        public SHARC.ShaderProgram TryLoadSharcProgram(BFRES bfres, string shaderFile, string shaderModel)
        {
            var archiveFile = bfres.FileInfo.ParentArchive;
            if (archiveFile == null)
                return null;

            foreach (var file in archiveFile.Files)
            {
                if (file.FileName == $"{shaderFile}.sharc") {
                    var sharc = SHARC.Load(file.FileData);
                    return sharc.ShaderPrograms.FirstOrDefault(x => x.Name == shaderModel);
                }
            }
            return null;
        }

        public void ResetAnimations()
        {
            foreach (var bone in ModelData.Skeleton.Bones)
                bone.Visible = true;

            ModelData.Skeleton.Reset();
        }

        public void DrawShadow(GLFrameworkEngine.GLContext control, BfresRender parentRender)
        {
            var shadowRender = GLFrameworkEngine.GlobalShaders.GetShader("SHADOW");
            control.CurrentShader = shadowRender;

            GL.Disable(EnableCap.CullFace);
            foreach (var mesh in this.Meshes)
            {
                if (!mesh.IsVisible || mesh.IsDepthShadow || mesh.IsCubeMap ||
                    !ModelData.Skeleton.Bones[mesh.BoneIndex].Visible)
                    continue;

                //Set light space matrix
             //   Matrix4 lightSpace = control.ShadowRenderer.getProjectionViewMatrix();
              //  shadowRender.SetMatrix4x4("lightSpaceMatrix", ref lightSpace);

                //Draw the mesh
                mesh.vao.Enable(shadowRender);
                mesh.Render(control, shadowRender);
            }
            GL.Enable(EnableCap.CullFace);
            GL.UseProgram(0);
        }

        public void DrawColorPicking(GLContext control)
        {
            if (disposed || !IsVisible)
                return;

            if (control.ColorPicker.PickingMode == ColorPicker.SelectionMode.Model)
                control.ColorPicker.SetPickingColor(this, control.CurrentShader);

            GL.Enable(EnableCap.DepthTest);
            foreach (BfresMeshAsset mesh in this.Meshes)
            {
                if (!mesh.IsVisible || mesh.IsDepthShadow || mesh.IsCubeMap)
                    continue;

                ((BfresMaterialAsset)mesh.MaterialAsset).SetRenderState(mesh.Shape.Material);

                //Draw the mesh
                if (control.ColorPicker.PickingMode == ColorPicker.SelectionMode.Mesh)
                    control.ColorPicker.SetPickingColor(mesh, control.CurrentShader);

                if (control.ColorPicker.PickingMode == ColorPicker.SelectionMode.Face)
                    control.ColorPicker.SetPickingColorFaces(mesh.PickableFaces, control.CurrentShader);


                DrawSolidColorMesh(control.CurrentShader, mesh);
            }
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
            GL.UseProgram(0);
        }

        public void Draw(GLContext control, GLFrameworkEngine.Pass pass, BfresRender parentRender)
        {
            if (disposed || !IsVisible)
                return;

            GL.Enable(EnableCap.TextureCubeMapSeamless);

            if (pass == GLFrameworkEngine.Pass.OPAQUE && Meshes.Any(x => x.IsSelected ))
            {
                GL.Enable(EnableCap.StencilTest);
                GL.Clear(ClearBufferMask.StencilBufferBit);
                GL.ClearStencil(0);
                GL.StencilFunc(StencilFunction.Always, 0x1, 0x1);
                GL.StencilOp(StencilOp.Keep, StencilOp.Replace, StencilOp.Replace);
            }

            //Go through each mesh and map materials using shader programs
            var meshes = RenderLayer.Sort(this);
            foreach (var mesh in meshes)
            {
                if (mesh.Pass != pass || !mesh.IsVisible || 
                    mesh.IsDepthShadow || mesh.IsCubeMap || mesh.UseColorBufferPass)
                    continue;

                //Load the material data
                var material = (FMAT)mesh.Shape.Material;
                mesh.Material = material;
                //Update the parent renderer
                ((BfresMaterialAsset)mesh.MaterialAsset).ParentRenderer = parentRender;

                if (!ModelData.Skeleton.Bones[mesh.BoneIndex].Visible)
                    continue;

                RenderMesh(control, mesh);
            }

            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.Enable(EnableCap.DepthTest);
            GL.Disable(EnableCap.TextureCubeMapSeamless);
            GL.Disable(EnableCap.AlphaTest);
            GL.Disable(EnableCap.Blend);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);

            if (meshes.Any(x => x.UseColorBufferPass))
                ScreenBufferTexture.FilterScreen(control);

            foreach (var mesh in meshes.Where(x => x.UseColorBufferPass))
            {
                if (pass != mesh.Pass || !mesh.IsVisible)
                    continue;

                //Load the material data
                var material = (FMAT)mesh.Shape.Material;
                mesh.Material = material;
                ((BfresMaterialAsset)mesh.MaterialAsset).ParentRenderer = parentRender;

                if (!ModelData.Skeleton.Bones[mesh.BoneIndex].Visible)
                    continue;

                RenderMesh(control, mesh);
            }

            if (Runtime.RenderSettings.WireframeOverlay)
                DrawWireframeOutline(control);

            if (pass == GLFrameworkEngine.Pass.TRANSPARENT)
                DrawSelection(control, parentRender.IsSelected || this.IsSelected);

            GL.Enable(EnableCap.DepthTest);
            GL.Disable(EnableCap.TextureCubeMapSeamless);
            GL.Disable(EnableCap.AlphaTest);
            GL.Disable(EnableCap.Blend);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
        }

        private void DrawSelectedFaces()
        {

        }

        private void DrawWireframeOutline(GLContext control)
        {
            GL.Enable(EnableCap.StencilTest);
            GL.PolygonMode(MaterialFace.Front, PolygonMode.Line);
            GL.Enable(EnableCap.LineSmooth);
            GL.LineWidth(1.5f);

            var selectionShader = GlobalShaders.GetShader("PICKING");
            control.CurrentShader = selectionShader;
            selectionShader.SetVector4("color", new Vector4(1, 1, 1, 1));

            foreach (var mesh in Meshes)
                DrawSolidColorMesh(selectionShader, mesh);

            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

            GL.Enable(EnableCap.DepthTest);
        }

        private void DrawSelection(GLContext control, bool parentSelected)
        {
            GL.Disable(EnableCap.AlphaTest);
            GL.Disable(EnableCap.Blend);

            GL.LineWidth(3.0f);
            GL.StencilFunc(StencilFunction.Equal, 0x0, 0x1);
            GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Replace);

            GL.PolygonMode(MaterialFace.Front, PolygonMode.Line);

            var selectionShader = GlobalShaders.GetShader("PICKING");
            control.CurrentShader = selectionShader;
            selectionShader.SetVector4("color", new Vector4(1,1,1,1));

            foreach (var mesh in Meshes) {
                if (mesh.IsSelected) {
                    DrawSolidColorMesh(selectionShader, mesh);
                }
            }

            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

            GL.Disable(EnableCap.StencilTest);
            GL.LineWidth(1);
        }

        private void DrawSolidColorMesh(GLFrameworkEngine.ShaderProgram shader, BfresMeshAsset mesh)
        {
            if (mesh.SkinCount > 0)
                SetModelMatrix(shader.program, ModelData.Skeleton, mesh.SkinCount > 1);

            var worldTransform = ParentRender.Transform.TransformMatrix;
            var transform = this.ModelData.Skeleton.Bones[mesh.BoneIndex].Transform;
            shader.SetMatrix4x4("RigidBindTransform", ref transform);
            shader.SetMatrix4x4("mtxMdl", ref worldTransform);
            shader.SetInt("SkinCount", mesh.SkinCount);
            shader.SetInt("UseSkinning", 1);

            //Draw the mesh
            mesh.defaultVao.Enable(shader);
            mesh.defaultVao.Use();
            mesh.Draw();
        }

        public void DrawGBuffer(GLContext control, BfresRender parentRender)
        {
            if (disposed || !IsVisible) 
                return;

            foreach (BfresMeshAsset mesh in this.Meshes)
            {
                if (!mesh.IsVisible || mesh.IsDepthShadow || mesh.IsCubeMap)
                    continue;

                RenderMesh(control, mesh, 2);
            }

            control.CurrentShader = null;

            GL.Enable(EnableCap.DepthTest);
            GL.Disable(EnableCap.TextureCubeMapSeamless);
            GL.Disable(EnableCap.AlphaTest);
            GL.Disable(EnableCap.Blend);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
            GL.UseProgram(0);
        }

        public void DrawShadowModel(GLFrameworkEngine.GLContext control, BfresRender parentRender)
        {
            if (disposed || !IsVisible)
                return;

            foreach (BfresMeshAsset mesh in this.Meshes)
            {
                if (!mesh.IsVisible || mesh.IsDepthShadow ||
                     mesh.IsCubeMap || mesh.UseColorBufferPass)
                    continue;

                RenderMesh(control, mesh, 1);
            }

            control.CurrentShader = null;

            GL.Enable(EnableCap.DepthTest);
            GL.Disable(EnableCap.TextureCubeMapSeamless);
            GL.Disable(EnableCap.AlphaTest);
            GL.Disable(EnableCap.Blend);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
            GL.UseProgram(0);
        }

        public void DrawDepthBuffer(GLFrameworkEngine.GLContext control, BfresRender parentRender)
        {
            if (disposed || !IsVisible)
                return;

            foreach (BfresMeshAsset mesh in this.Meshes)
            {
                if (!mesh.IsVisible || mesh.IsDepthShadow ||
                     mesh.IsCubeMap || mesh.UseColorBufferPass)
                    continue;

                RenderMesh(control, mesh, 1);
            }

            control.CurrentShader = null;

            GL.Enable(EnableCap.DepthTest);
            GL.Disable(EnableCap.TextureCubeMapSeamless);
            GL.Disable(EnableCap.AlphaTest);
            GL.Disable(EnableCap.Blend);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
            GL.UseProgram(0);
        }

        public void DrawDebug(GLContext control, BfresMeshAsset mesh)
        {
            if (Runtime.DebugRendering != Runtime.DebugRender.Diffuse)
                control.UseSRBFrameBuffer = false;

            DebugShaderRender.RenderMaterial(control);
            DrawSolidColorMesh(control.CurrentShader, mesh);
        }

        public void RenderMesh(GLContext control, BfresMeshAsset mesh, int stage = 0)
        {
            if (mesh.UpdateVertexData)
                mesh.UpdateVertexBuffer();

            if (Runtime.DebugRendering != Runtime.DebugRender.Default)
            {
                DrawDebug(control,mesh);
                return;
            }
            else if (mesh.MaterialAsset is ShaderRenderBase) {
                DrawCustomShaderRender(control, mesh, stage);
                return;
            }
            else //Draw default if not using game shader rendering.
            {
                if (!control.IsShaderActive(BfresRender.DefaultShader))
                    control.CurrentShader = BfresRender.DefaultShader;

                var mtxMdl = this.ParentRender.Transform.TransformMatrix;
                var mtxCam = control.Camera.ViewProjectionMatrix;
                control.CurrentShader.SetMatrix4x4("mtxMdl", ref mtxMdl);
                control.CurrentShader.SetMatrix4x4("mtxCam", ref mtxCam);

                mesh.MaterialAsset.Render(control, control.CurrentShader, mesh);
                DrawSolidColorMesh(control.CurrentShader, mesh);
            }
        }

        private void DrawCustomShaderRender(GLContext control, BfresMeshAsset mesh, int stage = 0)
        {
            var materialAsset = ((ShaderRenderBase)mesh.MaterialAsset);
            if (!materialAsset.HasValidProgram)
            {
                //Draw the mesh as solid color
                var solidRender = GLFrameworkEngine.GlobalShaders.GetShader("PICKING");
                control.CurrentShader = solidRender;

                solidRender.SetVector4("color", new Vector4(1,0,0,1));

                DrawSolidColorMesh(control.CurrentShader, mesh);
                return;
            }

            materialAsset.CheckProgram(control, mesh, stage);

            if (control.CurrentShader != materialAsset.Shader)
                control.CurrentShader = materialAsset.Shader;

            mesh.MaterialAsset.Render(control, materialAsset.Shader, mesh);

            //Draw the mesh
            mesh.vao.Enable(materialAsset.Shader);
            mesh.Render(control, materialAsset.Shader);
        }

        private void SetModelMatrix(int programID,  STSkeleton skeleton, bool useInverse = true)
        {
            GL.Uniform1(GL.GetUniformLocation(programID, "UseSkinning"), 1);

            for (int i = 0; i < skeleton.Bones.Count; i++)
            {
                Matrix4 transform = skeleton.Bones[i].Transform;
                //Check if the bone is smooth skinning aswell for accuracy purposes.
                if (useInverse || ((BfresBone)skeleton.Bones[i]).UseSmoothMatrix)
                    transform = skeleton.Bones[i].Inverse * skeleton.Bones[i].Transform;
                GL.UniformMatrix4(GL.GetUniformLocation(programID, String.Format("bones[{0}]", i)), false, ref transform);
            }
        }

        public void Destroy()
        {
            disposed = true;

            foreach (var mesh in Meshes)
                mesh.Dispose();
        }
    }
}
