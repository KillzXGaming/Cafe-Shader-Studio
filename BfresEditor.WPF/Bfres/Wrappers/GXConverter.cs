using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BfresLibrary;
using BfresLibrary.GX2;
using Toolbox.Core;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using GLFrameworkEngine;

namespace BfresEditor
{
    public class GXConverter
    {
        public static void ConvertRenderState(FMAT mat, RenderState renderState)
        {
            if (renderState != null)
                ConvertWiiURenderState(mat, renderState);

            //Switch support
            if (mat.Material.RenderInfos.ContainsKey("gsys_render_state_mode"))
                ConvertSwitchRenderState(mat);
        }

        static void ConvertWiiURenderState(FMAT mat, RenderState renderState)
        {
            var alphaControl = renderState.AlphaControl;
            var depthControl = renderState.DepthControl;
            var blendControl = renderState.BlendControl;
            var blendColor = renderState.BlendColor;

            mat.CullBack = renderState.PolygonControl.CullBack;
            mat.CullFront = renderState.PolygonControl.CullFront;

            var mode = renderState.FlagsMode;
            if (mode == RenderStateFlagsMode.Opaque)
                mat.BlendState.State = GLMaterialBlendState.BlendState.Opaque;
            else if (mode == RenderStateFlagsMode.Translucent)
                mat.BlendState.State = GLMaterialBlendState.BlendState.Translucent;
            else if (mode == RenderStateFlagsMode.AlphaMask)
                mat.BlendState.State = GLMaterialBlendState.BlendState.Mask;
            else
                mat.BlendState.State = GLMaterialBlendState.BlendState.Custom;

            mat.IsTransparent = mode != RenderStateFlagsMode.Opaque;

            mat.BlendState.ColorDst = (BlendingFactorDest)ConvertBlend(blendControl.ColorDestinationBlend);
            mat.BlendState.ColorSrc = (BlendingFactorSrc)ConvertBlend(blendControl.ColorSourceBlend);
            mat.BlendState.AlphaDst = (BlendingFactorDest)ConvertBlend(blendControl.AlphaDestinationBlend);
            mat.BlendState.AlphaSrc = (BlendingFactorSrc)ConvertBlend(blendControl.AlphaSourceBlend);
            mat.BlendState.AlphaOp = ConvertOp(blendControl.AlphaCombine);
            mat.BlendState.ColorOp = ConvertOp(blendControl.ColorCombine);

            mat.BlendState.Color = new OpenTK.Vector4(blendColor[0], blendColor[1], blendColor[2], blendColor[3]);
            mat.BlendState.DepthTest = depthControl.DepthTestEnabled;
            mat.BlendState.DepthWrite = depthControl.DepthWriteEnabled;
            mat.BlendState.AlphaTest = alphaControl.AlphaTestEnabled;
            mat.BlendState.AlphaValue = renderState.AlphaRefValue;
            mat.BlendState.BlendMask = renderState.ColorControl.BlendEnableMask == 1;
            //Todo the blend state flags seem off? This works for now.
            mat.BlendState.BlendColor = renderState.FlagsBlendMode != RenderStateFlagsBlendMode.None;
            switch (alphaControl.AlphaFunc)
            {
                case GX2CompareFunction.Always:
                    mat.BlendState.AlphaFunction = AlphaFunction.Always;
                    break;
                case GX2CompareFunction.Greater:
                    mat.BlendState.AlphaFunction = AlphaFunction.Greater;
                    break;
                case GX2CompareFunction.GreaterOrEqual:
                    mat.BlendState.AlphaFunction = AlphaFunction.Gequal;
                    break;
                case GX2CompareFunction.Equal:
                    mat.BlendState.AlphaFunction = AlphaFunction.Equal;
                    break;
                case GX2CompareFunction.LessOrEqual:
                    mat.BlendState.AlphaFunction = AlphaFunction.Lequal;
                    break;
                case GX2CompareFunction.NotEqual:
                    mat.BlendState.AlphaFunction = AlphaFunction.Notequal;
                    break;
                case GX2CompareFunction.Never:
                    mat.BlendState.AlphaFunction = AlphaFunction.Never;
                    break;
            }
        }

