using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.Animations;
using Toolbox.Core;
using BfresLibrary;
using OpenTK;
using GLFrameworkEngine;

namespace BfresEditor
{
    public class BfresSkeletalAnim : STSkeletonAnimation, IExportReplaceNode
    {
        public enum TrackType
        {
            XSCA = 0x4,
            YSCA = 0x8,
            ZSCA = 0xC,
            XPOS = 0x10,
            YPOS = 0x14,
            ZPOS = 0x18,
            XROT = 0x20,
            YROT = 0x24,
            ZROT = 0x28,
            WROT = 0x2C,
        }

        private string ModelName = null;

        private SkeletalAnim SkeletalAnim;

        public STSkeleton SkeletonOverride = null;

        private ResFile ParentFile;

        public BfresSkeletalAnim(ResFile resFile, SkeletalAnim anim, string name)
        {
            ParentFile = resFile;
            SkeletalAnim = anim;
            ModelName = name;
            CanPlay = false; //Default all animations to not play unless toggled in list
            Reload(anim);
        }

        public STSkeleton[] GetActiveSkeletons()
        {
            List<STSkeleton> skeletons = new List<STSkeleton>();
            if (SkeletonOverride != null)
            {
                skeletons.Add(SkeletonOverride);
                return skeletons.ToArray();
            }

            foreach (BfresRender render in DataCache.ModelCache.Values)
            {
                foreach (BfresModelAsset model in render.Models)
                    skeletons.Add(model.ModelData.Skeleton);
            }
            return skeletons.ToArray();
        }

        #region events

        public FileFilter[] ReplaceFilter => new FileFilter[]
        {
              new FileFilter(".bfska", "Raw Binary Animation"),
              new FileFilter(".json", "Readable Text"),
        };

        public FileFilter[] ExportFilter => new FileFilter[]
        {
              new FileFilter(".bfska", "Raw Binary Animation"),
              new FileFilter(".json", "Readable Text"),
        };

        public void Replace(string fileName)
        {
            if (Utils.GetExtension(fileName) == ".bfska")
                SkeletalAnim.Import(fileName, ParentFile);
            if (Utils.GetExtension(fileName) == ".json")
                SkeletalAnim.Import(fileName, ParentFile);

            Reload(SkeletalAnim);
        }

        public void Export(string fileName)
        {
            if (Utils.GetExtension(fileName) == ".dae")
            {
                Toolbox.Core.Collada.DAE.ExportAnimation(fileName, this, new Toolbox.Core.Collada.DAE.ExportSettings());
            }
            if (Utils.GetExtension(fileName) == ".bfska")
                SkeletalAnim.Export(fileName, ParentFile);
            if (Utils.GetExtension(fileName) == ".json")
                SkeletalAnim.Export(fileName, ParentFile);
        }

        #endregion

        /// <summary>
        /// Gets the active skeleton visbile in the scene that may be used for animation.
        /// </summary>
        /// <returns></returns>
        public override STSkeleton GetActiveSkeleton()
        {
            if (!DataCache.ModelCache.ContainsKey(ModelName))
                return null;

            var models = ((BfresRender)DataCache.ModelCache[ModelName]).Models;
            if (models.Count == 0) return null;

            return ((BfresModelAsset)models[0]).ModelData.Skeleton;
        }

