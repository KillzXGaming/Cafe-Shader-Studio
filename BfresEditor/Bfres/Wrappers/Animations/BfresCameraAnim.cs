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
using CafeStudio.UI;

namespace BfresEditor
{
    public class BfresCameraAnim : CameraAnimation
    {
        public BfresCameraAnim(CameraAnim anim)
        {
            CanPlay = false; //Default all animations to not play unless toggled in list
            Reload(anim);
        }

        public override void NextFrame(GLContext context)
        {
            var group = AnimGroups[0] as CameraAnimGroup;
            var camera = context.Camera;

            var posX = group.PositionX.GetFrameValue(this.Frame);
            var posY = group.PositionY.GetFrameValue(this.Frame);
            var posZ = group.PositionZ.GetFrameValue(this.Frame);
            var rotX = group.RotationX.GetFrameValue(this.Frame);
            var rotY = group.RotationY.GetFrameValue(this.Frame);
            var rotZ = group.RotationZ.GetFrameValue(this.Frame);
            var twist = group.Twist.GetFrameValue(this.Frame);
            var near = group.ClipNear.GetFrameValue(this.Frame);
            var far = group.ClipFar.GetFrameValue(this.Frame);
            var fov = group.FieldOfView.GetFrameValue(this.Frame);
            var aspect = group.AspectRatio.GetFrameValue(this.Frame);

            camera.SetKeyframe(CameraAnimationKeys.PositionX, posX);
            camera.SetKeyframe(CameraAnimationKeys.PositionY, posY);
            camera.SetKeyframe(CameraAnimationKeys.PositionZ, posZ);
            camera.SetKeyframe(CameraAnimationKeys.Near, near);
            camera.SetKeyframe(CameraAnimationKeys.Far, far);
            camera.SetKeyframe(CameraAnimationKeys.FieldOfView, fov);
            camera.SetKeyframe(CameraAnimationKeys.Distance, 0);
            camera.RotationLookat = group.IsLookat;

            if (group.IsLookat)
            {
                camera.SetKeyframe(CameraAnimationKeys.RotationX, 0);
                camera.SetKeyframe(CameraAnimationKeys.RotationY, 0);
                camera.SetKeyframe(CameraAnimationKeys.RotationZ, 0);
                camera.SetKeyframe(CameraAnimationKeys.Twist, twist);
                camera.SetKeyframe(CameraAnimationKeys.EyeX, rotX);
                camera.SetKeyframe(CameraAnimationKeys.EyeY, rotY);
                camera.SetKeyframe(CameraAnimationKeys.EyeZ, rotZ);
            }
            else
            {
                camera.SetKeyframe(CameraAnimationKeys.RotationX, rotX);
                camera.SetKeyframe(CameraAnimationKeys.RotationY, rotY);
                camera.SetKeyframe(CameraAnimationKeys.RotationZ, rotZ);
                camera.SetKeyframe(CameraAnimationKeys.Twist, 0);
                camera.SetKeyframe(CameraAnimationKeys.EyeX, 0);
                camera.SetKeyframe(CameraAnimationKeys.EyeY, 0);
                camera.SetKeyframe(CameraAnimationKeys.EyeZ, 0);
            }

            camera.UpdateMatrices();
        }

        public void Reload(CameraAnim anim)
        {
            Name = anim.Name;
            FrameCount = anim.FrameCount;
            FrameRate = 60.0f;
            Loop = anim.Flags.HasFlag(CameraAnimFlags.Looping);

            CameraAnimGroup group = new CameraAnimGroup();
            group.Name = anim.Name;
            group.IsOrtho = !anim.Flags.HasFlag(CameraAnimFlags.Perspective);
            group.IsLookat = !anim.Flags.HasFlag(CameraAnimFlags.EulerZXY);

            group.PositionX.KeyFrames.Add(new STKeyFrame(0, anim.BaseData.Position.X));
            group.PositionY.KeyFrames.Add(new STKeyFrame(0, anim.BaseData.Position.Y));
            group.PositionZ.KeyFrames.Add(new STKeyFrame(0, anim.BaseData.Position.Z));
            group.RotationX.KeyFrames.Add(new STKeyFrame(0, anim.BaseData.Rotation.X));
            group.RotationY.KeyFrames.Add(new STKeyFrame(0, anim.BaseData.Rotation.Y));
            group.RotationZ.KeyFrames.Add(new STKeyFrame(0, anim.BaseData.Rotation.Z));
            group.Twist.KeyFrames.Add(new STKeyFrame(0, anim.BaseData.Twist));
            group.ClipNear.KeyFrames.Add(new STKeyFrame(0, anim.BaseData.ClipNear));
            group.ClipFar.KeyFrames.Add(new STKeyFrame(0, anim.BaseData.ClipFar));
            group.AspectRatio.KeyFrames.Add(new STKeyFrame(0, anim.BaseData.AspectRatio));
            group.FieldOfView.KeyFrames.Add(new STKeyFrame(0, anim.BaseData.FieldOfView));

            AnimGroups.Clear();
            AnimGroups.Add(group);
            for (int i = 0; i < anim.Curves.Count; i++)
            {
                var curve = anim.Curves[i];
                switch ((CameraAnimDataOffset)curve.AnimDataOffset)
                {
                    case CameraAnimDataOffset.PositionX: BfresAnimations.GenerateKeys(group.PositionX, curve); break;
                    case CameraAnimDataOffset.PositionY: BfresAnimations.GenerateKeys(group.PositionY, curve); break;
                    case CameraAnimDataOffset.PositionZ: BfresAnimations.GenerateKeys(group.PositionZ, curve); break;
                    case CameraAnimDataOffset.RotationX: BfresAnimations.GenerateKeys(group.RotationX, curve); break;
                    case CameraAnimDataOffset.RotationY: BfresAnimations.GenerateKeys(group.RotationY, curve); break;
                    case CameraAnimDataOffset.RotationZ: BfresAnimations.GenerateKeys(group.RotationZ, curve); break;
                    case CameraAnimDataOffset.Twist: BfresAnimations.GenerateKeys(group.Twist, curve); break;
                    case CameraAnimDataOffset.ClipNear: BfresAnimations.GenerateKeys(group.ClipNear, curve); break;
                    case CameraAnimDataOffset.ClipFar: BfresAnimations.GenerateKeys(group.ClipFar, curve); break;
                    case CameraAnimDataOffset.AspectRatio: BfresAnimations.GenerateKeys(group.AspectRatio, curve); break;
                    case CameraAnimDataOffset.FieldOFView: BfresAnimations.GenerateKeys(group.FieldOfView, curve); break;
                }
            }
        }

        public class CameraAnimGroup : STAnimGroup
        {
            public BfresAnimationTrack ClipNear = new BfresAnimationTrack();
            public BfresAnimationTrack ClipFar = new BfresAnimationTrack();
            public BfresAnimationTrack AspectRatio = new BfresAnimationTrack();
            public BfresAnimationTrack FieldOfView = new BfresAnimationTrack();
            public BfresAnimationTrack PositionX = new BfresAnimationTrack();
            public BfresAnimationTrack PositionY = new BfresAnimationTrack();
            public BfresAnimationTrack PositionZ = new BfresAnimationTrack();

            public BfresAnimationTrack RotationX = new BfresAnimationTrack();
            public BfresAnimationTrack RotationY = new BfresAnimationTrack();
            public BfresAnimationTrack RotationZ = new BfresAnimationTrack();

            public BfresAnimationTrack Twist = new BfresAnimationTrack();

            public bool IsLookat = false;
            public bool IsOrtho = false;
        }
    }
}
