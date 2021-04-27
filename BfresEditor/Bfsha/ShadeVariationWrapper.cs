using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BfshaLibrary;
using CafeStudio.UI;

namespace BfresEditor
{
    public class ShadeVariationWrapper : IPropertyUI
    {
        public ShaderVariation VariationData;

        public ShadeVariationWrapper(ShaderVariation variation) {
            VariationData = variation;
        }

        public Type GetTypeUI() => typeof(ShaderVariationGUI);

        public void OnLoadUI(object uiInstance)
        {

        }

        public void OnRenderUI(object uiInstance)
        {
            var editor = (ShaderVariationGUI)uiInstance;
            editor.Render(VariationData);
        }
    }
}