        public void Reload(SkeletalAnim anim)
        {
            SkeletalAnim = anim;
            Name = anim.Name;
            FrameCount = anim.FrameCount;
            FrameRate = 60.0f;
            Loop = anim.Loop;

            AnimGroups.Clear();
            foreach (var boneAnim in anim.BoneAnims)
            {
                var group = new BoneAnimGroup();
                AnimGroups.Add(group);

                group.Name = boneAnim.Name;
                if (anim.FlagsRotate == SkeletalAnimFlagsRotate.Quaternion)
                    group.UseQuaternion = true;

                float scale = GLContext.PreviewScale;

                //Set the base data for the first set of keys if used
                if (boneAnim.FlagsBase.HasFlag(BoneAnimFlagsBase.Translate))
                {
                    group.Translate.X.KeyFrames.Add(new STKeyFrame(0, boneAnim.BaseData.Translate.X));
                    group.Translate.Y.KeyFrames.Add(new STKeyFrame(0, boneAnim.BaseData.Translate.Y));
                    group.Translate.Z.KeyFrames.Add(new STKeyFrame(0, boneAnim.BaseData.Translate.Z));
                }
                if (boneAnim.FlagsBase.HasFlag(BoneAnimFlagsBase.Rotate))
                {
                    group.Rotate.X.KeyFrames.Add(new STKeyFrame(0, boneAnim.BaseData.Rotate.X));
                    group.Rotate.Y.KeyFrames.Add(new STKeyFrame(0, boneAnim.BaseData.Rotate.Y));
                    group.Rotate.Z.KeyFrames.Add(new STKeyFrame(0, boneAnim.BaseData.Rotate.Z));
                    group.Rotate.W.KeyFrames.Add(new STKeyFrame(0, boneAnim.BaseData.Rotate.Z));
                }
                if (boneAnim.FlagsBase.HasFlag(BoneAnimFlagsBase.Scale))
                {
                    group.Scale.X.KeyFrames.Add(new STKeyFrame(0, boneAnim.BaseData.Scale.X));
                    group.Scale.Y.KeyFrames.Add(new STKeyFrame(0, boneAnim.BaseData.Scale.Y));
                    group.Scale.Z.KeyFrames.Add(new STKeyFrame(0, boneAnim.BaseData.Scale.Z));
                }

                if (boneAnim.ApplySegmentScaleCompensate)
                    group.UseSegmentScaleCompensate = true;

                //Generate keyed data from the curves
                foreach (var curve in boneAnim.Curves)
                {
                    switch ((TrackType)curve.AnimDataOffset)
                    {
                        case TrackType.XPOS: BfresAnimations.GenerateKeys(group.Translate.X, curve); break;
                        case TrackType.YPOS: BfresAnimations.GenerateKeys(group.Translate.Y, curve); break;
                        case TrackType.ZPOS: BfresAnimations.GenerateKeys(group.Translate.Z, curve); break;
                        case TrackType.XROT: BfresAnimations.GenerateKeys(group.Rotate.X, curve); break;
                        case TrackType.YROT: BfresAnimations.GenerateKeys(group.Rotate.Y, curve); break;
                        case TrackType.ZROT: BfresAnimations.GenerateKeys(group.Rotate.Z, curve); break;
                        case TrackType.WROT: BfresAnimations.GenerateKeys(group.Rotate.W, curve); break;
                        case TrackType.XSCA: BfresAnimations.GenerateKeys(group.Scale.X, curve); break;
                        case TrackType.YSCA: BfresAnimations.GenerateKeys(group.Scale.Y, curve); break;
                        case TrackType.ZSCA: BfresAnimations.GenerateKeys(group.Scale.Z, curve); break;
                    }
                }
            }
        }


