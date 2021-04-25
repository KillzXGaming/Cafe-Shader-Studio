using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;

namespace BfresEditor
{
    public class TextureMap : STGenericTextureMap
    {
        public override STGenericTexture GetTexture()
        {
            foreach (var cachedModel in GLFrameworkEngine.DataCache.ModelCache.Values)
            {
                if (cachedModel is BfresRender) {
                    var bfres = cachedModel as BfresRender;
                    if (bfres.Textures.ContainsKey(this.Name))
                        return bfres.Textures[this.Name];
                }
            }
            return null;
        }
    }
}
