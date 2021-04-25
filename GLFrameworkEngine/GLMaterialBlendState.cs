using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using OpenTK;

namespace GLFrameworkEngine
{
    [Serializable]
    public class GLMaterialBlendState
    {
        public bool BlendMask = false;

        public bool DepthTest = true;
        public DepthFunction DepthFunction = DepthFunction.Lequal;
        public bool DepthWrite = true;

        public bool AlphaTest = true;
        public AlphaFunction AlphaFunction = AlphaFunction.Gequal;
        public float AlphaValue = 0.5f;

        public BlendingFactorSrc ColorSrc = BlendingFactorSrc.SrcAlpha;
        public BlendingFactorDest ColorDst = BlendingFactorDest.OneMinusSrcAlpha;
        public BlendEquationMode ColorOp = BlendEquationMode.FuncAdd;

        public BlendingFactorSrc AlphaSrc = BlendingFactorSrc.One;
        public BlendingFactorDest AlphaDst = BlendingFactorDest.Zero;
        public BlendEquationMode AlphaOp = BlendEquationMode.FuncAdd;

        public BlendState State = BlendState.Opaque;

        public Vector4 Color = Vector4.Zero;

        public bool BlendColor = false;

        public enum BlendState
        {
            Opaque,
            Mask,
            Translucent,
            Custom,
        }

        public void RenderDepthTest()
        {
            if (DepthTest)
            {
                GL.Enable(EnableCap.DepthTest);
                GL.DepthFunc(DepthFunction);
                GL.DepthMask(DepthWrite);
            }
            else
                GL.Disable(EnableCap.DepthTest);
        }

        public void RenderAlphaTest()
        {
            if (AlphaTest)
            {
                GL.Enable(EnableCap.AlphaTest);
                GL.AlphaFunc(AlphaFunction, AlphaValue);
            }
            else
                GL.Disable(EnableCap.AlphaTest);
        }

        public void RenderBlendState()
        {
            if (BlendColor || BlendMask)
            {
                GL.Enable(EnableCap.Blend);
                GL.BlendFuncSeparate(ColorSrc, ColorDst, AlphaSrc, AlphaDst);
                GL.BlendEquationSeparate(ColorOp, AlphaOp);
                GL.BlendColor(Color.X, Color.Y, Color.Z, Color.W);
            }
            else
                GL.Disable(EnableCap.Blend);
        }
    }
}
