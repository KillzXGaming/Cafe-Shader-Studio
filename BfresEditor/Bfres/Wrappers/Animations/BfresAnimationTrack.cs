using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.Animations;
using BfresLibrary;

namespace BfresEditor
{
    public class BfresAnimationTrack : STAnimationTrack
    {
        public DWord Offset { get; set; }
        public float Scale { get; set; } = 1.0f;

        public void InsertKey(float frame, float value)
        {
            if (!KeyFrames.Any(x => x.Frame == frame))
                KeyFrames.Add(new STKeyFrame(frame, value));
        }

        private float GetWrapFrame(float frame)
        {
            var lastFrame = KeyFrames.Last().Frame;
            if (WrapMode == STLoopMode.Clamp)
            {
                if (frame > lastFrame)
                    return lastFrame;
                else
                    return frame;
            }
            else if (WrapMode == STLoopMode.Repeat)
            {
                while (frame > lastFrame)
                    frame -= lastFrame;
                return frame;
            }
            return frame;
        }
    }
}
