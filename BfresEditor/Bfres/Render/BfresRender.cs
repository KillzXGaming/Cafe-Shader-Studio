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
            if (!UpdateProbeMap || TurboNXRender.DiffuseLightmapTexture == null || Transform.Position == Vector3.Zero)
                return;

            if (DiffuseProbeTexture == null)
            {
                DiffuseProbeTexture = GLTextureCube.CreateEmptyCubemap(
                 32, PixelInternalFormat.R11fG11fB10f, PixelFormat.Rgb, PixelType.UnsignedInt10F11F11FRev, 2);

                //Allocate mip data. Need 2 seperate mip levels
                DiffuseProbeTexture.Bind();
                DiffuseProbeTexture.GenerateMipmaps();
                DiffuseProbeTexture.Unbind();
            }

            bool generated = LightingEngine.LightSettings.UpdateProbeCubemap(control,
                DiffuseProbeTexture, Transform.Position);

            DiffuseProbeTexture.Save($"LIGHTMAP_PROBE");

            if (!generated)
                return;

            UpdateProbeMap = false;
        }

        public void Load(BFRES bfres)
        {
            Name = bfres.Header;
            foreach (var model in bfres.Models)
                Models.Add(new BfresModelAsset(bfres, this, model));
            foreach (var tex in bfres.Textures)
                Textures.Add(tex.Name, tex);
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
        }

        /// <summary>
        /// Checks all meshes to see if any are within the camera fustrum.
        /// Returns true if any are in view.
        /// </summary>
        /// <returns></returns>
        public bool UpdateModelFustrum(GLContext control)
        {
            bool inFustrum = false;
            for (int i = 0; i < PickableMeshes.Count; i++)
            {
                //Update the fustrum boolean. This function only gets called once per mesh on updated frame
                PickableMeshes[i].InFustrum = IsMeshInFustrum(control, PickableMeshes[i]);
                if (PickableMeshes[i].InFustrum)
                    inFustrum = true;
            }
            return inFustrum;
        }
        
        /// <summary>
        /// Checks for when the current render is in the fustrum of the camera
        /// Returns true if in view.
        /// </summary>
        public override bool ModelInFustrum(GLContext control)
        {
            if (StayInFustrum) return true;

            InFustrum = UpdateModelFustrum(control);
            if (!Name.Contains("course")) //Draw distance map objects
                InFustrum = InFustrum && this.IsInRange(renderDistance, renderDistanceSquared,
                                control.Camera.Translation);

            return InFustrum;
        }

        public bool IsInRange(float range, float rangeSquared, Vector3 pos) {
            return (pos - Transform.Position).LengthSquared < rangeSquared;
        }

        /// <summary>
        /// Checks for when the given mesh render is in the fustrum of the camera
        /// Returns true if in view.
        /// </summary>
        private bool IsMeshInFustrum(GLContext control, GenericPickableMesh mesh)
        {
            if (StayInFustrum)
                return true;

            var msh = (BfresMeshAsset)mesh;
            msh.BoundingNode.UpdateTransform(Transform.TransformMatrix);
            return CameraFustrum.CheckIntersection(control.Camera, msh.BoundingNode);
        }

        public override void DrawModel(GLContext control, GLFrameworkEngine.Pass pass, Vector4 highlightColor)
        {
            if (!ModelInFustrum(control) || !IsVisible)
                return;

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
        }

        public void DrawSkeleton(GLContext control)
        {
            foreach (BfresModelAsset model in Models)
                if (model.IsVisible)
                    model.SkeletonRenderer.Render(control);
        }

        public void DrawColorPicking(GLContext control)
        {
            if (!ModelInFustrum(control) || !IsVisible)
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
            if (!ModelInFustrum(control) || !IsVisible)
                return;

            foreach (BfresModelAsset model in Models)
                if (model.IsVisible)
                    model.DrawShadowModel(control, this);
        }

        public override void DrawGBuffer(GLContext control)
        {
            if (!ModelInFustrum(control) || !IsVisible)
                return;

            foreach (BfresModelAsset model in Models) {
                if (model.IsVisible)
                    model.DrawGBuffer(control, this);
            }
        }

        public void DrawDepthBuffer(GLContext control)
        {
            if (!ModelInFustrum(control) || !IsVisible)
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
