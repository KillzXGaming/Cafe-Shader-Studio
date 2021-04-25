using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BfresLibrary;

namespace BfresEditor
{
    public class ShapeAnimFolder : SubSectionBase
    {
        public override string Header => "Shape Animations";

        public ShapeAnimFolder(BFRES bfres, ResFile resFile, ResDict<ShapeAnim> resDict)
        {
            foreach (ShapeAnim anim in resDict.Values)
            {
                var node = new BfresNodeBase(anim.Name);
                AddChild(node);

                var fsha = new BfresShapeAnim(anim, resFile.Name);
                node.Tag = fsha;
                bfres.ShapeAnimations.Add(fsha);
            }
        }
    }
}
