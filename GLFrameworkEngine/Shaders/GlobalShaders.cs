using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using OpenTK.Graphics.OpenGL;
using Toolbox.Core;

namespace GLFrameworkEngine
{
    /// <summary>
    /// Represents a global shader cache to store in tool shaders.
    /// The shader paths are initiated and shaders are only loaded when used.
    /// </summary>
    public class GlobalShaders
    {
        static Dictionary<string, ShaderProgram> Shaders = new Dictionary<string, ShaderProgram>();
        static Dictionary<string, string> ShaderPaths = new Dictionary<string, string>();

        //Paths for all the shaders relative to the shader folder
        static void InitPaths()
        {
            ShaderPaths.Add("BASIC", "Generic/Basic");
            ShaderPaths.Add("LINE", "Generic/Line");
            ShaderPaths.Add("NORMALS", "Normals/Normals");
            ShaderPaths.Add("BILLBOARD_TEXTURE", "Generic/BillboardTexture");
            ShaderPaths.Add("SCREEN", "Screen/Screen");
            ShaderPaths.Add("GRID", "Viewer/Grid");
            ShaderPaths.Add("SHADOW", "Shadow");
            ShaderPaths.Add("COLOR_CORRECTION", "ColorCorrection");
            ShaderPaths.Add("NORMALIZE_DEPTH", "NormalizeDepth");
            ShaderPaths.Add("PICKING", "Picking");
            ShaderPaths.Add("SELECTION", "Selection");
            ShaderPaths.Add("GIZMO", "Editor/Gizmo");
            ShaderPaths.Add("UV_WINDOW", "Editor/UVWindow");
            ShaderPaths.Add("IMAGE_EDITOR", "Editor/ImageEditor");
            ShaderPaths.Add("DEBUG", "BFRES/BfresDebug");
            ShaderPaths.Add("TEXTURE_ICON", "TextureIcon");
            ShaderPaths.Add("EQUIRECTANGULAR", "Cubemap/Equirectangular");
            ShaderPaths.Add("LIGHTMAP", "Lightmap");
            ShaderPaths.Add("CUBEMAP_HDRENCODE", "Cubemap/HdrEncode");
            ShaderPaths.Add("CUBEMAP_HDRDECODE", "Cubemap/HdrDecode");
            ShaderPaths.Add("CUBEMAP_IRRADIANCE", "Cubemap/Irradiance");
            ShaderPaths.Add("CUBEMAP_PREFILTER", "Cubemap/Prefilter");
            ShaderPaths.Add("EFFECTS_DEFAULT", "Effects/Default");
            ShaderPaths.Add("BLUR", "Blur/GaussianBlur");
            ShaderPaths.Add("BLOOM_EXTRACT", "BloomExtract");
            ShaderPaths.Add("LIGHTPREPASS", "LightPrepass");
            ShaderPaths.Add("LPP_CAUSTICS", "Caustics");
        }

        /// <summary>
        /// Gets a shader given a key to store them in and a shader file path.
        /// </summary>
        public static ShaderProgram GetShader(string key, string path)
        {
            if (!Shaders.ContainsKey(key))
            {
                Shaders.Add(key, LoadShader(path));
                Shaders[key].Link();
            }
            return Shaders[key];
        }

        /// <summary>
        /// Gets a shader given a key they are stored in.
        /// </summary>
        public static ShaderProgram GetShader(string key)
        {
            //Load the shader if not initiated yet.
            if (!Shaders.ContainsKey(key))
                InitShader(key);

            //Return the shader from the global list if present
            if (Shaders.ContainsKey(key))
                return Shaders[key];

            return null;
        }

        //Setup the shader for the first time
        static void InitShader(string key)
        {
            if (ShaderPaths.Count == 0)
                InitPaths();

            if (ShaderPaths.ContainsKey(key))
                Shaders.Add(key, LoadShader(ShaderPaths[key]));
        }

        //Load the shader from a path
        static ShaderProgram LoadShader(string name)
        {
            List<Shader> shaders = new List<Shader>();

            string shaderFolder = $"{Runtime.ExecutableDir}{Path.DirectorySeparatorChar}Shaders{Path.DirectorySeparatorChar}";
            string frag = $"{shaderFolder}{name}.frag";
            string vert = $"{shaderFolder}{name}.vert";
            string geom = $"{shaderFolder}{name}.geom";
            if (File.Exists(vert)) shaders.Add(new VertexShader(File.ReadAllText(vert)));
            if (File.Exists(frag)) shaders.Add(new FragmentShader(File.ReadAllText(frag)));
            if (File.Exists(geom)) shaders.Add(new GeomertyShader(File.ReadAllText(geom)));

           // if (shaders.Count == 0)
           //     throw new Exception($"Failed to find shaders at {name}");

            return new ShaderProgram(shaders.ToArray());
        }
    }
}
