using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BfresLibrary;

namespace BfresEditor
{
    public class ModelFolder : SubSectionBase
    {
        public override string Header => "Models";

        public ModelFolder(BFRES bfres, ResFile resFile, ResDict<Model> resDict)
        {
            foreach (Model model in resDict.Values)
            {
                var node = new BfresNodeBase(model.Name);
                node.Icon = "/Images/Model.png";
                AddChild(node);

                var fmdl = new FMDL(node, bfres, model);
                node.Tag = fmdl;
                bfres.Models.Add(fmdl);

                node.AddChild(new BfresNodeBase("Meshes"));
                node.AddChild(new MaterialFolder("Materials"));
                node.AddChild(new BfresNodeBase("Skeleton"));

                foreach (var mesh in fmdl.Meshes)
                {
                    var meshNode= new BfresNodeBase(mesh.Name)
                    {
                        Tag = mesh,
                        Icon = "/Images/Mesh.png"
                    };
                    mesh.MeshSelected = (o, s) => {
                        meshNode.IsSelected = (bool)o;
                    };
                    node.Children[0].AddChild(meshNode);
                }

                foreach (var mat in fmdl.Materials)
                    node.Children[1].AddChild(new BfresNodeBase(mat.Name) 
                    {
                        Tag = mat, Icon = ((FMAT)mat).Icon,
                    });
                foreach (var bone in ((FSKL)fmdl.Skeleton).CreateBoneHierarchy())
                    node.Children[2].AddChild(bone);
            }
        }
    }

    public class MaterialFolder : BfresNodeBase
    {
        public MaterialFolder(string text) : base(text)
        {

        }
    }
}
