using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BfresLibrary;

namespace BfresEditor
{
    public class SkeletalAnimFolder : SubSectionBase
    {
        public override string Header => "Skeletal Animations";

        public SkeletalAnimFolder(BFRES bfres, ResFile resFile, ResDict<SkeletalAnim> resDict)
        {
            foreach (SkeletalAnim anim in resDict.Values)
            {
                var node = new BfresNodeBase(anim.Name);
                node.Icon = "/Images/SkeletonAnimation.png";
                AddChild(node);

                var fska = new BfresSkeletalAnim(resFile, anim, resFile.Name);
                node.Tag = fska;
                bfres.SkeletalAnimations.Add(fska);
            }
        }
    }
}
