using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MapStudio.Rendering;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Toolbox.Core;
using AGraphicsLibrary;
using GLFrameworkEngine;

namespace BfresEditor
{
    public class BfresRender : GenericRenderer, IPickable
    {
        public GLTextureCube DiffuseProbeTexture = null;

        public bool IsSkybox = false;

        public bool StayInFustrum = true;

        public bool UpdateProbeMap = true;

        public static GLFrameworkEngine.ShaderProgram DefaultShader => GlobalShaders.GetShader("BFRES", "BFRES/Bfres");
        public static GLFrameworkEngine.ShaderProgram ShadowProgram => GlobalShaders.GetShader("BFRES", "BFRES/Picking");
        private static GLFrameworkEngine.ShaderProgram PickingShaderCustom=> GlobalShaders.GetShader("BFRES", "BFRES/Shadow");

        //List for mesh picking
        public List<GenericPickableMesh> PickableMeshes
        {
            get
            {
                List<GenericPickableMesh> meshes = new List<GenericPickableMesh>();
                for (int i = 0; i < Models.Count; i++)
                    meshes.AddRange(Models[i].MeshList);
                return meshes;
            }
        }

        private bool isSelected = false;
        public override bool IsSelected
        {
            get { return isSelected; }
            set
            {
                isSelected = value;
                foreach (BfresModelAsset model in Models)
                    model.IsSelected = value;
            }
        }

        public bool IsHovered { get; set; }

        public void DragDropped(object droppedItem) { }
        public void DragDroppedOnLeave() { }
        public void DragDroppedOnEnter() { }

        //A custom picking shader to update vertex shader with skinning information
        public override GLFrameworkEngine.ShaderProgram PickingShader => PickingShaderCustom;

        //Render distance for models to cull from far away.
        protected float renderDistanceSquared = 200000000;
        protected float renderDistance = 20000000;

        public BfresRender()
        {
        }

        //Temporary loader atm for map objects to cache quickly and easily
        public static BfresRender LoadFile(string filePath)
        {
            if (!DataCache.ModelCache.ContainsKey(filePath)) {

                BFRES bfres = new BFRES() { FileInfo = new File_Info() };
                bfres.Load(System.IO.File.OpenRead(filePath));
                bfres.Renderer.Name = filePath;
                DataCache.ModelCache.Add(filePath, bfres.Renderer);
                return (BfresRender)bfres.Renderer;
            }
            else {
                var cached = (BfresRender)DataCache.ModelCache[filePath];
                var render = new BfresRender();
                render.Name = filePath;
                render.Models.AddRange(cached.Models);
                foreach (var tex in cached.Textures)
                    render.Textures.Add(tex.Key, tex.Value);
                return render;
            }
        }

        public void UpdateProbeLighting(GLContext control)
        {
            return;

            if (!UpdateProbeMap || TurboNXRender.DiffuseLightmapTexture == null /*|| Transform.Position == Vector3.Zero*/)
                return;

            if (DiffuseProbeTexture == null)
            {
                DiffuseProbeTexture = GLTextureCube.CreateEmptyCubemap(
                 32, PixelInternalFormat.Rgb32f, PixelFormat.Rgb, PixelType.Float, 2);

                //Allocate mip data. Need 2 seperate mip levels
                DiffuseProbeTexture.Bind();
                DiffuseProbeTexture.GenerateMipmaps();
                DiffuseProbeTexture.Unbind();
            }

            
            Transform.Position = new Vector3(ProbeDebugger.Position.X, ProbeDebugger.Position.Y, ProbeDebugger.Position.Z);
            Transform.UpdateMatrix(true);

            var output = LightingEngine.LightSettings.UpdateProbeCubemap(control,
                DiffuseProbeTexture, new Vector3(
                    ProbeDebugger.Position.X, 
                    ProbeDebugger.Position.Y,
                    ProbeDebugger.Position.Z));

            if (output == null)
                return;

            ProbeDebugger.Generated = output.Generated;

            if (output.Generated)
                ProbeDebugger.probeData = output.ProbeData;

            ProbeDebugger.DiffuseProbeTexture = DiffuseProbeTexture;

           // DiffuseProbeTexture.SaveDDS("LIGHT_PROBE.dds");
          //  DiffuseProbeTexture.Save($"LIGHTMAP_PROBE.png");

            ProbeDebugger.ForceUpdate = false;
            UpdateProbeMap = false;
        }

        public void Load(BFRES bfres)
        {
            Name = bfres.Header;
            foreach (var model in bfres.Models)
                Models.Add(new BfresModelAsset(bfres, this, model));
            foreach (var tex in bfres.Textures)
                Textures.Add(tex.Name, tex);

            //Update frame bounding spheres
            List<Vector3> positons = new List<Vector3>();
            foreach (BfresModelAsset model in Models)
            {
                foreach (var mesh in model.Meshes) {
                    for (int i = 0; i < mesh.Shape.Vertices.Count; i++)
                    {
                        var pos = mesh.Shape.Vertices[i].Position;

                        if (mesh.SkinCount == 0)
                        {
                            var bone = model.ModelData.Skeleton.Bones[mesh.BoneIndex];
                            pos = Vector3.TransformPosition(pos, bone.Transform);
                        }
                        if (mesh.SkinCount == 1)
                        {
                            var index = mesh.Shape.Vertices[i].BoneIndices[0];
                            var bone = model.ModelData.Skeleton.Bones[index];
                            pos = Vector3.TransformPosition(pos, bone.Transform);
                        }
                        positons.Add(pos);
                    }
                }
            }

            BoundingSphere = GLFrameworkEngine.Utils.BoundingSphereGenerator.GenerateBoundingSphere(positons);
            positons.Clear();
        }

