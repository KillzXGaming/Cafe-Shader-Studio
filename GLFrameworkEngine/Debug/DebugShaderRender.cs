using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;
using OpenTK.Graphics.OpenGL;

namespace GLFrameworkEngine
{
    public class DebugShaderRender
    {
        public static void RenderMaterial(GLContext context)
        {
            var debugShader = context.CurrentShader;
            debugShader.SetInt("debugShading", (int)Runtime.DebugRendering);
            debugShader.SetInt("weightRampType", 2);
            debugShader.SetInt("selectedBoneIndex", Runtime.SelectedBoneIndex);

            int slot = 1;
            BindTexture(debugShader, RenderTools.uvTestPattern, "UVTestPattern", slot++);
            BindTexture(debugShader, RenderTools.boneWeightGradient, "weightRamp1", slot++);
            BindTexture(debugShader, RenderTools.boneWeightGradient2, "weightRamp2", slot++);
        }

        static void BindTexture(ShaderProgram shader, GLTexture tex, string uniform, int slot)
        {
            GL.ActiveTexture(TextureUnit.Texture0 + slot);
            tex.Bind();
            shader.SetInt(uniform, slot);
        }
    }
}
