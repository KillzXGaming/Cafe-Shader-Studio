using AampLibraryCSharp;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GLFrameworkEngine;

namespace AGraphicsLibrary
{
    public class ProbeMapManager
    {
        static ProbeLighting ProbeLighting { get; set; }

        const ushort MAX_INDEX = 65526;

        static Dictionary<string, UniformBlock> Blocks = new Dictionary<string, UniformBlock>();

        public static void Prepare(byte[] fileData)
        {
            ProbeLighting = new ProbeLighting();
            ProbeLighting.LoadValues(AampFile.LoadFile(new System.IO.MemoryStream(fileData)));
        }

        public static bool Generate(GLContext control, GLTextureCube diffuseCubemap, int lightmapTexID, Vector3 position)
        {
            if (ProbeLighting == null || diffuseCubemap == null)
                return false;

            GL.BindTexture(TextureTarget.TextureCubeMap, 0);

            float[] shData = new float[0];
            foreach (var box in ProbeLighting.Boxes) {
                shData = SearchVoxelGrid(box, position);
                if (shData.Length > 0)
                    break;
            }

            //No probe data found. 
            if (shData.Length == 0)
                return false;

            var data = new Vector4[7]
                {
                    new Vector4(shData[0], shData[1], shData[2], shData[3]),
                    new Vector4(shData[4], shData[5], shData[6], shData[7]),
                    new Vector4(shData[8], shData[9], shData[10],shData[11]),
                    new Vector4(shData[12],shData[13],shData[14],shData[15]),
                    new Vector4(shData[16],shData[17],shData[18],shData[19]),
                    new Vector4(shData[20],shData[21],shData[22],shData[23]),
                    new Vector4(shData[24],shData[25],shData[26], 0.0f),
                };
            FilterProbeVolume(control, LightmapManager.NormalsTexture, diffuseCubemap, data, lightmapTexID, diffuseCubemap.Width, 0);
            FilterProbeVolume(control, LightmapManager.NormalsTexture, diffuseCubemap, data, lightmapTexID, diffuseCubemap.Width / 2, 1);

            return true;
        }

        static float[] SearchVoxelGrid(ProbeBoxObject box, Vector3 position)
        {
            //Check if position is in the boundings of the grid
            if (!box.Grid.HasHit(position.X, position.Y, position.Z))
                return new float[0];

            var step = box.Grid.Step;
            var dataBuffer = box.DataBuffer;
            var indexBuffer = box.IndexBuffer;

            int baseIndex = 0;
            for (float y = box.Grid.Min.Y; y < box.Grid.Max.Y; y += step.Y)
            {
                for (float x = box.Grid.Min.X; x < box.Grid.Max.X; x += step.X)
                {
                    for (float z = box.Grid.Min.Z; z < box.Grid.Max.Z; z += step.Z)
                    {
                        //Half the box size based on the step distance
                        //We will check each box and make sure the position is inside it
                        var size = step / 2;

                        List<ProbeSphere> blendSpheres = new List<ProbeSphere>();

                        Vector3[] spherePositions = new Vector3[8];
                        Vector3 probeMax = new Vector3(float.MinValue);
                        Vector3 probeMin = new Vector3(float.MaxValue);

                        //8 points
                        for (int i = 0; i < 8; i++)
                        {
                            switch (i)
                            {
                                //Top Left
                                case 0:
                                    spherePositions[i] = new Vector3(x - size.X, y + size.Y, z - size.Z);
                                    break;
                                //Top Right
                                case 1:
                                    spherePositions[i] = new Vector3(x + size.X, y + size.Y, z - size.Z);
                                    break;
                                //Bottom Left
                                case 2:
                                    spherePositions[i] = new Vector3(x - size.X, y - size.Y, z - size.Z);
                                    break;
                                //Bottom Right
                                case 3:
                                    spherePositions[i] = new Vector3(x + size.X, y - size.Y, z - size.Z);
                                    break;
                                //Top Left
                                case 4:
                                    spherePositions[i] = new Vector3(x - size.X, y + size.Y, z + size.Z);
                                    break;
                                //Top Right
                                case 5:
                                    spherePositions[i] = new Vector3(x + size.X, y + size.Y, z + size.Z);
                                    break;
                                //Bottom Left
                                case 6:
                                    spherePositions[i] = new Vector3(x - size.X, y - size.Y, z + size.Z);
                                    break;
                                //Bottom Right
                                case 7:
                                    spherePositions[i] = new Vector3(x + size.X, y - size.Y, z + size.Z);
                                    break;
                            }
                            probeMax.X = Math.Max(spherePositions[i].X, probeMax.X);
                            probeMax.Y = Math.Max(spherePositions[i].Y, probeMax.Y);
                            probeMax.Z = Math.Max(spherePositions[i].Z, probeMax.Z);
                            probeMin.X = Math.Min(spherePositions[i].X, probeMin.X);
                            probeMin.Y = Math.Min(spherePositions[i].Y, probeMin.Y);
                            probeMin.Z = Math.Min(spherePositions[i].Z, probeMin.Z);
                        }

                        //Check inside boundings of spheres
                        if (!HasVoxelHit(probeMin, probeMax, position))
                        {
                            //position not in current step, continue to next step
                            //Increase base data index by 8
                            baseIndex += 8;
                            continue;
                        }

                        //Load the data into spheres
                        for (int i = 0; i < 8; i++)
                        {
                            int index = baseIndex++;
                            int bufferIndex = indexBuffer.IndexBuffer[index];

                            float[] probeLighting = new float[(int)dataBuffer.PerProbeFloatNum];
                            //MAX INDEX values have no values so skip them into an empty buffer
                            if (bufferIndex < MAX_INDEX)
                            {
                                //Shift the buffer index as per max float value.
                                bufferIndex *= (int)dataBuffer.PerProbeFloatNum;

                                for (int p = 0; p < dataBuffer.PerProbeFloatNum; p++)
                                    probeLighting[p] = dataBuffer.DataBuffer[bufferIndex + p];
                            }
                            //Load the probe information for blending.
                            blendSpheres.Add(new ProbeSphere()
                            {
                                position = spherePositions[i],
                                SHDATA = probeLighting,
                            });
                        }

                        //Position is inside the 8 sphere boundings.
                        //Blend the SH data to get the final 27 float output
                        return CalculateCenterPoint(blendSpheres, position);
                    }
                }
            }
            return new float[0];
        }

