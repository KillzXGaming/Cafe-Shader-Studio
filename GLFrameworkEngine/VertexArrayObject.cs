using System.Collections.Generic;
using System.Diagnostics;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace GLFrameworkEngine
{
    public class VertexArrayObject
    {
        public int vaoID;
        public readonly int buffer;
        public readonly int? indexBuffer;
        private readonly Dictionary<int, VertexAttribute> attributes;

        /// <summary>
        /// Creates an object to which you can add Attributes. When you are done call Submit()!
        /// </summary>
        /// <param name="buffer">The opengl buffer where all the vertexdata is/will be stored</param>
        /// <param name="indexBuffer">The opengl buffer where all the indices are/will be stored</param>
        public VertexArrayObject(int buffer, int? indexBuffer = null)
        {
            vaoID = -1;
            this.buffer = buffer;
            this.indexBuffer = indexBuffer;
            attributes = new Dictionary<int, VertexAttribute>();
        }

        /// <summary>
        /// Adds an attribute to the current vertex layout.
        /// </summary>
        public void AddAttribute(int index, int size, VertexAttribPointerType type, bool normalized, int stride, int offset)
        {
            attributes[index] = new VertexAttribute(size, type, normalized, stride, offset);
        }

        /// <summary>
        /// Deletes the vertex array object.
        /// </summary>
        public void Delete()
        {
            GL.DeleteVertexArray(vaoID);
        }

        /// <summary>
        /// Inits the vertex array object with a new ID.
        /// </summary>
        public void Initialize()
        {
            if (vaoID != -1)
                return;

            GL.GenVertexArrays(1, out int vao);
            GL.BindVertexArray(vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, buffer);

            vaoID = vao;

            foreach (KeyValuePair<int, VertexAttribute> a in attributes)
            {
                GL.EnableVertexAttribArray(a.Key);
                GL.VertexAttribPointer(a.Key, a.Value.size, a.Value.type, a.Value.normalized, a.Value.stride, a.Value.offset);
            }

            if (GLErrorHandler.CheckGLError()) Debugger.Break();
        }

        /// <summary>
        /// Binds the vertex data buffer
        /// </summary>
        public void Bind()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, buffer);
        }

        /// <summary>
        /// Binds this VertexArrayObject and the associated IndexBuffer if there is one
        /// </summary>
        /// <param name="control"></param>
        public void Use()
        {
            GL.BindVertexArray(vaoID);

            if (indexBuffer.HasValue)
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBuffer.Value);

            if (GLErrorHandler.CheckGLError()) Debugger.Break();
        }

        private struct VertexAttribute
        {
            public int size;
            public VertexAttribPointerType type;
            public bool normalized;
            public int stride;
            public int offset;
            public VertexAttribute(int size, VertexAttribPointerType type, bool normalized, int stride, int offset)
            {
                this.size = size;
                this.type = type;
                this.normalized = normalized;
                this.stride = stride;
                this.offset = offset;
            }
        }
    }
}