        static void ConvertSwitchRenderState(FMAT mat)
        {
            string blend = mat.GetRenderInfo("gsys_render_state_blend_mode");
            //Alpha test
            string alphaTest = mat.GetRenderInfo("gsys_alpha_test_enable");
            string alphaFunc = mat.GetRenderInfo("gsys_alpha_test_func");
            float alphaValue = mat.GetRenderInfo("gsys_alpha_test_value");

            string colorOp = mat.GetRenderInfo("gsys_color_blend_rgb_op");
            string colorDst = mat.GetRenderInfo("gsys_color_blend_rgb_dst_func");
            string colorSrc = mat.GetRenderInfo("gsys_color_blend_rgb_src_func");
            float[] blendColorF32 = mat.Material.RenderInfos["gsys_color_blend_const_color"].GetValueSingles();

            string alphaOp = mat.GetRenderInfo("gsys_color_blend_alpha_op");
            string alphaDst = mat.GetRenderInfo("gsys_color_blend_alpha_dst_func");
            string alphaSrc = mat.GetRenderInfo("gsys_color_blend_alpha_src_func");

            string depthTest = mat.GetRenderInfo("gsys_depth_test_enable");
            string depthTestFunc = mat.GetRenderInfo("gsys_depth_test_func");
            string depthWrite = mat.GetRenderInfo("gsys_depth_test_write");
            string state = mat.GetRenderInfo("gsys_render_state_mode");

            if (state == "opaque")
                mat.BlendState.State = GLMaterialBlendState.BlendState.Opaque;
            else if (state == "translucent")
                mat.BlendState.State = GLMaterialBlendState.BlendState.Translucent;
            else if (state == "mask")
                mat.BlendState.State = GLMaterialBlendState.BlendState.Mask;
            else
                mat.BlendState.State = GLMaterialBlendState.BlendState.Custom;

            string displayFace = mat.GetRenderInfo("gsys_render_state_display_face");
            if (displayFace == "front")
            {
                mat.CullFront = false;
                mat.CullBack = true;
            }
            if (displayFace == "back")
            {
                mat.CullFront = true;
                mat.CullBack = false;
            }
            if (displayFace == "both")
            {
                mat.CullFront = false;
                mat.CullBack = false;
            }
            if (displayFace == "none")
            {
                mat.CullFront = true;
                mat.CullBack = true;
            }

            if (!string.IsNullOrEmpty(state) && state != "opaque")
                mat.IsTransparent = true;

            mat.BlendState.Color = new OpenTK.Vector4(blendColorF32[0], blendColorF32[1], blendColorF32[2], blendColorF32[3]);
            mat.BlendState.BlendColor = blend == "color";
            mat.BlendState.DepthTest = depthTest == "true";
            mat.BlendState.DepthWrite = depthWrite == "true";
            mat.BlendState.AlphaTest = alphaTest == "true";
            mat.BlendState.AlphaValue = alphaValue;

            if (alphaFunc == "always")
                mat.BlendState.AlphaFunction = AlphaFunction.Always;
            if (alphaFunc == "equal")
                mat.BlendState.AlphaFunction = AlphaFunction.Equal;
            if (alphaFunc == "lequal")
                mat.BlendState.AlphaFunction = AlphaFunction.Lequal;
            if (alphaFunc == "gequal")
                mat.BlendState.AlphaFunction = AlphaFunction.Gequal;
            if (alphaFunc == "less")
                mat.BlendState.AlphaFunction = AlphaFunction.Less;
            if (alphaFunc == "greater")
                mat.BlendState.AlphaFunction = AlphaFunction.Greater;
            if (alphaFunc == "never")
                mat.BlendState.AlphaFunction = AlphaFunction.Never;
        }

        static BlendEquationMode ConvertOp(GX2BlendCombine func)
        {
            switch (func)
            {
                case GX2BlendCombine.Add: return BlendEquationMode.FuncAdd;
                case GX2BlendCombine.SourceMinusDestination: return BlendEquationMode.FuncSubtract;
                case GX2BlendCombine.DestinationMinusSource: return BlendEquationMode.FuncReverseSubtract;
                case GX2BlendCombine.Maximum: return BlendEquationMode.Max;
                case GX2BlendCombine.Minimum: return BlendEquationMode.Min;
                default: return BlendEquationMode.FuncAdd;

            }
        }


