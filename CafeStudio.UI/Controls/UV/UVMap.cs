using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Drawing;
using GLFrameworkEngine;

namespace CafeStudio.UI
{
    public class UVMap 
    {
        #region Draw UVs

        static VertexBufferObject vao;

        public List<Vector2> Points = new List<Vector2>();

        static int Length;

        public static void Init()
        {
            if (Length == 0)
            {
                int buffer = GL.GenBuffer();
                vao = new VertexBufferObject(buffer);
                vao.AddAttribute(0, 2, VertexAttribPointerType.Float, false, 8, 0);
                vao.Initialize();

                Length = 1;
            }
        }

        public void Reset() {
            Points.Clear();
        }

        public void Draw(UVViewport.Camera2D camera, Vector2 scale)
        {
            GL.Disable(EnableCap.CullFace);

            var shader = GlobalShaders.GetShader("UV_WINDOW");
            shader.Enable();

            var cameraMtx = camera.ViewMatrix * camera.ProjectionMatrix;

            //shader.SetMatrix4x4("mtxMdl", ref scaleMtx);
            shader.SetMatrix4x4("mtxCam", ref cameraMtx);

            shader.SetInt("hasTexture", 0);
            shader.SetVector2("scale", scale);
            shader.SetVector4("uColor", ColorUtility.ToVector4(Runtime.UVEditor.UVColor));

            vao.Enable(shader);
            vao.Use();
            GL.DrawArrays(PrimitiveType.LineLoop, 0, Points.Count);

            GL.Enable(EnableCap.CullFace);
        }

        public void UpdateVertexBuffer(int PolygonGroupIndex, int UvChannelIndex, List<STGenericMesh> genericObjects, STGenericTextureMap textureMap)
        {
            Init();

            if (Points.Count > 0) return;

            if (genericObjects.Count == 0) return;

            foreach (var genericObject in genericObjects)
            {
                int divisions = 4;
                int lineWidth = 1;

                System.Drawing.Color uvColor = Runtime.UVEditor.UVColor;
                System.Drawing.Color gridColor = System.Drawing.Color.Black;

                List<uint> f = new List<uint>();
                int displayFaceSize = 0;
                if (genericObject.PolygonGroups.Count > 0)
                {
                    if (PolygonGroupIndex == -1)
                    {
                        foreach (var group in genericObject.PolygonGroups)
                        {
                            f.AddRange(group.Faces);
                            displayFaceSize += group.Faces.Count;
                        }
                    }
                    else
                    {
                        if (genericObject.PolygonGroups.Count > PolygonGroupIndex)
                        {
                            f = genericObject.PolygonGroups[PolygonGroupIndex].Faces;
                            displayFaceSize = genericObject.PolygonGroups[PolygonGroupIndex].Faces.Count;
                        }
                    }
                }

                if (genericObject.Vertices.Count == 0 ||
                    genericObject.Vertices[0].TexCoords.Length == 0)
                    return;

                for (int v = 0; v < displayFaceSize; v += 3)
                {
                    if (displayFaceSize < 3 || genericObject.Vertices.Count < 3)
                        return;

                    Vector2 v1 = new Vector2(0);
                    Vector2 v2 = new Vector2(0);
                    Vector2 v3 = new Vector2(0);

                    if (f.Count <= v + 2)
                        continue;

                    if (genericObject.Vertices.Count > f[v + 2])
                    {
                        v1 = genericObject.Vertices[(int)f[v]].TexCoords[UvChannelIndex];
                        v2 = genericObject.Vertices[(int)f[v + 1]].TexCoords[UvChannelIndex];
                        v3 = genericObject.Vertices[(int)f[v + 2]].TexCoords[UvChannelIndex];

                        v1 = new Vector2(v1.X, 1 - v1.Y);
                        v2 = new Vector2(v2.X, 1 - v2.Y);
                        v3 = new Vector2(v3.X, 1 - v3.Y);

                        DrawUVTriangleAndGrid(v1, v2, v3, divisions, uvColor, lineWidth, gridColor, textureMap);
                    }
                }
            }

            List<float> list = new List<float>();
            for (int i = 0; i < Points.Count; i++)
            {
                list.Add(Points[i].X);
                list.Add(Points[i].Y);
            }

            vao.Bind();

            float[] data = list.ToArray();
            GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * data.Length, data, BufferUsageHint.StaticDraw);
        }

        private void DrawUVTriangleAndGrid(Vector2 v1, Vector2 v2, Vector2 v3, int divisions,
            System.Drawing.Color uvColor, int lineWidth, System.Drawing.Color gridColor, STGenericTextureMap textureMap)
        {
            GL.UseProgram(0);

            Vector2 scaleUv = new Vector2(2);
            Vector2 transUv = new Vector2(-1f);

            if (textureMap != null && textureMap.Transform != null)
            {
                scaleUv *= textureMap.Transform.Scale;
                transUv += textureMap.Transform.Translate;
            }

            Points.AddRange(DrawUvTriangle(v1, v2, v3, uvColor, scaleUv, transUv));
        }

        private static List<Vector2> DrawUvTriangle(Vector2 v1, Vector2 v2, Vector2 v3, System.Drawing.Color uvColor, Vector2 scaleUv, Vector2 transUv)
        {
            List<Vector2> points = new List<Vector2>();
            points.Add(v1 * scaleUv + transUv);
            points.Add(v2 * scaleUv + transUv);

            points.Add(v2 * scaleUv + transUv);
            points.Add(v3 * scaleUv + transUv);

            points.Add(v3 * scaleUv + transUv);
            points.Add(v1 * scaleUv + transUv);
            return points;
        }

        #endregion

    }
}
