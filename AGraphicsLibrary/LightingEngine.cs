﻿using System;
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

        //A lookup of lightmaps per object to get probe lighting (made from static light map base)
        public Dictionary<string, GLTextureCube> ProbeLightmaps = new Dictionary<string, GLTextureCube>();

        public GraphicFileParamResources Resources = new GraphicFileParamResources();

        public GLTexture3D ColorCorrectionTable;
        public GLTexture2DArray LightPrepassTexture;
        public GLTexture2D ShadowPrepassTexture;

        public bool UpdateColorCorrection = false;

        //3D Viewer settings
        public bool DisplayFog { get; set; } = true;
        public bool DisplayBloom { get; set; } = true;

        public void LoadArchive(List<ArchiveFileInfo> files)
        {
            Resources.LoadArchive(files);
            InitTextures();
        }

        public void InitTextures()
        {
            //Used for dynamic lights. Ie spot, point, lights
            //Dynamic lights are setup using the g buffer pass (normals) and depth information before material pass is drawn
            //Additional slices may be used for bloom intensity
            LightPrepassTexture = GLTexture2DArray.CreateUncompressedTexture(4, 4, 1, 1,
                PixelInternalFormat.Rgba16f, PixelFormat.Rgba, PixelType.Float);

            LightPrepassTexture.Bind();
            LightPrepassTexture.MagFilter = TextureMagFilter.Linear;
            LightPrepassTexture.MinFilter = TextureMinFilter.Linear;
            LightPrepassTexture.UpdateParameters();
            LightPrepassTexture.Unbind();

            //Shadows
            //Channel usage:
            //Red - Dynamic shadows
            //Green - Static shadows (course, for casting onto objects)
            //Blue - Soft shading (under kart, dynamic AO?)
            //Alpha - Usually gray
            ShadowPrepassTexture = GLTexture2D.CreateWhiteTexture(4, 4);

            foreach (var lmap in Resources.LightMapFiles.Values)
                lmap.Setup();
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

        public void UpdateLightPrepass(GLContext control, GLTexture gbuffer, GLTexture linearDepth) {
            if (LightPrepassTexture == null) {
                LightPrepassTexture = GLTexture2DArray.CreateUncompressedTexture(control.Width, control.Height, 1, 1,
                     PixelInternalFormat.R11fG11fB10f, PixelFormat.Rgb, PixelType.UnsignedInt10F11F11FRev);
                LightPrepassTexture.Bind();
                LightPrepassTexture.MagFilter = TextureMagFilter.Linear;
                LightPrepassTexture.MinFilter = TextureMinFilter.Linear;
                LightPrepassTexture.UpdateParameters();
                LightPrepassTexture.Unbind();
            }
            LightPrepassManager.CreateLightPrepassTexture(control, gbuffer, linearDepth, LightPrepassTexture);
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

        public void UpdateCubemap(List<GenericRenderer> renders, bool isWiiU) {
            CubemapManager.GenerateCubemaps(renders, isWiiU);
        }

        public void UpdateLightmap(GLContext control, string lightMapName)
        {
            var lmap = Resources.LightMapFiles.FirstOrDefault().Value;
            var env = Resources.EnvFiles["course_area.baglenv"];

            lmap.GenerateLightmap(control, env, lightMapName);
        }

        public ProbeMapManager.ProbeOutput UpdateProbeCubemap(GLContext control, GLTextureCube probeMap, OpenTK.Vector3 position)
        {
            var lmap = Resources.LightMapFiles.FirstOrDefault().Value;
            var collectRes = Resources.CollectFiles.FirstOrDefault().Value;

            //Find the area to get the current light map
            var areaObj = collectRes.GetArea(position.X, position.Y, position.Z);
            var lightMapName = areaObj.GetLightmapName();

            if (!lmap.Lightmaps.ContainsKey(lightMapName))
                UpdateLightmap(control, lightMapName);

            return ProbeMapManager.Generate(control, lmap.Lightmaps[lightMapName], probeMap.ID, position);
        }
    }
}
