using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.Animations;
using Toolbox.Core;
using BfresLibrary;
using GLFrameworkEngine;
using MapStudio.Rendering;
using OpenTK;

namespace BfresEditor
{
    public class BfresVisibilityAnim : STAnimation
    {
        private string ModelName = null;

        public int KeyIndex = 0;

        public BfresVisibilityAnim(VisibilityAnim anim, string name)
        {
            ModelName = name;
            CanPlay = false; //Default all animations to not play unless toggled in list
            Reload(anim);
        }

        public override void NextFrame()
        {
            foreach (BoneAnimGroup group in AnimGroups)
            {
                var skeletons = GetActiveSkeletons();
                foreach (var skeleton in skeletons)
                {
                    var bone = skeleton.SearchBone(group.Name);
                    if (bone == null)
                        continue;

                    ParseKeyTrack(bone, group);
                }
            }
        }

        private void ParseKeyTrack(STBone bone, BoneAnimGroup group)
        {
            float value = group.Track.GetFrameValue(this.Frame);
            bool isVisible = value != 0;
            bone.Visible = isVisible;
        }

        public void Reload(VisibilityAnim anim)
        {
            Name = anim.Name;
            FrameCount = anim.FrameCount;
            FrameRate = 60.0f;
            Loop = anim.Flags.HasFlag(VisibilityAnimFlags.Looping);

            if (anim.Names == null)
                return;

            AnimGroups.Clear();
            for (int i = 0; i < anim.Names.Count; i++)
            {
                var baseValue = anim.BaseDataList[i];

                BoneAnimGroup group = new BoneAnimGroup();
                group.Name = anim.Names[i];
                AnimGroups.Add(group);

                group.Track.KeyFrames.Add(new STKeyFrame() { Frame = 0, Value = baseValue ? 1 : 0 });

                if (anim.Curves.Count > i) {
                    BfresAnimations.GenerateKeys(group.Track, anim.Curves[i]);
                }
            }
        }

        public class BoneAnimGroup : STAnimGroup
        {
            public BfresAnimationTrack Track = new BfresAnimationTrack();
        }

        private List<BfresMeshAsset> GetMeshes(string name)
        {
            if (!DataCache.ModelCache.ContainsKey(ModelName))
                return new List<BfresMeshAsset>();

            List<BfresMeshAsset> meshes = new List<BfresMeshAsset>();

            var bfres = (BfresRender)DataCache.ModelCache[ModelName];

            //Don't animate objects out of the screen fustrum
            //  if (!bfres.InFustrum) return materials;

            foreach (BfresModelAsset model in bfres.Models) {
                foreach (var mesh in model.Meshes) {
                    if (mesh.Name == name)
                        meshes.Add(mesh);
                }
            }

            return meshes;
        }

        public STSkeleton[] GetActiveSkeletons()
        {
            List<STSkeleton> skeletons = new List<STSkeleton>();
            foreach (BfresRender render in DataCache.ModelCache.Values)
            {
                foreach (BfresModelAsset model in render.Models)
                    skeletons.Add(model.ModelData.Skeleton);
            }
            return skeletons.ToArray();
        }
    }
}
