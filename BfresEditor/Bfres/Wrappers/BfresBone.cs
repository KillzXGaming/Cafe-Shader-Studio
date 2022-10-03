using System;
using System.Collections.Generic;
using System.Text;
using BfresLibrary;
using BfresLibrary.GX2;
using BfresLibrary.Helpers;
using Toolbox.Core;
using CafeStudio.UI;
using OpenTK;

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
            //Update the flags when transform has been adjusted
            UpdateTransformFlags();
        }

        /// <summary>
        /// Updates the current bone transform flags.
        /// These flags determine what matrices can be ignored for matrix updating.
        /// </summary>
        public void UpdateTransformFlags()
        {
            BoneFlagsTransform flags = 0;

            //SRT checks to update matrices
            if (this.Position == Vector3.Zero)
                flags |= BoneFlagsTransform.TranslateZero;
            if (this.Scale == Vector3.One)
            {
                flags |= BoneFlagsTransform.ScaleOne;
                flags |= BoneFlagsTransform.ScaleVolumeOne;
            }
            if (this.Rotation == Quaternion.Identity)
                flags |= BoneFlagsTransform.RotateZero;

            //Extra scale flags
            if (this.Scale.X == this.Scale.Y && this.Scale.X == this.Scale.Z)
                flags |= BoneFlagsTransform.ScaleUniform;

            BoneData.FlagsTransform = flags;
        }

        /// <summary>
        /// Gets the transformation of the bone without it's parent transform applied.
        /// </summary>
        public override Matrix4 GetTransform()
        {
            var transform = Matrix4.Identity;
            if (BoneData.FlagsTransform.HasFlag(BoneFlagsTransform.Identity))
                return transform;

            if (!BoneData.FlagsTransform.HasFlag(BoneFlagsTransform.ScaleOne))
                transform *= Matrix4.CreateScale(Scale);
            if (!BoneData.FlagsTransform.HasFlag(BoneFlagsTransform.RotateZero))
                transform *= Matrix4.CreateFromQuaternion(Rotation);
            if (!BoneData.FlagsTransform.HasFlag(BoneFlagsTransform.TranslateZero))
                transform *= Matrix4.CreateTranslation(Position);

            return transform;
        }

        public void OnRenderUI(object uiInstance)
        {
            var editor = (BoneEditor)uiInstance;
            editor.LoadEditor(this);
        }
    }
}
