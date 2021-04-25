using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Toolbox.Core;

namespace GLFrameworkEngine
{
    public class GenericRenderer 
    {
        public virtual GLTransform Transform { get; set; } = new GLTransform();

        public Dictionary<string, STGenericTexture> Textures = new Dictionary<string, STGenericTexture>();

        public List<ModelAsset> Models = new List<ModelAsset>();

        public EventHandler TransformChanged;
        public EventHandler TransformApplied;

        public string ID { get; set; }

        public virtual bool IsVisible { get; set; } = true;

        public virtual bool IsSelected { get; set; }

        public virtual bool EnableShadows { get; set; }

        public virtual bool InFustrum { get; set; }

        public virtual GLFrameworkEngine.ShaderProgram GetShaderProgram() { return null; }

        public virtual List<string> DebugShading { get; }

        public virtual string DebugShadingMode { get; set; }

        //Note this is necessary to adjust if meshes are animated by shaders
        //For animated meshes use the normal vertex shader, then a picking color fragment shader
        //This is only for static meshes
        public virtual GLFrameworkEngine.ShaderProgram PickingShader => GLFrameworkEngine.GlobalShaders.GetShader("PICKING");

        public virtual void ResetAnimations() { }

        public virtual void ReloadFile(string fileName) { }

        bool UpdateModelMatrix = false;

        public string Name { get; set; }

        public void ResetAnim()
        {
        }

        public GenericRenderer(Vector3 position, Vector3 rotationEuler, Vector3 scale)
        {
            UpdateModelMatrix = true;
        }

        public GenericRenderer()
        {
            UpdateModelMatrix = true;
        }

        public virtual bool ModelInFustrum(GLFrameworkEngine.GLContext control)
        {
            return true;
        }

        public virtual void OnModelMatrixUpdated()
        {

        }

        public virtual void DrawModel(GLContext control, Pass pass, Vector4 highlightColor)
        {

        }

        public virtual void DrawShadowModel(GLContext control)
        {

        }

        public virtual void DrawGBuffer(GLContext control)
        {

        }

        public virtual void DrawCubeMapScene(GLContext control)
        {

        }

        public virtual void Dispose()
        {

        }
    }
}
