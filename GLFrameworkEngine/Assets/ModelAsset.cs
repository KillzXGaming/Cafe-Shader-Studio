using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using Toolbox.Core;

namespace GLFrameworkEngine
{
    [Serializable]
    public class ModelAsset : AssetBase
    {
        public virtual STGenericModel ModelData { get; set; }

        public virtual bool IsVisible { get; set; } = true;

        public Vector4 BoundingSphere { get; set; }

        public virtual IEnumerable<GenericPickableMesh> MeshList { get; } = new List<GenericPickableMesh>();
    }
}
