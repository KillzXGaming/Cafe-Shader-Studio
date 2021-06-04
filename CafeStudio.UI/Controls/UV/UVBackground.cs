using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using GLFrameworkEngine;

namespace CafeStudio.UI
{
    public class UVBackground
    {
        static VertexBufferObject vao;

        static int Length;

        public static void Init()
        {
            if (Length == 0)
            {
                int buffer = GL.GenBuffer();
                vao = new VertexBufferObject(buffer);
                vao.AddAttribute(0, 2, VertexAttribPointerType.Float, false, 16, 0);
                vao.AddAttribute(1, 2, VertexAttribPointerType.Float, false, 16, 8);
                vao.Initialize();

                Vector2[] positions = new Vector2[4]
                {
                    new Vector2(-1.0f, 1.0f),
                    new Vector2(-1.0f, -1.0f),
                    new Vector2(1.0f, 1.0f),
                    new Vector2(1.0f, -1.0f),
                };

                Vector2[] texCoords = new Vector2[4]
                {
                    new Vector2(0.0f, 1.0f),
                    new Vector2(0.0f, 0.0f),
                    new Vector2(1.0f, 1.0f),
                    new Vector2(1.0f, 0.0f),
                };

                List<float> list = new List<float>();
                for (int i = 0; i < 4; i++)
                {
                    list.Add(positions[i].X);
                    list.Add(positions[i].Y);
                    list.Add(texCoords[i].X);
                    list.Add(texCoords[i].Y);
                }

                Length = 4;

                float[] data = list.ToArray();
                GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * data.Length, data, BufferUsageHint.StaticDraw);
            }
        }

        public static void Draw(STGenericTexture texture,
            STGenericTextureMap textureMap, int width, int height, Vector2 aspectScale, Viewport2D.Camera2D camera)
        {
            Vector2 bgscale = new Vector2(100, 100);

            Init();

            GL.Disable(EnableCap.CullFace);

            var shader = GlobalShaders.GetShader("UV_WINDOW");
            shader.Enable();

            var cameraMtx = camera.ViewMatrix * camera.ProjectionMatrix;
            shader.SetMatrix4x4("mtxCam", ref cameraMtx);

            GL.ActiveTexture(TextureUnit.Texture1);
            BindTexture(texture, textureMap);
            shader.SetInt("uvTexture", 1);
            shader.SetInt("hasTexture", 1);
            shader.SetVector2("scale", bgscale * aspectScale);
            shader.SetVector2("texCoordScale", bgscale);
            shader.SetVector4("uColor", new Vector4(0.5f, 0.5f, 0.5f, 1.0f));

            if (texture != null) {
                shader.SetBoolToInt("isSRGB", texture.IsSRGB);
            }

            //Draw background
            vao.Enable(shader);
            vao.Use();
            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, Length);

            //Draw main texture quad inside boundings (0, 1)
            shader.SetVector2("scale", aspectScale);
            shader.SetVector2("texCoordScale", new Vector2(1));
            shader.SetVector4("uColor", new Vector4(1));

            vao.Enable(shader);
            vao.Use();
            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, Length);

            //Draw outline of boundings (0, 1)
            shader.SetInt("hasTexture", 0);
            shader.SetVector2("scale", aspectScale);
            shader.SetVector2("texCoordScale", new Vector2(1));
            shader.SetVector4("uColor", new Vector4(0,0,0,1));

            vao.Enable(shader);
            vao.Use();
            GL.LineWidth(1);
            GL.DrawArrays(PrimitiveType.LineLoop, 0, Length);

            GL.Enable(EnableCap.CullFace);
        }

        static void BindTexture(STGenericTexture tex, STGenericTextureMap texMap)
        {
            if (tex == null)
                return;

            if (tex.RenderableTex == null)
                tex.LoadRenderableTexture();

            var target = ((GLTexture)tex.RenderableTex).Target;
            var texID = tex.RenderableTex.ID;

            if (tex.Platform.OutputFormat == TexFormat.BC5_SNORM)
            {
                if (!GLTextureCache.DecodedFormats.ContainsKey(texID))
                {
                    var reloaded = GLTexture2D.FromGeneric(tex, new ImageParameters() {
                        UseSoftwareDecoder = true,
                    });
                    GLTextureCache.DecodedFormats.Add(texID, reloaded.ID);
                }
                texID = GLTextureCache.DecodedFormats[texID];
            }

            GL.BindTexture(target, texID);
            GL.TexParameter(target, TextureParameterName.TextureWrapS, (float)OpenGLHelper.WrapMode[texMap.WrapU]);
            GL.TexParameter(target, TextureParameterName.TextureWrapT, (float)OpenGLHelper.WrapMode[texMap.WrapV]);
            GL.TexParameter(target, TextureParameterName.TextureMinFilter, (int)OpenGLHelper.MinFilter[texMap.MinFilter]);
            GL.TexParameter(target, TextureParameterName.TextureMagFilter, (int)OpenGLHelper.MagFilter[texMap.MagFilter]);

            int[] mask = new int[4]
              {
                        GetSwizzle(tex.RedChannel),
                        GetSwizzle(tex.GreenChannel),
                        GetSwizzle(tex.BlueChannel),
                        GetSwizzle(tex.AlphaChannel),
              };
            GL.TexParameter(target, TextureParameterName.TextureSwizzleRgba, mask);
        }

        static int GetSwizzle(STChannelType channel)
        {
            switch (channel)
            {
                case STChannelType.Red: return (int)All.Red;
                case STChannelType.Green: return (int)All.Green;
                case STChannelType.Blue: return (int)All.Blue;
                case STChannelType.Alpha: return (int)All.Alpha;
                case STChannelType.One: return (int)All.One;
                case STChannelType.Zero: return (int)All.Zero;
                default: return 0;
            }
        }
    }
}
