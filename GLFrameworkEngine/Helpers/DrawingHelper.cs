using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;

namespace GLFrameworkEngine
{
    public class DrawingHelper
    {
        public static GLVertex[] GetSphereVertices(float radius, float subdiv)
        {
            List<GLVertex> vertices = new List<GLVertex>();

            float halfPi = (float)(Math.PI * 0.5);
            float oneThroughPrecision = 1.0f / subdiv;
            float twoPiThroughPrecision = (float)(Math.PI * 2.0 * oneThroughPrecision);

            float theta1, theta2, theta3;
            Vector3 norm = new Vector3(), pos = new Vector3();

            for (uint j = 0; j < subdiv / 2; j++)
            {
                theta1 = (j * twoPiThroughPrecision) - halfPi;
                theta2 = ((j + 1) * twoPiThroughPrecision) - halfPi;

                for (uint i = 0; i <= subdiv; i++)
                {
                    theta3 = i * twoPiThroughPrecision;

                    norm.X = (float)(Math.Cos(theta1) * Math.Cos(theta3));
                    norm.Y = (float)Math.Sin(theta1);
                    norm.Z = (float)(Math.Cos(theta1) * Math.Sin(theta3));
                    pos.X = radius * norm.X;
                    pos.Y = radius * norm.Y;
                    pos.Z = radius * norm.Z;

                    vertices.Add(new GLVertex() { Position = pos, Normal = norm });

                    norm.X = (float)(Math.Cos(theta2) * Math.Cos(theta3));
                    norm.Y = (float)Math.Sin(theta2);
                    norm.Z = (float)(Math.Cos(theta2) * Math.Sin(theta3));
                    pos.X = radius * norm.X;
                    pos.Y = radius * norm.Y;
                    pos.Z = radius * norm.Z;

                    vertices.Add(new GLVertex() { Position = pos, Normal = norm });
                }
            }

            return vertices.ToArray();
        }

        public struct GLVertex
        {
            public Vector3 Position { get; set; }
            public Vector3 Normal { get; set; }
        }
    }
}
