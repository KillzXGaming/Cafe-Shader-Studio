using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;

namespace BfresEditor
{
    public class BfresMeshEditor
    {
        MeshEditor MeshEditor = new MeshEditor();
        BfresMaterialEditor MaterialEditor = new BfresMaterialEditor();

        public void Init()
        {
            MaterialEditor.Init();
        }

        public void LoadEditor(FSHP mesh)
        {
            var material = mesh.Material;
            //MeshEditor.Render(mesh);
            MaterialEditor.LoadEditorMenus(material);
        }
    }
}
