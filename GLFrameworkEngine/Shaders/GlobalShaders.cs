using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using OpenTK.Graphics.OpenGL;

namespace GLFrameworkEngine
{
    public class GlobalShaders
    {
        static Dictionary<string, ShaderProgram> Shaders = new Dictionary<string, ShaderProgram>();

        public GlobalShaders() { }

        public static void Init() {
            Shaders.Add("BASIC", LoadShader("Generic/Basic"));
            Shaders.Add("NORMALS", LoadShader("Normals/Normals"));
            Shaders.Add("BILLBOARD_TEXTURE", LoadShader("Generic/BillboardTexture"));
            Shaders.Add("SCREEN", LoadShader("Screen/Screen"));
            Shaders.Add("GRID", LoadShader("Viewer/Grid"));
            Shaders.Add("SHADOW", LoadShader("Shadow"));
            Shaders.Add("COLOR_CORRECTION", LoadShader("ColorCorrection"));
            Shaders.Add("NORMALIZE_DEPTH", LoadShader("NormalizeDepth"));
            Shaders.Add("PICKING", LoadShader("Picking"));
            Shaders.Add("SELECTION", LoadShader("Selection"));
            Shaders.Add("GIZMO", LoadShader("Editor/Gizmo"));
            Shaders.Add("UV_WINDOW", LoadShader("Editor/UVWindow"));
            Shaders.Add("IMAGE_EDITOR", LoadShader("Editor/ImageEditor"));
            Shaders.Add("DEBUG", LoadShader("BFRES/BfresDebug"));
            Shaders.Add("TEXTURE_ICON", LoadShader("TextureIcon"));
            Shaders.Add("EQUIRECTANGULAR", LoadShader("Cubemap/Equirectangular"));
            Shaders.Add("LIGHTMAP", LoadShader("Lightmap"));
            Shaders.Add("CUBEMAP_HDRENCODE", LoadShader("Cubemap/HdrEncode"));
            Shaders.Add("CUBEMAP_HDRDECODE", LoadShader("Cubemap/HdrDecode"));
            Shaders.Add("CUBEMAP_IRRADIANCE", LoadShader("Cubemap/Irradiance"));
            Shaders.Add("CUBEMAP_PREFILTER", LoadShader("Cubemap/Prefilter"));

            if (GLErrorHandler.CheckGLError())
                System.Diagnostics.Debugger.Break();

            return;

            Shaders.Add("BLOOM_EXTRACT", LoadShader("BloomExtract"));
            Shaders.Add("BLOOM_EXTRACT_AGL", LoadShader("BloomExtractAGL"));
            Shaders.Add("LIGHTPREPASS", LoadShader("LightPrepass"));
            Shaders.Add("PROBE", LoadShader("ProbeCubemap"));
            Shaders.Add("BFRES_LOW", LoadShader("BFRES/BfresLow"));
            Shaders.Add("CUBEMAP_FILTER", LoadShader("CubemapFilter"));
            Shaders.Add("IRRADIANCE_CUBEMAP", LoadShader("IrradianceCubemap"));
            Shaders.Add("SHADOWPREPASS", LoadShader("ShadowPrepass"));

            Shaders.Add("LUT_DISPLAY", LoadShader("LUT/LutDisplay"));



        }

        public static ShaderProgram GetShader(string key, string path)
        {
            if (!Shaders.ContainsKey(key)) {
                Shaders.Add(key, LoadShader(path));
                Shaders[key].Link();
            }
            return Shaders[key];
        }

        public static ShaderProgram GetShader(string key)
        {
            if (!Shaders.ContainsKey("BASIC")) Init();

            if (Shaders.ContainsKey(key))
            {
                Shaders[key].Link();
                return Shaders[key];
            }
            return null;
        }

        static ShaderProgram LoadShader(string name)
        {
            Console.WriteLine($"LoadShader {name}");

            List<Shader> shaders = new List<Shader>();

            string shaderFolder = $"Shaders//";
            string frag = $"{shaderFolder}{name}.frag";
            string vert = $"{shaderFolder}{name}.vert";
            string geom = $"{shaderFolder}{name}.geom";
            if (File.Exists(vert)) shaders.Add(new VertexShader(File.ReadAllText(vert)));
            if (File.Exists(frag)) shaders.Add(new FragmentShader(File.ReadAllText(frag)));
            if (File.Exists(geom)) shaders.Add(new GeomertyShader(File.ReadAllText(geom)));

            return new ShaderProgram(shaders.ToArray());
        }
    }
}