        /// <summary>
        /// Resets all the animation states to defaults.
        /// Animation value lists are cleared, bones have reset transforms.
        /// </summary>
        public override void ResetAnimations()
        {
            foreach (BfresModelAsset model in Models)
            {
                foreach (var mesh in model.Meshes)
                    ((FMAT)mesh.Material).ResetAnimations();

                model.ResetAnimations();
            }
            //Small adjustment for camera animations
            if (GLContext.ActiveContext != null) {
                GLContext.ActiveContext.Camera.ResetAnimations();
            }
        }

        /// <summary>
        /// Checks all meshes to see if any are within the camera fustrum.
        /// Returns true if any are in view.
        /// </summary>
        /// <returns></returns>
        public bool UpdateModelFrustum(GLContext control)
        {
            bool inFustrum = false;
            foreach (BfresModelAsset model in Models)
            {
                for (int i = 0; i < model.Meshes.Count; i++)
                {
                    //Update the fustrum boolean. This function only gets called once per mesh on updated frame
                    model.Meshes[i].InFrustum = IsMeshInFrustum(control, model.Meshes[i]);
                    if (PickableMeshes[i].InFrustum)
                        inFustrum = true;
                }

            }
            return inFustrum;
        }
        
        /// <summary>
        /// Checks for when the current render is in the fustrum of the camera
        /// Returns true if in view.
        /// </summary>
        public override bool ModelInFrustum(GLContext control)
        {
            if (StayInFustrum) return true;

            InFustrum = UpdateModelFrustum(control);
            if (!Name.Contains("course")) //Draw distance map objects
                InFustrum = InFustrum && this.IsInRange(renderDistance, renderDistanceSquared,
                                control.Camera.TargetPosition);

            return InFustrum;
        }

        public bool IsInRange(float range, float rangeSquared, Vector3 pos) {
            return (pos - Transform.Position).LengthSquared < rangeSquared;
        }

        /// <summary>
        /// Checks for when the given mesh render is in the fustrum of the camera
        /// Returns true if in view.
        /// </summary>
        private bool IsMeshInFrustum(GLContext control, GenericPickableMesh mesh)
        {
             if (StayInFustrum)
                 return true;

            var msh = (BfresMeshAsset)mesh;
            msh.BoundingNode.UpdateTransform(Transform.TransformMatrix);
            return CameraFrustum.CheckIntersection(control.Camera, msh.BoundingNode);
        }

        public override void DrawModel(GLContext control, GLFrameworkEngine.Pass pass, Vector4 highlightColor)
        {
            if (!ModelInFrustum(control) || !IsVisible)
                return;

            if (ProbeDebugger.ForceUpdate)
                UpdateProbeMap = true;

            if (Runtime.DebugRendering != Runtime.DebugRender.Default)
                control.CurrentShader = GlobalShaders.GetShader("DEBUG");
            else if (control.CurrentShader != BfresRender.DefaultShader)
                control.CurrentShader = BfresRender.DefaultShader;

            Transform.UpdateMatrix();
            foreach (BfresModelAsset model in Models)
                if (model.IsVisible)
                    model.Draw(control, pass, this);


            if (Runtime.DisplayBones)
                DrawSkeleton(control);

            if (Runtime.RenderBoundingBoxes)
                DrawBoundings(control);
        }

        /// <summary>
        /// Draws the caustics from "CausticsArea" models.
        /// </summary>
        public override void DrawCaustics(GLContext context, GLTexture gbuffer, GLTexture linearDepth)
        {
            foreach (BfresModelAsset model in Models)
            {
                //Model resource used for caustic light projection in light pre pass
                if (model.Name == "CausticsArea")
                    model.DrawCaustics(context, gbuffer, linearDepth);
            }
        }

        public override void OnBeforeDraw(GLContext context)
        {
            if (UpdateProbeMap)
                UpdateProbeLighting(context);
        }

        public void DrawSkeleton(GLContext control)
        {
            foreach (BfresModelAsset model in Models)
                if (model.IsVisible)
                    model.SkeletonRenderer.Render(control);
        }