        public override void NextFrame()
        {
            base.NextFrame();

            var skeletons = GetActiveSkeletons();
            if (skeletons == null) return;

            foreach (var skeleton in skeletons)
                skeleton.Updated = false;

            foreach (var skeleton in skeletons)
            {
                //Skeleton instance updated already (can update via attachment from bone)
                if (skeleton.Updated)
                    continue;

                bool update = false;

                foreach (var group in AnimGroups)
                {
                    if (group is BoneAnimGroup)
                    {
                        var boneAnim = (BoneAnimGroup)group;
                        STBone bone = skeleton.SearchBone(boneAnim.Name);
                        if (bone == null)
                            continue;

                        update = true;

                        Vector3 position = bone.Position;
                        Vector3 scale = bone.Scale;

                        if (boneAnim.Translate.X.HasKeys)
                            position.X = boneAnim.Translate.X.GetFrameValue(Frame) * GLContext.PreviewScale;
                        if (boneAnim.Translate.Y.HasKeys)
                            position.Y = boneAnim.Translate.Y.GetFrameValue(Frame) * GLContext.PreviewScale;
                        if (boneAnim.Translate.Z.HasKeys)
                            position.Z = boneAnim.Translate.Z.GetFrameValue(Frame) * GLContext.PreviewScale;

                        if (boneAnim.Scale.X.HasKeys)
                            scale.X = boneAnim.Scale.X.GetFrameValue(Frame);
                        if (boneAnim.Scale.Y.HasKeys)
                            scale.Y = boneAnim.Scale.Y.GetFrameValue(Frame);
                        if (boneAnim.Scale.Z.HasKeys)
                            scale.Z = boneAnim.Scale.Z.GetFrameValue(Frame);

                        bone.AnimationController.Position = position;
                        bone.AnimationController.Scale = scale;
                        bone.AnimationController.UseSegmentScaleCompensate = boneAnim.UseSegmentScaleCompensate;

                        if (boneAnim.UseQuaternion)
                        {
                            Quaternion rotation = bone.Rotation;

                            if (boneAnim.Rotate.X.HasKeys)
                                rotation.X = boneAnim.Rotate.X.GetFrameValue(Frame);
                            if (boneAnim.Rotate.Y.HasKeys)
                                rotation.Y = boneAnim.Rotate.Y.GetFrameValue(Frame);
                            if (boneAnim.Rotate.Z.HasKeys)
                                rotation.Z = boneAnim.Rotate.Z.GetFrameValue(Frame);
                            if (boneAnim.Rotate.W.HasKeys)
                                rotation.W = boneAnim.Rotate.W.GetFrameValue(Frame);

                            bone.AnimationController.Rotation = rotation;
                        }
                        else
                        {
                            Vector3 rotationEuluer = bone.EulerRotation;
                            if (boneAnim.Rotate.X.HasKeys)
                                rotationEuluer.X = boneAnim.Rotate.X.GetFrameValue(Frame);
                            if (boneAnim.Rotate.X.HasKeys)
                                rotationEuluer.Y = boneAnim.Rotate.Y.GetFrameValue(Frame);
                            if (boneAnim.Rotate.Z.HasKeys)
                                rotationEuluer.Z = boneAnim.Rotate.Z.GetFrameValue(Frame);

                            bone.AnimationController.EulerRotation = rotationEuluer;
                        }
                    }
                }

                if (update)
                    skeleton.Update();
            }
        }

        public class BoneAnimGroup : STAnimGroup
        {
            public Vector3Group Translate { get; set; }
            public Vector4Group Rotate { get; set; }
            public Vector3Group Scale { get; set; }

            public bool UseSegmentScaleCompensate { get; set; }

            public bool UseQuaternion = false;

            public BoneAnimGroup()
            {
                Translate = new Vector3Group() { Name = "Translate" };
                Rotate = new Vector4Group() { Name = "Rotate" };
                Scale = new Vector3Group() { Name = "Scale" };
                SubAnimGroups.Add(Translate);
                SubAnimGroups.Add(Rotate);
                SubAnimGroups.Add(Scale);
            }
        }

        public class Vector3Group : STAnimGroup
        {
            public BfresAnimationTrack X { get; set; }
            public BfresAnimationTrack Y { get; set; }
            public BfresAnimationTrack Z { get; set; }

            public Vector3Group()
            {
                X = new BfresAnimationTrack() { Name = "X", ChannelIndex = 0 };
                Y = new BfresAnimationTrack() { Name = "Y", ChannelIndex = 1 };
                Z = new BfresAnimationTrack() { Name = "Z", ChannelIndex = 2 };
            }

            public override List<STAnimationTrack> GetTracks()
            {
                List<STAnimationTrack> tracks = new List<STAnimationTrack>();
                tracks.Add(X);
                tracks.Add(Y);
                tracks.Add(Z);
                return tracks;
            }
        }

        public class Vector4Group : STAnimGroup
        {
            public BfresAnimationTrack X { get; set; }
            public BfresAnimationTrack Y { get; set; }
            public BfresAnimationTrack Z { get; set; }
            public BfresAnimationTrack W { get; set; }

            public Vector4Group()
            {
                X = new BfresAnimationTrack() { Name = "X", ChannelIndex = 0 };
                Y = new BfresAnimationTrack() { Name = "Y", ChannelIndex = 1 };
                Z = new BfresAnimationTrack() { Name = "Z", ChannelIndex = 2 };
                W = new BfresAnimationTrack() { Name = "W", ChannelIndex = 3 };
            }

            public override List<STAnimationTrack> GetTracks()
            {
                List<STAnimationTrack> tracks = new List<STAnimationTrack>();
                tracks.Add(X);
                tracks.Add(Y);
                tracks.Add(Z);
                return tracks;
            }
        }
    }
}
