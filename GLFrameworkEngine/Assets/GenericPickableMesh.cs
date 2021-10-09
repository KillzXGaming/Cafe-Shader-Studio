using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using Toolbox.Core;

namespace GLFrameworkEngine
{
    public class GenericPickableMesh 
    {
        public string Name;

        public Pass Pass = Pass.OPAQUE;

        public Vector4 BoundingSphere = Vector4.Zero;

        public bool Hovered;

        public bool InFrustum = true;

        public virtual MaterialAsset MaterialAsset { get; set; }

        public STGenericMaterial Material
        {
            get { return MaterialAsset.MaterialData; }
            set { MaterialAsset.MaterialData = value; }
        }

        public BoundingNode BoundingNode = new BoundingNode();

        public GenericPickableMesh()  {
            MaterialAsset = new MaterialAsset();
        }

        public virtual void UpdateVertexBuffer()
        {

        }

        public virtual void AssignAsset(AssetBase asset)
        {
            if (asset is MaterialAsset) {
                MaterialAsset = (MaterialAsset)asset;
            }
        }

        public virtual void Render(GLContext control, ShaderProgram shader)
        {

        }

        public virtual void Dispose()
        {
        }
    }
}