        public void DrawBoundings(GLContext control)
        {
            foreach (BfresModelAsset model in Models)
            {
                if (!model.IsVisible)
                    continue;

                foreach (var mesh in model.Meshes)
                {
                    if (!mesh.IsVisible || !mesh.InFrustum)
                        continue;

                    //Go through each bounding in the current displayed mesh
                    var polygonGroup = mesh.Shape.PolygonGroups[mesh.LODMeshLevel] as MeshPolygonGroup;
                    foreach (var bounding in polygonGroup.Boundings)
                    {
                        var min = bounding.Center - bounding.Extent;
                        var max = bounding.Center + bounding.Extent;

                        var shader = GlobalShaders.GetShader("PICKING");
                        control.CurrentShader = shader;
                        control.CurrentShader.SetVector4("color", new Vector4(1));

                        Matrix4 transform = Matrix4.Identity;

                        if (mesh.SkinCount == 0)
                        {
                            transform = model.ModelData.Skeleton.Bones[mesh.BoneIndex].Transform;
                            control.CurrentShader.SetMatrix4x4("mtxMdl", ref transform);

                            var bnd = mesh.BoundingNode.Box;
                            BoundingBoxRender.Draw(control, bnd.Min, bnd.Max);
                        }
                        else
                        {
                            foreach (var boneIndex in mesh.Shape.Shape.SkinBoneIndices)
                            {
                                transform = model.ModelData.Skeleton.Bones[boneIndex].Transform;
                                control.CurrentShader.SetMatrix4x4("mtxMdl", ref transform);

                                BoundingBoxRender.Draw(control,
                                    new Vector3(min.X, min.Y, min.Z),
                                    new Vector3(max.X, max.Y, max.Z));
                            }
                        }
                    }


                    /*
                                        var center = mesh.BoundingNode.Center;
                                        var radius = mesh.BoundingNode.Radius;

                                        GL.Enable(EnableCap.Blend);

                                        transform = Matrix4.CreateScale(radius) * Matrix4.CreateTranslation(center) * transform;
                                        control.CurrentShader.SetMatrix4x4("mtxMdl", ref transform);
                                        control.CurrentShader.SetVector4("color", new Vector4(0,0,0,0.2f));

                                        SphereRender.Draw(control);

                                        GL.Disable(EnableCap.Blend);*/
                }
            }
            control.CurrentShader = null;
        }

        public void DrawColorPicking(GLContext control)
        {
            if (!ModelInFrustum(control) || !IsVisible)
                return;

            Transform.UpdateMatrix();
            var shader = GlobalShaders.GetShader("PICKING");
            control.CurrentShader = shader;

            if (control.ColorPicker.PickingMode == ColorPicker.SelectionMode.Object)
                control.ColorPicker.SetPickingColor(this, shader);

            foreach (BfresModelAsset model in Models)
            {
                if (model.IsVisible)
                    model.DrawColorPicking(control);
            }
        }

        public override void DrawShadowModel(GLContext control)
        {
            if (!ModelInFrustum(control) || !IsVisible)
                return;

            foreach (BfresModelAsset model in Models)
                if (model.IsVisible)
                    model.DrawShadowModel(control, this);
        }

        public override void DrawGBuffer(GLContext control)
        {
            if (!ModelInFrustum(control) || !IsVisible)
                return;

            foreach (BfresModelAsset model in Models) {
                if (model.IsVisible)
                    model.DrawGBuffer(control, this);
            }
        }

        public void DrawDepthBuffer(GLContext control)
        {
            if (!ModelInFrustum(control) || !IsVisible)
                return;

            foreach (BfresModelAsset model in Models) {
                model.DrawDepthBuffer(control, this);
            }
        }
        
        public override void DrawCubeMapScene(GLContext control)
        {
            foreach (BfresModelAsset model in Models) {
                foreach (var mesh in model.Meshes)
                {
                    if (!mesh.RenderInCubeMap && !mesh.IsCubeMap && mesh.Pass != Pass.OPAQUE || mesh.IsDepthShadow)
                        continue;

                    var material = (FMAT)mesh.Shape.Material;
                    mesh.Material = material;
                    ((BfresMaterialAsset)mesh.MaterialAsset).ParentRenderer = this;

                    model.RenderMesh(control, mesh);
                }
                foreach (var mesh in model.Meshes) {
                    if (!mesh.RenderInCubeMap && !mesh.IsCubeMap && mesh.Pass != Pass.TRANSPARENT || mesh.IsDepthShadow)
                        continue;

                    var material = (FMAT)mesh.Shape.Material;
                    mesh.Material = material;
                    ((BfresMaterialAsset)mesh.MaterialAsset).ParentRenderer = this;

                    model.RenderMesh(control, mesh);
                }
            }
            control.CurrentShader = null;
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        public override void Dispose()
        {
            Console.WriteLine("Disposing " + this.Name);

            foreach (BfresModelAsset model in Models)
                model.Destroy();
            foreach (var tex in Textures.Values)
                tex.RenderableTex?.Dispose();

            Models.Clear();
            Textures.Clear();
        }

        public static void ClearShaderCache()
        {
            foreach (var shader in CafeShaderDecoder.GLShaderPrograms)
                shader.Value.Program.Dispose();
            foreach (var shader in TegraShaderDecoder.GLShaderPrograms)
                shader.Value.Dispose();

            CafeShaderDecoder.GLShaderPrograms.Clear();
            TegraShaderDecoder.GLShaderPrograms.Clear();
        }
    }
}
