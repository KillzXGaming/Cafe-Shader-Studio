using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using OpenTK;

namespace GLFrameworkEngine
{
    public class ShaderProgram : IDisposable
    {
        public int program;

        private Dictionary<string, int> attributes = new Dictionary<string, int>();
        private Dictionary<string, int> uniforms = new Dictionary<string, int>();
        private int activeAttributeCount;
        private HashSet<Shader> shaders = new HashSet<Shader>();

        // This isn't in OpenTK's enums for some reason.
        // https://www.khronos.org/registry/OpenGL/api/GL/glcorearb.h
        private static readonly int GL_PROGRAM_BINARY_MAX_LENGTH = 0x8741;

        public ShaderProgram(Shader[] shaders) {
            foreach (Shader shader in shaders)
            {
                if (!this.shaders.Contains(shader))
                    this.shaders.Add(shader);
            }
            program = CompileShaders();
        }

        public ShaderProgram(Shader vertexShader, Shader fragmentShader) {
            if (!this.shaders.Contains(vertexShader))
                this.shaders.Add(vertexShader);
            if (!this.shaders.Contains(fragmentShader))
                this.shaders.Add(fragmentShader);
            program = CompileShaders();
        }

        public ShaderProgram(byte[] binaryData, BinaryFormat format)
        {
            GL.ProgramBinary(program, format, binaryData, binaryData.Length);
        }

        public void Link()
        {
            GL.LinkProgram(program);
        }

        public void Enable() {
            GL.UseProgram(program);
        }

        public void Disable() {
            GL.UseProgram(0);
        }

        public void Dispose() {
            foreach (var shader in shaders)
                shader.Dispose();

            GL.DeleteProgram(program);
        }

        public void SetTexture(GLTexture tex, string uniform, int id)
        {
            GL.ActiveTexture(TextureUnit.Texture0 + id);
            tex.Bind();
            this.SetInt(uniform, id);
        }

        public void SetVector4(string name, Vector4 value)
        {
            if (uniforms.ContainsKey(name))
                GL.Uniform4(uniforms[name], value);
        }

        public void SetVector3(string name, Vector3 value)
        {
            if (uniforms.ContainsKey(name))
                GL.Uniform3(uniforms[name], value);
        }

        public void SetVector2(string name, Vector2 value)
        {
            if (uniforms.ContainsKey(name))
                GL.Uniform2(uniforms[name], value);
        }

        public void SetFloat(string name, float value)
        {
            if (uniforms.ContainsKey(name))
                GL.Uniform1(uniforms[name], value);
        }

        public void SetInt(string name, int value)
        {
            if (uniforms.ContainsKey(name))
                GL.Uniform1(uniforms[name], value);
        }

        public void SetBool(string name, bool value)
        {
            int intValue = value == true ? 1 : 0;

            if (uniforms.ContainsKey(name))
                GL.Uniform1(uniforms[name], intValue);
        }

        public void SetBoolToInt(string name, bool value)
        {
            if (!uniforms.ContainsKey(name))
                return;

            if (value)
                GL.Uniform1(uniforms[name], 1);
            else
                GL.Uniform1(this[name], 0);
        }

        public void SetColor(string name, System.Drawing.Color color)
        {
            if (uniforms.ContainsKey(name))
                GL.Uniform4(uniforms[name], color.R, color.G, color.B, color.A);
        }

        public void SetMatrix4x4(string name, ref Matrix4 value, bool transpose = false)
        {
            if (uniforms.ContainsKey(name))
                GL.UniformMatrix4(uniforms[name], transpose, ref value);
        }

        public int this[string name]
        {
            get { return uniforms[name]; }
        }

        private void LoadUniorms(int program)
        {
            uniforms.Clear();

            GL.GetProgram(program, GetProgramParameterName.ActiveUniforms, out activeAttributeCount);
            for (int i = 0; i < activeAttributeCount; i++)
            {
                string name = GL.GetActiveUniform(program, i, out int size, out ActiveUniformType type);
                int location = GL.GetUniformLocation(program, name);

                // Overwrite existing vertex attributes.
                uniforms[name] = location;
            }
        }

        private void LoadAttributes(int program)
        {
            attributes.Clear();

            GL.GetProgram(program, GetProgramParameterName.ActiveAttributes, out activeAttributeCount);
            for (int i = 0; i < activeAttributeCount; i++)
            {
                string name = GL.GetActiveAttrib(program, i, out int size, out ActiveAttribType type);
                int location = GL.GetAttribLocation(program, name);

                // Overwrite existing vertex attributes.
                attributes[name] = location;
            }
        }

        public int GetAttribute(string name)
        {
            if (string.IsNullOrEmpty(name) || !attributes.ContainsKey(name))
                return -1;
            else
                return attributes[name];
        }


        public void EnableVertexAttributes()
        {
            foreach (KeyValuePair<string, int> attrib in attributes)
                GL.EnableVertexAttribArray(attrib.Value);
        }

        public void DisableVertexAttributes()
        {
            foreach (KeyValuePair<string, int> attrib in attributes)
                GL.DisableVertexAttribArray(attrib.Value);
        }

        public void Compile()
        {
            program = CompileShaders();

            LoadAttributes(program);
            LoadUniorms(program);
            OnCompiled();
        }

        public void SaveBinary(string fileName)
        {
            CreateBinary(out byte[] binaryData, out BinaryFormat format);
            System.IO.File.WriteAllBytes(fileName, binaryData);
        }

        private void CreateBinary(out byte[] binaryData, out BinaryFormat format)
        {
            GL.GetProgram(program, (GetProgramParameterName)GL_PROGRAM_BINARY_MAX_LENGTH, out int size);
            binaryData = new byte[size];
            GL.GetProgramBinary(program, size, out _, out format, binaryData);
        }

        public virtual void OnCompiled() { }

        private int CompileShaders()
        {
            int program = GL.CreateProgram();
            foreach (Shader shader in shaders) {
                GL.AttachShader(program, shader.id);
            }
            GL.LinkProgram(program);
            foreach (var shader in shaders)
            {
                Console.WriteLine($"{shader.type.ToString("g")}:");

                string log = GL.GetShaderInfoLog(shader.id);
                Console.WriteLine(log);
            }
            LoadAttributes(program);
            LoadUniorms(program);
            return program;
        }
    }

    public class Shader : IDisposable
    {
        public Shader(string src, ShaderType type)
        {
            id = GL.CreateShader(type);
            GL.ShaderSource(id, src);
            GL.CompileShader(id);
            this.type = type;
        }

        public string GetShaderSource()
        {
            string source = "";

            GL.GetShader(id, ShaderParameter.ShaderSourceLength, out int length);
            if (length != 0)
                GL.GetShaderSource(id, length, out _, out source);
            return source;
        }

        public string GetInfoLog() {
            return GL.GetShaderInfoLog(id);
        }

        public void Dispose() {
            GL.DeleteShader(id);
        }

        public ShaderType type;

        public int id;
    }

    public class FragmentShader : Shader
    {
        public FragmentShader(string src)
            : base(src, ShaderType.FragmentShader)
        {

        }
    }

    public class VertexShader : Shader
    {
        public VertexShader(string src)
            : base(src, ShaderType.VertexShader)
        {

        }
    }

    public class GeomertyShader : Shader
    {
        public GeomertyShader(string src)
            : base(src, ShaderType.GeometryShader)
        {

        }
    }
}
