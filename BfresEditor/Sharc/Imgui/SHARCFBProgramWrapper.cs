using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BfshaLibrary;
using CafeStudio.UI;

namespace BfresEditor
{
    public class SHARCFBProgramWrapper : IPropertyUI
    {
        public GX2VertexShader VertexBinary;
        public GX2PixelShader FragmentBinary;

        public SHARCFBProgramWrapper(GX2Shader vertexShader, GX2Shader fragmentShader) {
            VertexBinary = (GX2VertexShader)vertexShader;
            FragmentBinary = (GX2PixelShader)fragmentShader;
        }

        public Type GetTypeUI() => typeof(GX2ShaderGUI);

        public void OnLoadUI(object uiInstance)
        {
            var editor = (GX2ShaderGUI)uiInstance;
            editor.OnLoad(VertexBinary, FragmentBinary);
        }

        public void OnRenderUI(object uiInstance)
        {
            var editor = (GX2ShaderGUI)uiInstance;
            editor.Render(VertexBinary, FragmentBinary);
        }
    }
}
