using System;
using System.Collections.Generic;
using System.Text;
using Toolbox.Core;
using Toolbox.Core.Imaging;
using Toolbox.Core.WiiU;
using BfresLibrary.WiiU;
using BfresLibrary;
using BfresLibrary.GX2;
using GLFrameworkEngine;
using CafeStudio.UI;

namespace BfresEditor
{
    public class FtexTexture : STGenericTexture, IDragDropNode
    {
        public TextureAsset TextureAsset { get; set; }

        /// <summary>
        /// The texture section used in the bfres.
        /// </summary>
        public Texture Texture;

        /// <summary>
        /// The file in which the data in this section is parented to.
        /// </summary>
        public ResFile ParentFile { get; set; }

        public FtexTexture() { }

        public FtexTexture(ResFile resFile, Texture texture) : base() {
            ParentFile = resFile;
            Texture = texture;
            ReloadImage();
        }

        private void ReloadImage()
        {
            Name = Texture.Name;
            Width = Texture.Width;
            Height = Texture.Height;
            MipCount = Texture.MipCount;
            Depth = Texture.Depth;
            ArrayCount = Texture.ArrayLength;
            RedChannel = SetChannel(Texture.CompSelR);
            GreenChannel = SetChannel(Texture.CompSelG);
            BlueChannel = SetChannel(Texture.CompSelB);
            AlphaChannel = SetChannel(Texture.CompSelA);

            Platform = new WiiUSwizzle((GX2.GX2SurfaceFormat)Texture.Format)
            {
                AAMode = (GX2.GX2AAMode)Texture.AAMode,
                TileMode = (GX2.GX2TileMode)Texture.TileMode,
                SurfaceDimension = (GX2.GX2SurfaceDimension)Texture.Dim,
                SurfaceUse = (GX2.GX2SurfaceUse)Texture.Use,
                MipOffsets = Texture.MipOffsets,
                Swizzle = Texture.Swizzle,
                Alignment = Texture.Alignment,
                MipData = Texture.MipData,
                Pitch = Texture.Pitch,
            };
            if (Texture.Format.ToString().Contains("SRGB"))
                IsSRGB = true;

            if (Texture.Dim == GX2SurfaceDim.Dim2DArray)
                SurfaceType = STSurfaceType.Texture2D_Array;

            DisplayProperties = Texture;

            this.LoadRenderableTexture();
            TextureAsset = new TextureAsset();
            TextureAsset.RenderableTex = (GLTexture)this.RenderableTex;
        }

        public override byte[] GetImageData(int ArrayLevel = 0, int MipLevel = 0, int DepthLevel = 0) {
            return Texture.Data;
        }

        public override void SetImageData(List<byte[]> imageData, uint width, uint height, int arrayLevel = 0)
        {
            throw new NotImplementedException();
        }

        private STChannelType SetChannel(GX2CompSel channelType)
        {
            if (channelType == GX2CompSel.ChannelR) return STChannelType.Red;
            else if (channelType == GX2CompSel.ChannelG) return STChannelType.Green;
            else if (channelType == GX2CompSel.ChannelB) return STChannelType.Blue;
            else if (channelType == GX2CompSel.ChannelA) return STChannelType.Alpha;
            else if (channelType == GX2CompSel.Always0) return STChannelType.Zero;
            else return STChannelType.One;
        }
    }
}