        static BlendingFactor ConvertBlend(GX2BlendFunction func)
        {
            switch (func)
            {
                case GX2BlendFunction.ConstantAlpha: return BlendingFactor.ConstantAlpha;
                case GX2BlendFunction.ConstantColor: return BlendingFactor.ConstantColor;
                case GX2BlendFunction.DestinationColor: return BlendingFactor.DstColor;
                case GX2BlendFunction.DestinationAlpha: return BlendingFactor.DstAlpha;
                case GX2BlendFunction.One: return BlendingFactor.One;
                case GX2BlendFunction.OneMinusConstantAlpha: return BlendingFactor.OneMinusConstantAlpha;
                case GX2BlendFunction.OneMinusConstantColor: return BlendingFactor.OneMinusConstantColor;
                case GX2BlendFunction.OneMinusDestinationAlpha: return BlendingFactor.OneMinusDstAlpha;
                case GX2BlendFunction.OneMinusDestinationColor: return BlendingFactor.OneMinusDstColor;
                case GX2BlendFunction.OneMinusSourceAlpha: return BlendingFactor.OneMinusSrcAlpha;
                case GX2BlendFunction.OneMinusSourceColor: return BlendingFactor.OneMinusSrcColor;
                case GX2BlendFunction.OneMinusSource1Alpha: return (BlendingFactor)BlendingFactorSrc.OneMinusSrc1Alpha;
                case GX2BlendFunction.OneMinusSource1Color: return (BlendingFactor)BlendingFactorSrc.OneMinusSrc1Color;
                case GX2BlendFunction.SourceAlpha: return BlendingFactor.SrcAlpha;
                case GX2BlendFunction.SourceAlphaSaturate: return BlendingFactor.SrcAlphaSaturate;
                case GX2BlendFunction.Source1Alpha: return BlendingFactor.Src1Alpha;
                case GX2BlendFunction.SourceColor: return BlendingFactor.SrcColor;
                case GX2BlendFunction.Source1Color: return BlendingFactor.Src1Color;
                case GX2BlendFunction.Zero: return BlendingFactor.Zero;
                default: return BlendingFactor.One;
            }
        }

        public static STTextureMinFilter ConvertMinFilter(GX2TexMipFilterType mip, GX2TexXYFilterType wrap)
        {
            if (mip == GX2TexMipFilterType.Linear)
            {
                switch (wrap)
                {
                    case GX2TexXYFilterType.Bilinear: return STTextureMinFilter.LinearMipmapLinear;
                    case GX2TexXYFilterType.Point: return STTextureMinFilter.NearestMipmapLinear;
                    default: return STTextureMinFilter.LinearMipmapNearest;
                }
            }
            else if (mip == GX2TexMipFilterType.Point)
            {
                switch (wrap)
                {
                    case GX2TexXYFilterType.Bilinear: return STTextureMinFilter.LinearMipmapNearest;
                    case GX2TexXYFilterType.Point: return STTextureMinFilter.NearestMipmapNearest;
                    default: return STTextureMinFilter.NearestMipmapLinear;
                }
            }
            else
            {
                switch (wrap)
                {
                    case GX2TexXYFilterType.Bilinear: return STTextureMinFilter.Linear;
                    case GX2TexXYFilterType.Point: return STTextureMinFilter.Nearest;
                    default: return STTextureMinFilter.Linear;
                }
            }
        }

        public static STTextureMagFilter ConvertMagFilter(GX2TexXYFilterType wrap)
        {
            switch (wrap)
            {
                case GX2TexXYFilterType.Bilinear: return STTextureMagFilter.Linear;
                case GX2TexXYFilterType.Point: return STTextureMagFilter.Nearest;
                default: return STTextureMagFilter.Linear;
            }
        }

        public static STTextureWrapMode ConvertWrapMode(GX2TexClamp wrap)
        {
            switch (wrap)
            {
                case GX2TexClamp.Wrap: return STTextureWrapMode.Repeat;
                case GX2TexClamp.Clamp: return STTextureWrapMode.Clamp;
                case GX2TexClamp.Mirror: return STTextureWrapMode.Mirror;
                case GX2TexClamp.MirrorOnce: return STTextureWrapMode.Mirror;
                case GX2TexClamp.MirrorOnceBorder: return STTextureWrapMode.Mirror;
                case GX2TexClamp.MirrorOnceHalfBorder: return STTextureWrapMode.Mirror;
                default: return STTextureWrapMode.Clamp;
            }
        }

        public static void ConvertPolygonState(FMAT mat, Material material)
        {
            if (material.RenderState == null)
                return;

            var polyMode = material.RenderState.PolygonControl;
            mat.CullBack = polyMode.CullBack;
            mat.CullFront = polyMode.CullFront;
        }
    }
}
