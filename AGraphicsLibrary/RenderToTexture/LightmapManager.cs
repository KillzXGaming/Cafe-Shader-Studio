using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using AGraphicsLibrary;
using Toolbox.Core;
using GLFrameworkEngine;

namespace AGraphicsLibrary
{
    public class LightmapManager
    {
        public static Framebuffer FilterLevel0;
        public static Framebuffer FilterLevel1;

        public static GLTexture2D NormalsTexture;
        static GLTexture2D GradientTexture;

        static UniformBlock LightingDirectionBlock;
        static UniformBlock LightingColorBlock;

        public static Vector2 Offset { get; set; }

        public static void Init(int size)
        {
            DrawBuffersEnum[] buffers = new DrawBuffersEnum[6]
            {
                DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1,
                DrawBuffersEnum.ColorAttachment2, DrawBuffersEnum.ColorAttachment3,
                DrawBuffersEnum.ColorAttachment4, DrawBuffersEnum.ColorAttachment5,
           };

            FilterLevel0 = new Framebuffer(FramebufferTarget.Framebuffer);
            FilterLevel1 = new Framebuffer(FramebufferTarget.Framebuffer);
            FilterLevel0.SetDrawBuffers(buffers);
            FilterLevel1.SetDrawBuffers(buffers);

            var normalsDDS = new DDS(new System.IO.MemoryStream(Properties.Resources.normals));
            NormalsTexture = GLTexture2D.FromGeneric(normalsDDS, new ImageParameters());
            GradientTexture = GLTexture2D.FromBitmap(Properties.Resources.gradient);

            NormalsTexture.Bind();
            NormalsTexture.WrapR = TextureWrapMode.ClampToEdge;
            NormalsTexture.WrapT = TextureWrapMode.ClampToEdge;
            NormalsTexture.WrapS = TextureWrapMode.ClampToEdge;
            NormalsTexture.MinFilter = TextureMinFilter.Nearest;
            NormalsTexture.MagFilter = TextureMagFilter.Nearest;
            NormalsTexture.UpdateParameters();
            NormalsTexture.Unbind();

            LightingDirectionBlock = new UniformBlock();
            LightingColorBlock = new UniformBlock();
        }

        public static void CreateLightmapTexture(GLContext control, 
            EnvironmentGraphics environmentSettings, int areaIndex, GLTextureCube output)
        {
            //Force generate mipmaps to update the mip allocation so mips can be assigned.
            output.Bind();
            GL.GenerateMipmap(GenerateMipmapTarget.TextureCubeMap);
            GL.BindTexture(TextureTarget.TextureCubeMap, 0);
            GL.Enable(EnableCap.TextureCubeMapSeamless);

            if (FilterLevel0 == null)
                Init(output.Width);

            int CUBE_SIZE = output.Width;

            GL.Disable(EnableCap.FramebufferSrgb);

            FilterLevel0.Bind();
            LoadCubemapLevel(control, CUBE_SIZE, 0, environmentSettings, areaIndex, output.ID);
            FilterLevel0.Unbind();

            FilterLevel1.Bind();
            LoadCubemapLevel(control, CUBE_SIZE / 2, 1, environmentSettings, areaIndex, output.ID);
            FilterLevel1.Unbind();
        }

        static void LoadCubemapLevel(GLContext control, int size, int level, EnvironmentGraphics environmentSettings, int areaIndex, int ID)
        {
            GL.Viewport(0, 0, size, size);

            var shader = GlobalShaders.GetShader("LIGHTMAP");
            shader.Enable();

            UpdateUniforms(control, shader, level == 0, environmentSettings, areaIndex);

            for (int i = 0; i < 6; i++)
            {
                GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
                    FramebufferAttachment.ColorAttachment0 + i,
                    TextureTarget.TextureCubeMapPositiveX + i, ID, level);
            }

            GL.ClearColor(0, 0, 0, 0);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            ScreenQuadRender.Draw();

            var errorcheck = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (errorcheck != FramebufferErrorCode.FramebufferComplete)
                throw new Exception(errorcheck.ToString());

            GL.UseProgram(0);
        }

        static void UpdateUniforms(GLContext control, ShaderProgram shader, bool isLightPass,
             EnvironmentGraphics environmentSettings, int areaIndex)
        {
            GL.ActiveTexture(TextureUnit.Texture0 + 1);
            NormalsTexture.Bind();
            shader.SetInt("sampler0", 1);

            GL.ActiveTexture(TextureUnit.Texture0 + 2);
            GradientTexture.Bind();
            shader.SetInt("sampler1", 2);

            //Set lighting data
            var lightDir = environmentSettings.DirectionalLights.FirstOrDefault();

            LightingDirectionBlock.Buffer.Clear();
            LightingDirectionBlock.Add(new Vector4(0));
            LightingDirectionBlock.Add(new Vector4(lightDir.Direction.X, lightDir.Direction.Y, lightDir.Direction.Z, 0));
            //Not hemi direction? 
            LightingDirectionBlock.Add(new int[4] { -2147483648, -1082130432, -2147483648, 0 });
            LightingDirectionBlock.RenderBuffer(shader.program, "cbuf_block4");

            LightingColorBlock.Buffer.Clear();
            LightingColorBlock.Add(FillColorBlock(isLightPass, environmentSettings, areaIndex));
            LightingColorBlock.RenderBuffer(shader.program, "cbuf_block5");
        }

        static byte[] FillColorBlock(bool isLightPass, EnvironmentGraphics environmentSettings, int areaIndex)
        {
            var mem = new System.IO.MemoryStream();
            using (var writer = new Toolbox.Core.IO.FileWriter(mem))
            {
                writer.Write(new Vector4(1, 0, 0, 0));
                writer.Write(new Vector4(1, 0, 0, 0));
                writer.SeekBegin(128);
                writer.Write(new Vector4(0.015625f, 0, 0, 0));
                writer.Write(new Vector4(0.078125f, 0, 0, 0));
                writer.SeekBegin(256);

                var lightDir = environmentSettings.DirectionalLights.FirstOrDefault();
                var hemi = environmentSettings.GetAreaHemisphereLight("course", areaIndex);

                //Light pass uses directional lights.
                if (isLightPass)
                {
                     writer.Write(lightDir.BacksideColor.ToVector4() * lightDir.Intensity);
                     writer.Write(lightDir.DiffuseColor.ToVector4() * lightDir.Intensity);
                }
                else //No light pass for shadow areas.
                {
                    writer.Write(new Vector4(0, 0, 0, 1));
                    writer.Write(new Vector4(0, 0, 0, 1));
                }

                writer.Write(hemi.GroundColor.ToVector4() * hemi.Intensity);
                writer.Write(hemi.SkyColor.ToVector4() * hemi.Intensity);
            }
            return mem.ToArray();
        }
    }
}
