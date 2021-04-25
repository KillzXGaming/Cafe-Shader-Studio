using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BfresLibrary;
using GLFrameworkEngine;

namespace BfresEditor
{
    public class BoneVisibilityAnimFolder : SubSectionBase
    {
        public override string Header => "Bone Visibility Animations";

        public BoneVisibilityAnimFolder(BFRES bfres, ResFile resFile, ResDict<VisibilityAnim> resDict)
        {
            foreach (VisibilityAnim anim in resDict.Values)
            {
                var node = new BfresNodeBase(anim.Name);
                AddChild(node);

                var fsha = new BfresVisibilityAnim(anim, resFile.Name);
                node.Tag = fsha;
                bfres.VisibilityAnimations.Add(fsha);
            }
        }
    }
}
