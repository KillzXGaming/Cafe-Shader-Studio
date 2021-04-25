using System;
using System.Collections.Generic;
using BfresLibrary;
using Toolbox.Core;
using GLFrameworkEngine;
using Syroot.NintenTools.NSW.Bntx;

namespace BfresEditor
{
    public class TextureFolder : SubSectionBase
    {
        public override string Header => "Textures";

        public List<AssetBase> Assets
        {
            get
            {
                List<AssetBase> assetList = new List<AssetBase>();
                for (int i = 0; i < Children.Count; i++)
                {
                    if (Children[i].Tag is FtexTexture)
                        assetList.Add(((FtexTexture)Children[i].Tag).TextureAsset);
                }
                return assetList;
            }
        }

        public TextureFolder(BFRES bfres, ResFile resFile, ResDict<TextureShared> resDict, ExternalFile externalTextureFile)
        {
            if (externalTextureFile != null)
            {
                var bntxFile = externalTextureFile.LoadedFileData as BntxFile;
                Tag = new BntxWrapper(bntxFile);
            }

            List<TextureAsset> assetList = new List<TextureAsset>();
            foreach (TextureShared tex in resDict.Values)
            {
                var node = new BfresNodeBase(tex.Name);
                node.Icon = "/Images/Texture.png";
                AddChild(node);

                if (tex is BfresLibrary.WiiU.Texture)
                {
                    FtexTexture ftex = new FtexTexture(resFile, (BfresLibrary.WiiU.Texture)tex);
                    bfres.Textures.Add(ftex);
                    node.Tag = ftex;
                    assetList.Add(ftex.TextureAsset);
                }
                else
                {
                    var texture = (BfresLibrary.Switch.SwitchTexture)tex;
                    BntxTexture bntxTexture = new BntxTexture(texture.BntxFile, texture.Texture);
                    bfres.Textures.Add(bntxTexture);
                    node.Tag = bntxTexture;
                }
            }
        }
    }
}
