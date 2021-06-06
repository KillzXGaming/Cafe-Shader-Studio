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
        private readonly int? instancedBuffer;
        private readonly Dictionary<object, VertexAttribute> attributes;
        private readonly Dictionary<object, VertexAttribute> attributesInstanced;

        private bool _disposed;

        public VertexBufferObject(int buffer, int? indexBuffer = null, int? instancedBuffer = null)
        {
            ID = -1;
            this.buffer = buffer;
            this.indexBuffer = indexBuffer;
            this.instancedBuffer = instancedBuffer;
            this._disposed = false;
            attributes = new Dictionary<object, VertexAttribute>();
            attributesInstanced = new Dictionary<object, VertexAttribute>();
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

        public void AddInstancedAttribute(string name, int size, VertexAttribPointerType type, bool normalized, int stride, int offset)
        {
            attributesInstanced.Add(name, new VertexAttribute(size, type, normalized, stride, offset, true));
        }

        public void Initialize()
        {
            if (_disposed || ID != -1)
                return;

            GL.GenVertexArrays(1, out int vao);
            Bind();

            ID = vao;
            if (GLErrorHandler.CheckGLError()) Debugger.Break();
        }

        public void Enable(ShaderProgram shader)
        {
            if (_disposed) return;

            GL.BindVertexArray(ID);
            EnableAttributes(shader, attributes, buffer);

            if (instancedBuffer != null)
            {
                EnableAttributes(shader, attributesInstanced, instancedBuffer.Value);
            }
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
        }

        private void EnableAttributes(ShaderProgram shader, Dictionary<object, VertexAttribute> attributes, int bufferID)
        {
            foreach (KeyValuePair<object, VertexAttribute> a in attributes)
            {
                int location = -1;
                if (a.Key is string)
                    location = shader.GetAttribute((string)a.Key);
                else
                    location = (int)a.Key;

                GL.EnableVertexAttribArray(location);
                GL.BindBuffer(BufferTarget.ArrayBuffer, bufferID);

                if (a.Value.type == VertexAttribPointerType.Int)
                    GL.VertexAttribIPointer(location, a.Value.size, VertexAttribIntegerType.Int, a.Value.stride, new System.IntPtr(a.Value.offset));
                else
                    GL.VertexAttribPointer(location, a.Value.size, a.Value.type, a.Value.normalized, a.Value.stride, a.Value.offset);

                if (a.Value.instance)
                    GL.VertexAttribDivisor(location, a.Value.divisor);
            }
        }

        private void DisableAttributes(ShaderProgram shader, Dictionary<object, VertexAttribute> attributes)
        {
            foreach (KeyValuePair<object, VertexAttribute> a in attributes)
            {
                int location = -1;
                if (a.Key is string)
                    location = shader.GetAttribute((string)a.Key);
                else
                    location = (int)a.Key;

                GL.DisableVertexAttribArray(location);
            }
        }

        public void Disable(ShaderProgram shader)
        {
            if (_disposed) return;

            GL.BindBuffer(BufferTarget.ArrayBuffer, buffer);
            DisableAttributes(shader, attributes);
            if (instancedBuffer != null)
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, instancedBuffer.Value);
                DisableAttributes(shader, attributesInstanced);
            }
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        public void BindVertexArray()
        {
            GL.BindVertexArray(ID);
        }

        public void Bind()
        {
            if (_disposed) return;

            GL.BindVertexArray(ID);
            GL.BindBuffer(BufferTarget.ArrayBuffer, buffer);
        }

        public void Use()
        {
            if (_disposed) return;

            GL.BindVertexArray(ID);
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
            if (instancedBuffer.HasValue)
                GL.DeleteBuffer(instancedBuffer.Value);

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
            public bool instance;
            public int divisor;

            public VertexAttribute(int size, VertexAttribPointerType type, bool normalized, int stride, int offset, bool instance = false, int divisor = 1)
            {
                this.size = size;
                this.type = type;
                this.normalized = normalized;
                this.stride = stride;
                this.offset = offset;
                this.instance = instance;
                this.divisor = divisor;
            }
        }
    }
}
