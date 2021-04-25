using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Diagnostics;

namespace GLFrameworkEngine
{
    public struct VertexBufferObject : IDisposable
    {
        private int ID;
        private readonly int buffer;
        private readonly int? indexBuffer;
        private readonly Dictionary<object, VertexAttribute> attributes;

        private bool _disposed;

        public VertexBufferObject(int buffer, int? indexBuffer = null)
        {
            ID = -1;
            this.buffer = buffer;
            this.indexBuffer = indexBuffer;
            this._disposed = false;
            attributes = new Dictionary<object, VertexAttribute>();
        }

        public void Clear()
        {
            attributes.Clear();
        }

        public void AddAttribute(int location, int size, VertexAttribPointerType type, bool normalized, int stride, int offset)
        {
            attributes.Add(location, new VertexAttribute(size, type, normalized, stride, offset));
        }

        public void AddAttribute(string name, int size, VertexAttribPointerType type, bool normalized, int stride, int offset)
        {
            attributes.Add(name, new VertexAttribute(size, type, normalized, stride, offset));
        }

        public void Initialize()
        {
            if (_disposed || ID != -1)
                return;

            GL.GenVertexArrays(1, out int vao);
            GL.BindVertexArray(vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, buffer);
            ID = vao;

            if (GLErrorHandler.CheckGLError()) Debugger.Break();
        }

        public void Enable(ShaderProgram shader)
        {
            if (_disposed) return;

            GL.BindVertexArray(ID);
            GL.BindBuffer(BufferTarget.ArrayBuffer, buffer);
            foreach (KeyValuePair<object, VertexAttribute> a in attributes)
            {
                int location = -1;
                if (a.Key is string)
                    location = shader.GetAttribute((string)a.Key);
                else
                    location = (int)a.Key;

                GL.EnableVertexAttribArray(location);
                if (a.Value.type == VertexAttribPointerType.Int)
                    GL.VertexAttribIPointer(location, a.Value.size, VertexAttribIntegerType.Int, a.Value.stride, new System.IntPtr(a.Value.offset));
                else
                    GL.VertexAttribPointer(location, a.Value.size, a.Value.type, a.Value.normalized, a.Value.stride, a.Value.offset);
            }
        }

        public void Disable(ShaderProgram shader)
        {
            if (_disposed) return;

            GL.BindBuffer(BufferTarget.ArrayBuffer, buffer);
            foreach (KeyValuePair<object, VertexAttribute> a in attributes)
            {
                int location = -1;
                if (a.Key is string)
                    location = shader.GetAttribute((string)a.Key);
                else
                    location = (int)a.Key;

                GL.DisableVertexAttribArray(location);
            }
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        public void Bind()
        {
            if (_disposed) return;

            GL.BindBuffer(BufferTarget.ArrayBuffer, buffer);
        }

        public void Use()
        {
            if (_disposed) return;

            if (indexBuffer.HasValue)
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBuffer.Value);
            else
                GL.BindBuffer(BufferTarget.ArrayBuffer, buffer);
        }

        public void Dispose()
        {
            GL.DeleteVertexArray(ID);
            GL.DeleteBuffer(buffer);
            if (indexBuffer.HasValue)
                GL.DeleteBuffer(indexBuffer.Value);

            _disposed = true;
            ID = -1;
            attributes.Clear();
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
