using System;
using System.Collections.Generic;
using System.Text;
using BfresLibrary;
using BfresLibrary.GX2;
using BfresLibrary.Helpers;
using Toolbox.Core;
using CafeStudio.UI;

namespace BfresEditor
{
    public class BfresBone : STBone, IPropertyUI
    {
        public Bone BoneData { get; set; }
        public Skeleton ParentSkeletonData { get; set; }

        public bool RigidSkinning => BoneData.RigidMatrixIndex != -1;
        public bool SmoothSkinning => BoneData.SmoothMatrixIndex != -1;

        public bool UseSmoothMatrix => SmoothSkinning && !RigidSkinning;

        public BfresBone(STSkeleton skeleton) : base(skeleton)
        {

        }

        public Type GetTypeUI() => typeof(BoneEditor);

        public void OnLoadUI(object uiInstance)
        {
   
        }

        /// <summary>
        /// Updates the drawn bone transform back to the bfres file data
        /// </summary>
        public void UpdateBfresTransform()
        {
            BoneData.Position = new Syroot.Maths.Vector3F(
                this.Position.X,
                this.Position.Y,
                this.Position.Z);
            BoneData.Scale = new Syroot.Maths.Vector3F(
                this.Scale.X,
                this.Scale.Y,
                this.Scale.Z);

            if (ParentSkeletonData.FlagsRotation == SkeletonFlagsRotation.EulerXYZ)
            {
                BoneData.Rotation = new Syroot.Maths.Vector4F(
                    this.EulerRotation.X,
                    this.EulerRotation.Y,
                    this.EulerRotation.Z, 1.0f);
            }
            else
            {
                BoneData.Rotation = new Syroot.Maths.Vector4F(
                    this.Rotation.X,
                    this.Rotation.Y,
                    this.Rotation.Z,
                    this.Rotation.W);
            }
        }

        public void OnRenderUI(object uiInstance)
        {
            var editor = (BoneEditor)uiInstance;
            editor.LoadEditor(this);
        }
    }
}
