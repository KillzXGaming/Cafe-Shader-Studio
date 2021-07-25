using System;
using System.Collections.Generic;
using System.IO;
using GLFrameworkEngine;
using Toolbox.Core.IO;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace AGraphicsLibrary
{
    public class ProbeDebugVoxelDrawer
    {
        static int vertexBuffer;

        static VertexArrayObject vao;

        static Vertex[] Vertices = new Vertex[0];

        static void Init(int volumeIndex)
        {
            GL.GenBuffers(1, out vertexBuffer);

            vao = new VertexArrayObject(vertexBuffer);
            vao.AddAttribute(0, 3, VertexAttribPointerType.Float, false, Vertex.SIZE, 0);
            //7 coef
            vao.AddAttribute(1, 4, VertexAttribPointerType.Float, false, Vertex.SIZE, 12);
            vao.AddAttribute(2, 4, VertexAttribPointerType.Float, false, Vertex.SIZE, 28);
            vao.AddAttribute(3, 4, VertexAttribPointerType.Float, false, Vertex.SIZE, 44);
            vao.AddAttribute(4, 4, VertexAttribPointerType.Float, false, Vertex.SIZE, 60);
            vao.AddAttribute(5, 4, VertexAttribPointerType.Float, false, Vertex.SIZE, 76);
            vao.AddAttribute(6, 4, VertexAttribPointerType.Float, false, Vertex.SIZE, 92);
            vao.AddAttribute(7, 4, VertexAttribPointerType.Float, false, Vertex.SIZE, 108);

            vao.Initialize();

            var probeLighting = ProbeMapManager.ProbeLighting;
            var volume = probeLighting.Boxes[volumeIndex];
            Vertices = InitProbes(volume).ToArray();

            vao.Bind();
            GL.BufferData<Vertex>(BufferTarget.ArrayBuffer, Vertex.SIZE * Vertices.Length, Vertices, BufferUsageHint.StaticDraw);
        }

        public static void Draw(GLContext context)
        {
            if (ProbeMapManager.ProbeLighting == null)
                return;

            if (Vertices.Length == 0)
                Init(0);

            var shader = GlobalShaders.GetShader("PROBE_VOXEL");
            context.CurrentShader = shader;

            vao.Use();

            GL.PointSize(5);
            GL.DrawArrays(PrimitiveType.Points, 0, Vertices.Length);
            GL.PointSize(1);
        }

        static List<Vertex> InitProbes(ProbeVolume volume)
        {
            List<Vertex> probes = new List<Vertex>();

            var probeCount = volume.Grid.GetProbeCount();
            for (uint x = 0; x < probeCount.X; x++) {
                for (uint y = 0; y < probeCount.Y; y++) {
                    for (uint z = 0; z < probeCount.Z; z++) {
                        int voxelIndex = volume.Grid.GetVoxelIndex(x, y, z);
                        Vector3 positon = volume.Grid.GetVoxelPosition(x, y, z);

                        for (int j = 0; j < 1; j++) {
                            uint dataIndex = volume.IndexBuffer.GetSHDataIndex(voxelIndex, j);
                            if (!volume.IndexBuffer.IsIndexValueValid(dataIndex))
                                continue;

                            float[] data = volume.DataBuffer.GetSHData((int)dataIndex);

                            var probe = new Vertex();
                            probe.Position = positon;
                            probe.Coef0 = new Vector4(data[0], data[1], data[2], data[3]);
                            probe.Coef1 = new Vector4(data[4], data[5], data[6], data[7]);
                            probe.Coef2 = new Vector4(data[8], data[9], data[10], data[11]);
                            probe.Coef3 = new Vector4(data[12], data[13], data[14], data[15]);
                            probe.Coef4 = new Vector4(data[16], data[17], data[18], data[19]);
                            probe.Coef5 = new Vector4(data[20], data[21], data[22], data[23]);
                            probe.Coef6 = new Vector4(data[24], data[25], data[26], 0);
                            probes.Add(probe);
                        }
                    }
                }
            }
            return probes;
        }

        struct Vertex
        {
            public Vector3 Position;
            public Vector4 Coef0;
            public Vector4 Coef1;
            public Vector4 Coef2;
            public Vector4 Coef3;
            public Vector4 Coef4;
            public Vector4 Coef5;
            public Vector4 Coef6;

            public static int SIZE = 4 * (3 + (4 * 7));
        }
    }
}
