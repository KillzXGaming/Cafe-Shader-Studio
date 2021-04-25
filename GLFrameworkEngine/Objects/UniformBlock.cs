using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace GLFrameworkEngine
{
    public class UniformBlock
    {
        public List<byte> Buffer = new List<byte>();

        public int Size => Buffer.Count * sizeof(byte);

        private int ID;

        public UniformBlock()
        {
            GL.GenBuffers(1, out ID);
        }

        public void Add(byte[] value)
        {
            Buffer.AddRange(value);
        }

        public void Add(uint[] value)
        {
            for (int i = 0; i < value.Length; i++)
                Buffer.AddRange(BitConverter.GetBytes(value[i]));
        }

        public void Add(int[] value)
        {
            for (int i = 0; i < value.Length; i++)
                Buffer.AddRange(BitConverter.GetBytes(value[i]));
        }

        public void Add(float[] value)
        {
            for (int i = 0; i < value.Length; i++)
                AddFloat(value[i]);
        }

        public void Add(Vector2[] value)
        {
            for (int i = 0; i < value.Length; i++)
                Add(value[i]);
        }

        public void Add(Vector3[] value)
        {
            for (int i = 0; i < value.Length; i++)
                Add(value[i]);
        }

        public void Add(Vector4[] value)
        {
            for (int i = 0; i < value.Length; i++)
                Add(value[i]);
        }

        public void Add(float value)
        {
            Buffer.AddRange(BitConverter.GetBytes(value));
        }

        public void AddFloat(float value)
        {
            Buffer.AddRange(BitConverter.GetBytes(value));
            Buffer.AddRange(new byte[12]); //Padding
        }

        public void AddInt(int value)
        {
            Buffer.AddRange(BitConverter.GetBytes(value));
            Buffer.AddRange(new byte[12]); //Padding
        }

        public void Add(Vector2 value)
        {
            Add(value.X);
            Add(value.Y);
        }

        public void Add(Vector3 value)
        {
            Add(value.X);
            Add(value.Y);
            Add(value.Z);
            Buffer.AddRange(new byte[4]); //Buffer aligned so make sure it's 16 bytes size
        }

        public void Add(Vector4 value)
        {
            Add(value.X);
            Add(value.Y);
            Add(value.Z);
            Add(value.W);
        }

        public void Bind()
        {
            GL.BindBuffer(BufferTarget.UniformBuffer, ID);
        }

        public void RenderBuffer(int programID, string name)
        {
            var index = GL.GetUniformBlockIndex(programID, name);
            if (index == -1)
                return;

            Bind();

            int bindingPoint = index;
            GL.UniformBlockBinding(programID, index, bindingPoint);
            GL.BindBufferBase(BufferRangeTarget.UniformBuffer, index, ID);

            UpdateBufferData();
        }

        public void UpdateBufferData()
        {
            //Bind the data
            var buffer = Buffer.ToArray();

            Bind();
            GL.BufferData(BufferTarget.UniformBuffer, buffer.Length, buffer, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.UniformBuffer, 0);
        }

        public void Dispose()
        {
            GL.DeleteBuffer(ID);
            Buffer.Clear();
        }

        int activeAttributeBlockCount;

        static Dictionary<string, int> vertexBlocks = new Dictionary<string, int>();
        static Dictionary<string, int> fragmentBlocks = new Dictionary<string, int>();

        private void LoadUniorms(int program)
        {
            vertexBlocks.Clear();
            fragmentBlocks.Clear();

            GL.GetProgram(program, GetProgramParameterName.ActiveUniformBlocks, out activeAttributeBlockCount);
            for (int i = 0; i < activeAttributeBlockCount; i++)
            {
                string name = GL.GetActiveUniformBlockName(program, i);
                int index = GL.GetUniformBlockIndex(program, name);

                // Overwrite existing blocks.
                if (name.Contains("fp_"))
                    fragmentBlocks[name] = index;
                if (name.Contains("vp_"))
                    vertexBlocks[name] = index;

                Console.WriteLine($"SHADERBLOCK {name} {index}");
            }
        }
    }
}
