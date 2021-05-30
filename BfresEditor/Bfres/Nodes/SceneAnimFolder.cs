using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BfresLibrary;
using GLFrameworkEngine;

namespace BfresEditor
{
    public class SceneAnimFolder : SubSectionBase
    {
        public override string Header => "Scene Animations";

        public SceneAnimFolder(BFRES bfres, ResFile resFile, ResDict<SceneAnim> resDict)
        {
            foreach (SceneAnim anim in resDict.Values)
            {
                var node = new BfresNodeBase(anim.Name);
                AddChild(node);

                foreach (CameraAnim camAnim in anim.CameraAnims.Values) {
                    var camnode = new BfresNodeBase(camAnim.Name);
                    node.AddChild(camnode);

                    var fsha = new BfresCameraAnim(camAnim);
                    node.Tag = fsha;
                }
                foreach (LightAnim lightAnim in anim.LightAnims.Values)
                {
                    var camnode = new BfresNodeBase(lightAnim.Name);
                    node.AddChild(camnode);
                }
                foreach (FogAnim fogAnim in anim.FogAnims.Values)
                {
                    var camnode = new BfresNodeBase(fogAnim.Name);
                    node.AddChild(camnode);
                }
            }
        }
    }
}
