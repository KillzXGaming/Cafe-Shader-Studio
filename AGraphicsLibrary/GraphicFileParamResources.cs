using System;
using System.Collections.Generic;
using System.IO;
using AampLibraryCSharp;
using Toolbox.Core;

namespace AGraphicsLibrary
{
    public class GraphicFileParamResources
    {
        public Dictionary<string, EnvironmentGraphics> EnvFiles = new Dictionary<string, EnvironmentGraphics>();
        public Dictionary<string, AglLightMap> LightMapFiles = new Dictionary<string, AglLightMap>();
        public Dictionary<string, CubeMapGraphics> CubeMapFiles = new Dictionary<string, CubeMapGraphics>();
        public Dictionary<string, AreaCollection> CollectFiles = new Dictionary<string, AreaCollection>();
        public Dictionary<string, ColorCorrection> ColorCorrectionFiles = new Dictionary<string, ColorCorrection>();
        public Dictionary<string, ShadowGraphics> ShadowFiles = new Dictionary<string, ShadowGraphics>();

        public GraphicFileParamResources()
        {
            Init();
        }

        public void ClearFiles()
        {
            ColorCorrectionFiles.Clear();
            EnvFiles.Clear();
            CollectFiles.Clear();
            ShadowFiles.Clear();
            CubeMapFiles.Clear();
            LightMapFiles.Clear();
        }

        public void LoadArchive(List<ArchiveFileInfo> files)
        {
            ClearFiles();
            foreach (var file in files)
            {
                if (file.FileName.Contains("baglccr"))
                    ColorCorrectionFiles.Add(file.FileName, LoadColorCorrection(file.FileData));
                if (file.FileName.Contains("baglenv"))
                    EnvFiles.Add(file.FileName, LoadEnvironmentGraphics(file.FileData));
                if (file.FileName.Contains("bgsdw"))
                    ShadowFiles.Add(file.FileName, LoadShadowGraphics(file.FileData));
                if (file.FileName.Contains("baglcube"))
                    CubeMapFiles.Add(file.FileName, LoadCubemapGraphics(file.FileData));
                if (file.FileName.Contains("collect.genvres"))
                    CollectFiles.Add(file.FileName, new AreaCollection(file.FileData));
                if (file.FileName.Contains("bagllmap"))
                    LightMapFiles.Add(file.FileName, new AglLightMap(file.FileData));
            }
            Init();
        }

        public void Init()
        {
            //Init with default files for default engine usage
            if (EnvFiles.Count == 0)
            {
                EnvFiles.Add("pointlight_course.baglenv", new EnvironmentGraphics());
                EnvFiles.Add("pointlight_player.baglenv", new EnvironmentGraphics());
                EnvFiles.Add("course_area.baglenv", new EnvironmentGraphics());
            }
            if (CollectFiles.Count == 0)
                CollectFiles.Add("collect.genvres", new AreaCollection());
            if (ShadowFiles.Count == 0)
                ShadowFiles.Add("stage.bgsdw", new ShadowGraphics());
            if (ColorCorrectionFiles.Count == 0)
                ColorCorrectionFiles.Add("map.baglccr", new ColorCorrection());
            if (LightMapFiles.Count == 0)
                LightMapFiles.Add("map.bagllmap", new AglLightMap());
            if (CubeMapFiles.Count == 0)
                CubeMapFiles.Add("stage.baglcube", new CubeMapGraphics());

            CubeMapFiles["stage.baglcube"].CubeMapObjects.Add(new CubeMapObject());
        }

        private ColorCorrection LoadColorCorrection(Stream file)
        {
            var aamp = AampFile.LoadFile(file);
            return new ColorCorrection(aamp);
        }

        private EnvironmentGraphics LoadEnvironmentGraphics(Stream file)
        {
            var aamp = AampFile.LoadFile(file);
            return new EnvironmentGraphics(aamp);
        }

        private ShadowGraphics LoadShadowGraphics(Stream file)
        {
            var aamp = AampFile.LoadFile(file);
            return new ShadowGraphics(aamp);
        }

        private CubeMapGraphics LoadCubemapGraphics(Stream file)
        {
            var aamp = AampFile.LoadFile(file);
            return new CubeMapGraphics(aamp);
        }
    }
}
