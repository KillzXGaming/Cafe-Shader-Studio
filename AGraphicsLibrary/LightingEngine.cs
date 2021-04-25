using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using AampLibraryCSharp;
using OpenTK.Graphics.OpenGL;
using GLFrameworkEngine;
using Toolbox.Core;

namespace AGraphicsLibrary
{
    public class LightingEngine
    {
        public static LightingEngine LightSettings = new LightingEngine();

        //A lookup of lightmaps per area. Used to get the static base light map generated on level load
        public Dictionary<int, GLTextureCube> Lightmaps = new Dictionary<int, GLTextureCube>();
        //A lookup of lightmaps per object to get probe lighting (made from static light map base)
        public Dictionary<string, GLTextureCube> ProbeLightmaps = new Dictionary<string, GLTextureCube>();

        public GLTexture3D ColorCorrectionTable;
        public GLTexture2DArray LightPrepassTexture;
        public GLTexture2D LightPrepassTexture2D;
        public GLTexture2D ShadowPrepassTexture;

        public bool UpdateColorCorrection = false;

        public ColorCorrection ColorCorrection { get; set; }

        public CubeMapGraphics CubeMaps { get; set; }

        public EnvironmentGraphics CourseArea { get; set; }
        public EnvironmentGraphics PointlightPlayer { get; set; }
        public EnvironmentGraphics PointlightCourse { get; set; }

        public ShadowGraphics StageShadows { get; set; }

        public AreaCollection CollectResource { get; set; }

        //3D Viewer settings
        public bool DisplayFog { get; set; } = true;
        public bool DisplayBloom { get; set; } = true;

        public LightingEngine()
        {
            ColorCorrection = new ColorCorrection();
            CourseArea = new EnvironmentGraphics();
            PointlightPlayer = new EnvironmentGraphics();
            PointlightCourse = new EnvironmentGraphics();
            StageShadows = new ShadowGraphics();
            CubeMaps = new CubeMapGraphics();
            CollectResource = new AreaCollection();

            CubeMaps.CubeMapObjects.Add(new CubeMapObject());
        }

        public void InitTextures()
        {
            if (ShadowPrepassTexture != null)
                return;

            ShadowPrepassTexture = GLTexture2D.CreateUncompressedTexture(4, 4,
                PixelInternalFormat.Rgba, PixelFormat.Rgba, PixelType.UnsignedByte);

            LightPrepassTexture = GLTexture2DArray.CreateUncompressedTexture(4, 4,
                           PixelInternalFormat.R11fG11fB10f, PixelFormat.Rgb, PixelType.UnsignedInt10F11F11FRev);
        }


        public void LoadArchive(List<ArchiveFileInfo> files)
        {
            foreach (var file in files)
            {
                if (file.FileName.Contains("baglccr"))
                    ColorCorrection = LoadColorCorrection(file.FileData);
                if (file.FileName.Contains("course_area.baglenv"))
                    CourseArea = LoadEnvironmentGraphics(file.FileData);
                if (file.FileName.Contains("pointlight_course.baglenv"))
                    PointlightCourse = LoadEnvironmentGraphics(file.FileData);
                if (file.FileName.Contains("pointlight_player.baglenv"))
                    PointlightPlayer = LoadEnvironmentGraphics(file.FileData);
                if (file.FileName.Contains("stage.bgsdw"))
                    StageShadows = LoadShadowGraphics(file.FileData);
                if (file.FileName.Contains("stage.baglcube"))
                    CubeMaps = LoadCubemapGraphics(file.FileData);
                if (file.FileName.Contains("collect.genvres"))
                    CollectResource = new AreaCollection(file.FileData);
            }
            InitTextures();
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

        public void UpdateColorCorrectionTable()
        {
            if (ColorCorrectionTable == null)
            {
                ColorCorrectionTable = GLTexture3D.CreateUncompressedTexture(8, 8, 8,
                    PixelInternalFormat.R11fG11fB10f, PixelFormat.Rgb, PixelType.UnsignedInt10F11F11FRev);

                ColorCorrectionTable.Bind();
                ColorCorrectionTable.MagFilter = TextureMagFilter.Linear;
                ColorCorrectionTable.MinFilter = TextureMinFilter.Linear;
                ColorCorrectionTable.UpdateParameters();
                ColorCorrectionTable.Unbind();
            }

            ColorCorrectionManager3D.CreateColorLookupTexture(ColorCorrectionTable);
            UpdateColorCorrection = false;
        }

        public void UpdateLightPrepass(GLContext control, int normalsTexture, int depthTexture) {
            if (LightPrepassTexture == null)
            {
                LightPrepassTexture = GLTexture2DArray.CreateUncompressedTexture(control.Width, control.Height,
                     PixelInternalFormat.R11fG11fB10f, PixelFormat.Rgb, PixelType.UnsignedInt10F11F11FRev);
            }
            LightPrepassManager.CreateLightPrepassTexture(control,  normalsTexture, depthTexture, LightPrepassTexture);
        }

        public void UpdateShadowPrepass(GLContext control, int shadowMap, int depthTexture)
        {
            if (ShadowPrepassTexture == null)
            {
                ShadowPrepassTexture = GLTexture2D.CreateUncompressedTexture(control.Width, control.Height,
                     PixelInternalFormat.Rgba, PixelFormat.Rgba, PixelType.UnsignedByte);
            }
            ShadowPrepassManager.CreateShadowPrepassTexture(control, shadowMap, depthTexture, ShadowPrepassTexture);
        }

        public void UpdateCubemap(List<GenericRenderer> renders) {
            CubemapManager.GenerateCubemaps(renders);
        }

        public void UpdateLightmap(GLContext control, int areaIndex)
        {
            //Todo properly reverse engineer shader
            return;

            GLTextureCube output = null;
            if (!Lightmaps.ContainsKey(areaIndex))
            {
                output = GLTextureCube.CreateEmptyCubemap(
                 32, PixelInternalFormat.Rgb32f, PixelFormat.Rgb, PixelType.Float, 2);

                //Allocate mip data. Need 2 seperate mip levels
                output.Bind();
                output.MinFilter = TextureMinFilter.LinearMipmapLinear;
                output.MagFilter = TextureMagFilter.Linear;
                output.UpdateParameters();
                output.GenerateMipmaps();
                output.Unbind();

                Lightmaps.Add(areaIndex, output);
            }
            else
                output = Lightmaps[areaIndex];

            LightmapManager.CreateLightmapTexture(control, CourseArea, areaIndex, output);
        }

        public bool UpdateProbeCubemap(GLContext control, GLTextureCube probeMap, OpenTK.Vector3 position)
        {
            //Find the area to get the current light map
            var areaObj = LightSettings.CollectResource.GetArea(position.X, position.Y, position.Z);
            var areaIndex = areaObj.AreaIndex;

            if (!Lightmaps.ContainsKey(areaIndex))
                UpdateLightmap(control, areaIndex);

            return ProbeMapManager.Generate(control, Lightmaps[areaIndex], probeMap.ID, position);
        }
    }
}