        static float[] CalculateCenterPoint(List<ProbeSphere> spheres, Vector3 positon)
        {
           float[] buffer = new float[27];
            /*    for (int i = 0; i < buffer.Length; i++)
                   buffer[i] = 3.0f;
               return buffer;
               */

            return spheres.FirstOrDefault().SHDATA;

            for (int i = 0; i < spheres.Count; i++) {
                for (int j = 0; j < spheres[i].SHDATA.Length; j++) {
                    var value = spheres[i].SHDATA[j];

                }
            }
            return buffer;
        }

        static bool HasVoxelHit(Vector3 min, Vector3 max, Vector3 position)
        {
            return (position.X >= min.X && position.X <= max.X) &&
                   (position.Y >= min.Y && position.Y <= max.Y) &&
                   (position.Z >= min.Z && position.Z <= max.Z);
        }

        class ProbeSphere
        {
            public Vector3 position;

            public float[] SHDATA;
        }

        static void LoadUniforms(int programID, Vector4[] shData)
        {
            UniformBlock paramBlock = GetBlock("paramBlock");
            paramBlock.Add(new Vector4(1, 0, 0, 0));
            paramBlock.Add(new Vector4(0, 0, 0, 0));
            paramBlock.Add(new Vector4(0, 0, 0, 0));
            paramBlock.Add(new Vector4(0, 0, 0, 0));
            paramBlock.Add(new Vector4(1, 0, 0, 0));
            paramBlock.RenderBuffer(programID, "cbuf_block3");

            UniformBlock dataBlock = GetBlock("shBlock");
            dataBlock.Add(shData[0]);
            dataBlock.Add(shData[1]);
            dataBlock.Add(shData[2]);
            dataBlock.Add(shData[3]);
            dataBlock.Add(shData[4]);
            dataBlock.Add(shData[5]);
            dataBlock.Add(shData[6]);
            dataBlock.RenderBuffer(programID, "cbuf_block4");
        }

        static Framebuffer frameBuffer = null;

        static void Init(int size)
        {
            DrawBuffersEnum[] buffers = new DrawBuffersEnum[6]
            {
                DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1,
                DrawBuffersEnum.ColorAttachment2, DrawBuffersEnum.ColorAttachment3,
                DrawBuffersEnum.ColorAttachment4, DrawBuffersEnum.ColorAttachment5,
            };

            frameBuffer = new Framebuffer(FramebufferTarget.Framebuffer, size, size, PixelInternalFormat.R11fG11fB10f, 0, false);
            frameBuffer.SetDrawBuffers(buffers);
        }

        static void FilterProbeVolume(GLContext control, GLTexture2D normalsMap, GLTextureCube diffuseCubemap,
           Vector4[] shData, int lightmapTexID, int size, int mipLevel)
        {
            if (frameBuffer == null)
                Init(size);

            if (frameBuffer.Width != size)
                frameBuffer.Resize(size, size);

            frameBuffer.Bind();
            GL.Viewport(0, 0, size, size);

            //attach face to fbo as color attachment 
            for (int i = 0; i < 6; i++)
            {
                //Each fragment output is a cubemap face
                GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
                    FramebufferAttachment.ColorAttachment0 + i,
                     TextureTarget.TextureCubeMapPositiveX + i, lightmapTexID, mipLevel);
            }

            var shader = GlobalShaders.GetShader("PROBE");
            shader.Enable();

            var programID = shader.program;

            LoadUniforms(programID, shData);

            GL.ActiveTexture(TextureUnit.Texture0 + 1);
            normalsMap.Bind();
            shader.SetInt("sampler0", 1);

            GL.ActiveTexture(TextureUnit.Texture0 + 2);
            diffuseCubemap.Bind();
            shader.SetInt("sampler1", 2);

            //Draw once with 6 fragment outputs to form a cubemap 
            GL.ClearColor(0, 0, 0, 1);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            ScreenQuadRender.Draw();

            var errorcheck = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (errorcheck != FramebufferErrorCode.FramebufferComplete)
                throw new Exception(errorcheck.ToString());

            frameBuffer.Unbind();
            GL.BindTexture(TextureTarget.TextureCubeMap, 0);
            GL.UseProgram(0);
        }

        static UniformBlock GetBlock(string name)
        {
            if (!Blocks.ContainsKey(name))
                Blocks.Add(name, new UniformBlock());

            return Blocks[name];
        }
    }
}
