using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GLFrameworkEngine;

namespace BfresEditor
{
    public class RenderLayer
    {
        public static List<BfresMeshAsset> Sort(BfresModelAsset model)
        {
            //Seperate draw lists by render state
            var opaqueMaskMeshes = model.Meshes.Where(x =>
            ((FMAT)x.Material).BlendState.State == GLMaterialBlendState.BlendState.Opaque ||
            ((FMAT)x.Material).BlendState.State == GLMaterialBlendState.BlendState.Mask);

            var transCustomMeshes = model.Meshes.Where(x =>
            ((FMAT)x.Material).BlendState.State == GLMaterialBlendState.BlendState.Translucent ||
            ((FMAT)x.Material).BlendState.State == GLMaterialBlendState.BlendState.Custom);

            List<BfresMeshAsset> meshes = new List<BfresMeshAsset>();
            meshes.AddRange(SortPriority(opaqueMaskMeshes));
            meshes.AddRange(SortPriority(transCustomMeshes));
            return meshes;
        }

        public static List<BfresMeshAsset> SortPriority(IEnumerable<BfresMeshAsset> meshes) {
            return meshes.OrderBy(x => (((BfresMeshAsset)x).Priority)).ToList();
        }
    }
}
