using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using GLFrameworkEngine;

namespace AGraphicsLibrary
{
    public class ColorCorrectionManager3D
    {
        public static Framebuffer Filter;

        public static Vector2 Offset { get;set; }

        public static void Init(int size)
        {
            Filter = new Framebuffer(FramebufferTarget.Framebuffer, size, size, PixelInternalFormat.R11fG11fB10f);
            DrawBuffersEnum[] drawBuffers = new DrawBuffersEnum[size];
            for (int i = 0; i < size; i++)
                drawBuffers[i] = DrawBuffersEnum.ColorAttachment0 + i;

            Filter.SetDrawBuffers(drawBuffers);
        }

        static bool saved = false;

        public static void CreateColorLookupTexture(GLTexture3D output)
        {
            GL.BindTexture(output.Target, 0);

            int LUT_SIZE = output.Width;

            if (Filter == null)
                Init(LUT_SIZE);

            Filter.Bind();

            for (int i = 0; i < 8; i++)
            {
                GL.FramebufferTextureLayer(FramebufferTarget.Framebuffer,
                    FramebufferAttachment.ColorAttachment0 + i, output.ID, 0, i);
            }

            GL.Viewport(0, 0, LUT_SIZE, LUT_SIZE);
            GL.ClearColor(0, 0, 0, 0);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            var shader = GlobalShaders.GetShader("COLOR_CORRECTION");
            shader.Enable();

            UpdateUniforms(shader);
            ScreenQuadRender.Draw();
            Filter.Unbind();

            GL.UseProgram(0);

            var errorcheck = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (errorcheck != FramebufferErrorCode.FramebufferComplete)
                throw new Exception(errorcheck.ToString());
        }

        static void UpdateUniforms(ShaderProgram shader)
        {
            var lightingEngine = LightingEngine.LightSettings;

            var colorCorrection = lightingEngine.ColorCorrection;
            shader.SetFloat("uGamma", colorCorrection.Gamma);
            shader.SetFloat("uBrightness", colorCorrection.Brightness);
            shader.SetFloat("uSaturation", colorCorrection.Saturation);
            shader.SetFloat("uHue", colorCorrection.Hue);
            shader.SetVector4("uToycamLevel1", colorCorrection.ToyCamLevel1.ToVector4());
            shader.SetVector4("uToycamLevel2", colorCorrection.ToyCamLevel2.ToVector4());

            var curves = colorCorrection.Level;
            if (curves != null)
            {
                float numValues = 256;

                float amount = 1.0f / (numValues - 1f);
                float time = 0;

                //Level curve
                for (int j = 0; j < numValues; j++)
                {
                    float x = Interpolate(curves[0], time);
                    float y = Interpolate(curves[1], time);
                    float z = Interpolate(curves[2], time);
                    float w = Interpolate(curves[3], time);

                    Vector4 curveValues = new Vector4(x, y, z, w);
                    GL.Uniform4(GL.GetUniformLocation(shader.program, $"uCurve0[{j}]"), curveValues);
                    GL.Uniform4(GL.GetUniformLocation(shader.program, $"uCurve1[{j}]"), curveValues);

                    time += amount;
                }
            }
        }

        static float Interpolate(AampLibraryCSharp.Curve curve, float t)
        {
            switch (curve.CurveType)
            {
                case AampLibraryCSharp.CurveType.Hermit2D:
                    return InterpolateHermite2D(t, curve.NumUses, curve.valueFloats);
                default:
                    return 0.0f;
                   //     throw new Exception($"Unsupported color type! {curve.CurveType}");
            }
        }

        //https://github.com/open-ead/sead/blob/16d150caade87410309acbc04069ec9067c78fd6/modules/src/hostio/seadHostIOCurve.cpp
        static float InterpolateHermite2D(float t, uint numUses, float[] f)
        {
            int n = (int)numUses / 3;
            if (f[0] >= t)
                return f[1];

            if (f[3 * (n - 1)] <= t)
                return f[3 * (n - 1) + 1];

            for (int i = 0; i < n; ++i)
            {
                var j = 3 * i;
                if (f[j + 3] > t)
                {
                    var x = (t - f[j]) / (f[j + 3] - f[j]);
                    return ((2 * x * x * x) - (3 * x * x) + 1) * f[j + 1]  // (2t^3 - 3t^2 + 1)p0
                           + ((-2 * x * x * x) + (3 * x * x)) * f[j + 4]   // (-2t^3 + 2t^2)p1
                           + ((x * x * x) - (x * x)) * f[j + 5]            // (t^3 - t^2)m1
                           + ((x * x * x) - (2 * x * x) + x) * f[j + 2]    // (t^3 - 2t^2 + t)m0
                        ;
                }
            }

            return 0;
        }

        static float fracPart(float x) {
            return x - ((int)x);
        }
    }
}
