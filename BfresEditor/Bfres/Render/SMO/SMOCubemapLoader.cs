using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Toolbox.Core.IO;
using Toolbox.Core;
using RedStarLibrary;
using GLFrameworkEngine;

namespace BfresEditor
{
    //Todo this would work with ripping, however the prefilter effect is not supported yet
    //The current tool only uses the second HDR cubemap cached to resources
    public class SMOCubemapLoader
    {
        public static string Stage = "CapWorldHomeStage";

        static Dictionary<string, CubemapArea> Scenarios = new Dictionary<string, CubemapArea>();

        public static void LoadCubemap()
        {
            Scenarios.Clear();

            string path = $"{GlobalSettings.GamePath}\\ObjectData\\CubeMap{Stage}.szs";

            //Load the cubemap (archive -> bfres textures)
            var file = STFileLoader.OpenFileFormat(path) as IArchiveFile;
            var cubemapArchive = file.Files.FirstOrDefault().OpenFile() as BFRES;

            if (!Directory.Exists("TextureCache"))
                Directory.CreateDirectory("TextureCache");

            foreach (var texture in cubemapArchive.Textures)
            {
                if (texture.Name != "Default_")
                    continue;

                if (!File.Exists($"TextureCache\\{texture.Name}.dds"))
                    texture.SaveDDS($"TextureCache\\{texture.Name}.dds");
            }

            foreach (var texture in cubemapArchive.Textures)
            {
                if (texture.Name != "Default_")
                    continue;

                var dds = new DDS($"TextureCache\\{texture.Name}.dds");
                dds.Parameters.UseSoftwareDecoder = true;
                dds.Parameters.FlipY = true;

                //Cubemaps load into areas and have presets from render info in materials
                string sceneName = texture.Name.Split('_').LastOrDefault();
                string cubemapType = texture.Name.Split('_').FirstOrDefault();

                if (string.IsNullOrWhiteSpace(sceneName))
                    sceneName = "Scenario1";

                if (!Scenarios.ContainsKey(sceneName))
                    Scenarios.Add(sceneName, new CubemapArea());

                Scenarios[sceneName].Cubemaps.Add(cubemapType, GLTextureCube.FromDDS(dds));
            }
        }

        public static GLTexture GetDefaultCubemap(int id = 1) {
            return Scenarios[GetAreaName(id)].Cubemaps["Default"];
        }

        public static GLTexture GetSeaCubemap(int id = 1) {
            return Scenarios[GetAreaName(id)].Cubemaps["Sea"];
        }

        public static GLTexture GetSkyboxCubemap(int id = 0) {
            return Scenarios[GetAreaName(id)].Cubemaps["SkyOnly"];
        }

        static string GetAreaName(int id) {
            return $"Scenario{id}";
        }

        class CubemapArea
        {
            public Dictionary<string, GLTexture> Cubemaps = new Dictionary<string, GLTexture>();
        }
    }
}
