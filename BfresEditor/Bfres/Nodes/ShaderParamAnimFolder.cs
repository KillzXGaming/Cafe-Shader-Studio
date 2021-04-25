using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BfresLibrary;

namespace BfresEditor
{
    public class ShaderParamAnimFolder : SubSectionBase
    {
        public override string Header => "Shader Param Animations";

        public ShaderParamAnimFolder(BFRES bfres, ResFile resFile, ResDict<MaterialAnim> resDict)
        {
            foreach (MaterialAnim anim in resDict.Values)
            {
                var node = new BfresNodeBase(anim.Name);
                AddChild(node);

                var fmaa = new BfresMaterialAnim(anim, resFile.Name);
                node.Tag = fmaa;
                bfres.MaterialAnimations.Add(fmaa);
            }
        }
    }
}
