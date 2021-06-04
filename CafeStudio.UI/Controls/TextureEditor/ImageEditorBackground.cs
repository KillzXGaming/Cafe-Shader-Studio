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
    public class ImageEditorBackground
    {
        static VertexBufferObject vao;

        static int Length;

        static int mipLevel = 0;

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

        public static void Draw(STGenericTexture texture, int width, int height, Viewport2D.Camera2D camera, bool showAlpha)
        {
            Vector3 scale = new Vector3(1, 1, 1);
            scale = UpdateAspectScale(scale, width, height, texture);

            Init();

            var shader = GlobalShaders.GetShader("IMAGE_EDITOR");
            shader.Enable();

            var cameraMtx = Matrix4.CreateScale(100) * camera.ProjectionMatrix;
            shader.SetMatrix4x4("mtxCam", ref cameraMtx);

            GL.Disable(EnableCap.Blend);

            DrawBackground(shader);

            cameraMtx = camera.ViewMatrix * camera.ProjectionMatrix;
            shader.SetMatrix4x4("mtxCam", ref cameraMtx);

            DrawImage(shader, texture, scale.Xy, showAlpha);
        }

        static GLMaterialBlendState ImageBlendState = new GLMaterialBlendState() 
        {
            BlendColor = true,
        };

        static void DrawImage(ShaderProgram shader, STGenericTexture texture, Vector2 scale, bool showAlpha)
        {
            ImageBlendState.RenderBlendState();

            //Draw main texture quad inside boundings (0, 1)
             shader.SetVector2("scale", scale);
           // shader.SetVector2("scale", new Vector2(1));
            shader.SetVector4("uColor", new Vector4(1));
            shader.SetBoolToInt("isSRGB", texture.IsSRGB);
            shader.SetBoolToInt("displayAlpha", showAlpha);
            shader.SetVector2("texCoordScale", new Vector2(1));
            shader.SetFloat("width", texture.Width);
            shader.SetFloat("height", texture.Height);
            shader.SetInt("currentMipLevel", 0);

            GL.ActiveTexture(TextureUnit.Texture1);
            BindTexture(texture);
            shader.SetInt("textureInput", 1);
            shader.SetInt("hasTexture", 1);

            //Draw background
            vao.Enable(shader);
            vao.Use();
            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, Length);
        }

        static void DrawBackground(ShaderProgram shader)
        {
            var backgroundTexture = IconManager.GetTextureIcon("CHECKERBOARD");

            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, backgroundTexture);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            shader.SetInt("backgroundTexture", 1);

            shader.SetVector2("scale", new Vector2(30));
            shader.SetVector2("texCoordScale", new Vector2(30));

            shader.SetVector4("uColor", new Vector4(1));
            
            //Draw background
            vao.Enable(shader);
            vao.Use();
            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, Length);
        }

        static Vector3 UpdateAspectScale(Vector3 scale, int width, int height, STGenericTexture tex)
        {
            //Adjust scale via aspect ratio
            if (width > height)
            {
                float aspect = (float)tex.Width / (float)tex.Height;
                scale.X *= aspect;
            }
            else
            {
                float aspect = (float)tex.Height / (float)tex.Width;
                scale.Y *= aspect;
            }
            return scale;
        }

        static void BindTexture(STGenericTexture tex)
        {
            if (tex == null)
                return;

            if (tex.RenderableTex == null)
                tex.LoadRenderableTexture();

            var target = ((GLTexture)tex.RenderableTex).Target;
            var texID = tex.RenderableTex.ID;

            if (tex.Platform.OutputFormat == TexFormat.BC5_SNORM) {
                if (!GLTextureCache.DecodedFormats.ContainsKey(texID)) {
                    var reloaded = GLTexture2D.FromGeneric(tex, new ImageParameters()
                    {
                        UseSoftwareDecoder = (tex.Platform.OutputFormat == TexFormat.BC5_SNORM),
                    });
                    GLTextureCache.DecodedFormats.Add(texID, reloaded.ID);
                }
                texID = GLTextureCache.DecodedFormats[texID];
            }

            //Fixed mip layer with nearest setting
            GL.BindTexture(target, texID);
            GL.TexParameter(target, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(target, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(target, TextureParameterName.TextureMaxLod, (int)15);
            GL.TexParameter(target, TextureParameterName.TextureMinLod, (int)mipLevel);

            int[] mask = new int[4] { (int)All.Red, (int)All.Green, (int)All.Blue, (int)All.Alpha };
            if (ImageEditor.UseChannelComponents)
            {
                mask = new int[4]
                {
                    OpenGLHelper.GetSwizzle(tex.RedChannel),
                    OpenGLHelper.GetSwizzle(tex.GreenChannel),
                    OpenGLHelper.GetSwizzle(tex.BlueChannel),
                    //For now prevent full disappearance of zero alpha types on alpha channel.
                    //This is typically used on BC4 and BC5 types when not using alpha data.
                    tex.AlphaChannel == STChannelType.Zero ? 1 : OpenGLHelper.GetSwizzle(tex.AlphaChannel),
                };
            }

            GL.TexParameter(target, TextureParameterName.TextureSwizzleRgba, mask);
        }
    }
}
