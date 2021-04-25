using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;

namespace GLFrameworkEngine
{
    public class EquirectangularRender
    {
        static Dictionary<string, GLTexture2D> cubemapCache = new Dictionary<string, GLTexture2D>();

        static int ID = -1;

        public static GLTexture2D CreateTextureRender(GLTexture texture, int arrayLevel, int mipLevel)
        {
            if (cubemapCache.ContainsKey(texture.ID.ToString()))
                return cubemapCache[texture.ID.ToString()];

            int width = texture.Width * 4;
            int height = texture.Height * 3;

            width = 512;
            height = 256;

            var shader = GlobalShaders.GetShader("EQUIRECTANGULAR");
            var textureOutput = GLTexture2D.CreateUncompressedTexture(width, height, PixelInternalFormat.Rgba32f);
            textureOutput.MipCount = texture.MipCount;

            texture.Bind();

            textureOutput.Bind();
            textureOutput.GenerateMipmaps();
            textureOutput.Unbind();

            Framebuffer frameBuffer = new Framebuffer(FramebufferTarget.Framebuffer, width, height);
            frameBuffer.Bind();

            GL.Disable(EnableCap.Blend);

            shader.Enable();
            shader.SetBoolToInt("is_array", texture is GLTextureCubeArray);

            if (texture is GLTextureCubeArray)
            {
                GL.ActiveTexture(TextureUnit.Texture1);
                texture.Bind();
                shader.SetInt("dynamic_texture_array", 1);
            }
            else
            {
                GL.ActiveTexture(TextureUnit.Texture1);
                texture.Bind();
                shader.SetInt("dynamic_texture", 1);
            }

            for (int i = 0; i < textureOutput.MipCount; i++)
            {
                int mipWidth = (int)(width * Math.Pow(0.5, i));
                int mipHeight = (int)(height * Math.Pow(0.5, i));
                frameBuffer.Resize(mipWidth, mipHeight);

                GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                    textureOutput.ID, i);

                shader.SetInt("arrayLevel", arrayLevel);
                shader.SetInt("mipLevel", i);

                GL.ClearColor(0, 0, 0, 0);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                GL.Viewport(0, 0, mipWidth, mipHeight);

                //Draw the texture onto the framebuffer
                ScreenQuadRender.Draw();
            }

            //Disable shader and textures
            GL.UseProgram(0);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            cubemapCache.Add(texture.ID.ToString(), textureOutput);
            return textureOutput;
        }
    }
}
