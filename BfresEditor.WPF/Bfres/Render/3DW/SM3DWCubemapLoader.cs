using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Toolbox.Core.IO;
using Toolbox.Core;
using GLFrameworkEngine;

namespace BfresEditor
{
    public class SM3DWCubemapLoader
    {
        public static string Stage = "EnterCatMarioStage";

        static Dictionary<string, CubemapArea> Areas = new Dictionary<string, CubemapArea>();

        public static void LoadCubemap()
        {
            Areas.Clear();

            string path = $"{SM3DWShaderLoader.GamePath}\\CubeMapTextureData\\{Stage}.szs";

            //Load the cubemap (archive -> bfres textures)
            var file = STFileLoader.OpenFileFormat(path) as IArchiveFile;
            var cubemapArchive = file.Files.FirstOrDefault().OpenFile() as BFRES;

            if (!Directory.Exists("TextureCache"))
                Directory.CreateDirectory("TextureCache");

            foreach (var texture in cubemapArchive.Textures)
            {
                if (!File.Exists($"TextureCache\\{texture.Name}.dds") && texture.Name.StartsWith("Default_Obj"))
                    texture.SaveDDS($"TextureCache\\{texture.Name}.dds");
            }

            foreach (var texture in cubemapArchive.Textures)
            {
                if (!texture.Name.StartsWith("Default_Obj"))
                    continue;

                var dds = new DDS($"TextureCache\\{texture.Name}.dds");

                //Cubemaps load into areas and have presets from render info in materials
                string areaName = texture.Name.Split('_').FirstOrDefault();
                string presetName = texture.Name.Split('_').LastOrDefault();

                if (!Areas.ContainsKey(areaName))
                    Areas.Add(areaName, new CubemapArea());

                Areas[areaName].Cubemaps.Add(presetName, GLTextureCube.FromDDS(dds));
            }
        }

        public static GLTexture GetCubemap(FMAT material)
        {
            //Get cubemap from roughness preset.
            var preset = material.GetRenderInfo("roughness_preset");
            return Areas["Default"].Cubemaps[$"Obj{preset}"];
        }

        public static GLTexture GetIrradianceCubemap() {
            return Areas["Default"].Cubemaps["ObjIrradiance"];
        }
        
        class CubemapArea
        {
            public Dictionary<string, GLTexture> Cubemaps = new Dictionary<string, GLTexture>();
        }
    }
}
