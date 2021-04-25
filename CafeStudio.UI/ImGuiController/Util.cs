using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;

namespace CafeStudio.UI
{
    static class Util
    {
        [Pure]
        public static float Clamp(float value, float min, float max)
        {
            return value < min ? min : value > max ? max : value;
        }

        [Conditional("DEBUG")]
        public static void CheckGLError(string title)
        {
            return;

            var error = GL.GetError();
            if (error != ErrorCode.NoError)
            {
                Debug.Print($"{title}: {error}");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LabelObject(ObjectLabelIdentifier objLabelIdent, int glObject, string name)
        {
            // OpenGL 3.3 doesn't have glObjectLabel as that is a 4.3 feature. So we do nothing here...
            // GL.ObjectLabel(objLabelIdent, glObject, name.Length, name);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CreateTexture(TextureTarget target, string Name, out int Texture)
        {
            Texture = GL.GenTexture();
            GL.BindTexture(target, Texture);
            GL.BindTexture(target, 0);
            LabelObject(ObjectLabelIdentifier.Texture, Texture, $"Texture: {Name}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CreateProgram(string Name, out int Program)
        {
            Program = GL.CreateProgram();
            LabelObject(ObjectLabelIdentifier.Program, Program, $"Program: {Name}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CreateShader(ShaderType type, string Name, out int Shader)
        {
            Shader = GL.CreateShader(type);
            LabelObject(ObjectLabelIdentifier.Shader, Shader, $"Shader: {type}: {Name}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CreateBuffer(string Name, out int Buffer)
        {
            Buffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, Buffer);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            LabelObject(ObjectLabelIdentifier.Buffer, Buffer, $"Buffer: {Name}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CreateVertexBuffer(string Name, out int Buffer) => CreateBuffer($"VBO: {Name}", out Buffer);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CreateElementBuffer(string Name, out int Buffer) => CreateBuffer($"EBO: {Name}", out Buffer);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CreateVertexArray(string Name, out int VAO)
        {
            VAO = GL.GenVertexArray();
            GL.BindVertexArray(VAO);
            GL.BindVertexArray(0);
            LabelObject(ObjectLabelIdentifier.VertexArray, VAO, $"VAO: {Name}");
        }
    }
}
