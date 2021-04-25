using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BfresLibrary;

namespace BfresEditor
{
    public class ColorAnimFolder : SubSectionBase
    {
        public override string Header => "Color Animations";

        public ColorAnimFolder(BFRES bfres, ResFile resFile, ResDict<MaterialAnim> resDict)
        {
            foreach (MaterialAnim anim in resDict.Values)
            {
                var node = new BfresNodeBase(anim.Name);
                AddChild(node);

                var fsha = new BfresMaterialAnim(anim, resFile.Name);
                node.Tag = fsha;
                bfres.MaterialAnimations.Add(fsha);
            }
        }
    }
}
