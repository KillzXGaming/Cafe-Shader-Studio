using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Toolbox.Core;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using GLFrameworkEngine;

namespace AGraphicsLibrary
{
    /// <summary>
    /// A manager for keeping track of the scene's cubemaps.
    /// This will render cubemaps in the current scene given the list of renderable objects.
    /// </summary>
    public class CubemapManager
    {
        public static GLTextureCubeArray CubeMapTexture { get; set; }

        public const int CUBEMAP_UPSCALE_SIZE = 256;
        public const int CUBEMAP_SIZE = 128;
        public const int MAX_LAYER_COUNT = 8;

        //Keep things simple and use single mips for now then generate later
        public const int MAX_MIP_LEVEL = 1;

        public const bool SAVE_TO_DISK = false;

        public CubemapManager()
        {

        }

        public static void InitDefault(GLTextureCubeArray texture)
        {
            if (CubeMapTexture == null)
                CubeMapTexture = texture;
        }

        //Update all existing cubemap uint objects
        public static void GenerateCubemaps(List<GenericRenderer> targetModels)
        {
            if (CubeMapTexture != null)
                CubeMapTexture.Dispose();

            CubeMapTexture = new GLTextureCubeArray();
            CubeMapTexture.Bind();
            GL.TexParameter(CubeMapTexture.Target, TextureParameterName.TextureBaseLevel, 0);
            GL.TexParameter(CubeMapTexture.Target, TextureParameterName.TextureMaxLevel, 13);
            GL.TexParameter(CubeMapTexture.Target, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(CubeMapTexture.Target, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(CubeMapTexture.Target, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(CubeMapTexture.Target, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(CubeMapTexture.Target, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(CubeMapTexture.Target, TextureParameterName.TextureMinLod, 0.0f);
            GL.TexParameter(CubeMapTexture.Target, TextureParameterName.TextureMaxLod, 14.0f);
            GL.TexParameter(CubeMapTexture.Target, TextureParameterName.TextureLodBias, 0.0f);

            CubeMapTexture.Width = CUBEMAP_SIZE;
            CubeMapTexture.Height = CUBEMAP_SIZE;

            //Generate a fixed 8 cubemap array
            for (int mip = 0; mip < MAX_MIP_LEVEL; mip++)
            {
                int mipWidth = (int)(CUBEMAP_SIZE * Math.Pow(0.5, mip));
                int mipHeight = (int)(CUBEMAP_SIZE * Math.Pow(0.5, mip));

                GL.TexImage3D(CubeMapTexture.Target, 0, PixelInternalFormat.Rgba16f,
                    mipWidth, mipHeight, MAX_LAYER_COUNT * 6, mip,
                    PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);
            }

            CubeMapTexture.Unbind();

            GLTextureCube cubemapTexture = GLTextureCube.CreateEmptyCubemap(
                CUBEMAP_UPSCALE_SIZE, PixelInternalFormat.Rgba16f, PixelFormat.Rgba, PixelType.Float, MAX_MIP_LEVEL);
  
            //Get a list of cubemaps in the scene
            //The lighting engine has cube map objects with the object placement to draw
            var lightingEngine = LightingEngine.LightSettings;
            var cubeMapUints = lightingEngine.CubeMaps.CubeMapObjects;
            int layer = 0;
            foreach (var cubeMap in cubeMapUints)
            {
                var cUint = cubeMap.CubeMapUint;
                //Cubemap has no area assigned skip it
                if (cubeMap.CubeMapUint.Name == string.Empty)
                    continue;

                //Setup the camera to render the cube map faces
                CubemapCamera camera = new CubemapCamera(
                    new Vector3(cUint.Position.X, cUint.Position.Y, cUint.Position.Z)
                    * GLContext.PreviewScale, cUint.Near, cUint.Far);

                var context = new GLContext();
                context.Camera = camera;

                GenerateCubemap(context, cubemapTexture, camera, targetModels, MAX_MIP_LEVEL);

                //HDR encode and output into the array
                CubemapHDREncodeRT.CreateCubemap(context, cubemapTexture, CubeMapTexture, layer, MAX_MIP_LEVEL, false, true);

                if (SAVE_TO_DISK)
                {
                    cubemapTexture.SaveDDS(cubeMap.Name + "default.dds");
                    CubeMapTexture.SaveDDS(cubeMap.Name + "hdr.dds");
                }
       
                layer++;    
            }

            //Just generate mips to keep things easier
            CubeMapTexture.Bind();
            CubeMapTexture.GenerateMipmaps();
            CubeMapTexture.Unbind();
        }

        static void GenerateCubemap(GLContext control, GLTextureCube texture,
             CubemapCamera camera, List<GenericRenderer> models, int numMips)
        {
            GL.BindTexture(TextureTarget.TextureCubeMap, 0);

            int size = CUBEMAP_UPSCALE_SIZE;

            Framebuffer frameBuffer = new Framebuffer(FramebufferTarget.Framebuffer);
            frameBuffer.SetDrawBuffers(DrawBuffersEnum.ColorAttachment0);
            frameBuffer.Bind();

            //Render all 6 faces
            for (int mip = 0; mip < numMips; mip++)
            {
                int mipWidth = (int)(size * Math.Pow(0.5, mip));
                int mipHeight = (int)(size * Math.Pow(0.5, mip));

                //frameBuffer.Resize(mipWidth, mipHeight);
                GL.Viewport(0, 0, mipWidth, mipHeight);

                for (int i = 0; i < 6; i++)
                {
                    //First filter a normal texture 2d face
                    GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
                                   FramebufferAttachment.ColorAttachment0,
                                    TextureTarget.TextureCubeMapPositiveX + i, texture.ID, mip);

                    //point camera in the right direction
                    camera.SwitchToFace(i);

                    GL.ClearColor(0, 0, 0, 1);
                    GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                    //render scene to fbo, and therefore to the current face of the cubemap
                    foreach (var model in models)
                        model.DrawCubeMapScene(control);
                }
            }
         
            var errorcheck = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (errorcheck != FramebufferErrorCode.FramebufferComplete)
                throw new Exception(errorcheck.ToString());

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            frameBuffer.Dispoe();
            frameBuffer.DisposeRenderBuffer();

            GL.UseProgram(0);
        }
    }

    public class CubeMapRender
    {
        public CubeMapObject CubeMapObject { get; set; }

        public GLTextureCube RenderedTexture { get; set; }
        public GLTextureCube IrradianceTexture { get; set; }
    }

    public class CubemapCamera : Camera
    {
        private float pitch = 0;
        private float yaw = 0;

        public CubemapCamera(Vector3 center, float znear = 1, float zfar = 500000)
        {
            Translation = center;
            ZNear = znear;
            ZFar = zfar;
            FovDegrees = 90.0f;
            Width = 1;
            Height = 1;
            this.CreateProjectionMatrix();
        }

        public void SwitchToFace(int faceIndex)
        {
            switch (faceIndex)
            {
                case 0:
                    pitch = 0;
                    yaw = 90;
                    break;
                case 1:
                    pitch = 0;
                    yaw = -90;
                    break;
                case 2:
                    pitch = 90;
                    yaw = 0;
                    break;
                case 3:
                    pitch = -90;
                    yaw = 0;
                    break;
                case 4:
                    pitch = 0;
                    yaw = 0;
                    break;
                case 5:
                    pitch = 0;
                    yaw = 180;
                    break;
            }
            this.updateViewMatrix();
        }

        private void CreateProjectionMatrix()
        {
            float y_scale = (float)((1f / Math.Tan(MathHelper.DegreesToRadians(FovDegrees / 2f))));
            float x_scale = y_scale / AspectRatio;
            float frustum_length = ZFar - ZNear;

            projectionMatrix.M11 = x_scale;
            projectionMatrix.M22 = y_scale;
            projectionMatrix.M33 = -((ZFar + ZNear) / frustum_length);
            projectionMatrix.M34 = -1;
            projectionMatrix.M43 = -((2 * ZNear * ZFar) / frustum_length);
            projectionMatrix.M44 = 0;
        }

        private void updateViewMatrix()
        {
            Vector3 position = new Vector3(-Translation.X, -Translation.Y, -Translation.Z);

            viewMatrix = Matrix4.Identity;
            viewMatrix *= Matrix4.CreateTranslation(position);
            viewMatrix *= Matrix4.CreateRotationX(MathHelper.DegreesToRadians(pitch));
            viewMatrix *= Matrix4.CreateRotationY(MathHelper.DegreesToRadians(yaw));
            ProjectionMatrix = projectionMatrix;
            ViewMatrix = viewMatrix;
        }
    }
}
