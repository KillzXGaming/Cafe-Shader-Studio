using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Toolbox.Core.Animations;
using System.Diagnostics;

namespace CafeStudio.UI
{
    public class AnimationPlayer
    {
        private Stopwatch stopWatch;

        System.Timers.Timer animationTimer;

        public AnimPlayerState AnimationPlayerState = AnimPlayerState.Stop;

        public bool IsPlaying => AnimationPlayerState == AnimPlayerState.Playing;
        public bool IsLooping { get; set; } = true;

        public float StartFrame { get; set; } = 0;
        public float FrameCount { get; set; } = 1;

        public float FrameRate = 60.0f;

        public bool ForceLoop { get; set; } = true;

        public EventHandler OnFrameChanged;

        public float CurrentFrame;

        public List<STAnimation> CurrentAnimations = new List<STAnimation>();

        public AnimationPlayer()
        {
            stopWatch = new Stopwatch();
            stopWatch.Start();

            animationTimer = new System.Timers.Timer()
            {
                Interval = (int)(1000.0f / 60.0f),
            };
            animationTimer.Elapsed += timer_Tick;

            UpdateFramerate();
        }

        void timer_Tick(object sender, EventArgs e)
        {
            UpdateAnimationFrame();
        }

        public void UpdateFramerate()
        {
            animationTimer.Interval = (int)(1000.0f / FrameRate);
        }

        public bool HasAnimation(STAnimation animation) => CurrentAnimations.Contains(animation);

        public void AddAnimation(STAnimation animation, string groupName, bool reset = true)
        {
            if (animation == null || CurrentAnimations.Contains(animation))
                return;

            if (reset)
            {
                Reset();
                CurrentAnimations.Clear();
            }

            animation.CanPlay = true;

            FrameCount = animation.FrameCount;
            StartFrame = animation.StartFrame;
            CurrentFrame = StartFrame;

            groupName = System.IO.Path.GetFileNameWithoutExtension(groupName);

            CurrentAnimations.Add(animation);

            if (reset)
                SetAnimationsToFrame(0);
        }

        public void Reset(bool clearAnimations = true)
        {
            if (clearAnimations)
                CurrentAnimations.Clear();
            StartFrame = 0;
            CurrentFrame = 0;
            AnimationPlayerState = AnimPlayerState.Stop;

            ResetModels();
        }

        public void ResetModels()
        {
            foreach (var container in GLFrameworkEngine.DataCache.ModelCache.Values)
                container.ResetAnimations();
        }

        public void Play()
        {
            animationTimer.Start();
            AnimationPlayerState = AnimPlayerState.Playing;
        }

        public void Stop()
        {
            animationTimer.Stop();
            CurrentFrame = StartFrame;
            AnimationPlayerState = AnimPlayerState.Stop;
        }

        public void Pause()
        {
            animationTimer.Stop();
            AnimationPlayerState = AnimPlayerState.Stop;
        }

        bool disposed = false;

        public void OnControlClosing()
        {
            animationTimer.Stop();
            animationTimer.Dispose();

            disposed = true;
            AnimationPlayerState = AnimPlayerState.Stop;
            stopWatch.Stop();

            Reset();
            CurrentAnimations.Clear();
        }

        public void UpdateAnimationFrame()
        {
            if (disposed) return;

            if (CurrentFrame >= FrameCount - 1)
            {
                if (IsLooping)
                {
                    //Reset the min setting as animations can potentically be switched
                    CurrentFrame = StartFrame;
                }
                else
                    Stop();
            }
            else
            {
                if (CurrentFrame < FrameCount)
                    CurrentFrame += 1.0f;
            }
            OnFrameAdvanced();
        }

        private int timing = 0;

        private void OnFrameAdvanced()
        {
            OnFrameChanged?.Invoke(this, EventArgs.Empty);

            timing += (int)stopWatch.ElapsedMilliseconds;
            stopWatch.Reset();
            stopWatch.Start();

            //Update by interval
            if (timing > 16)
            {
                timing = timing % 16;
                SetFrame(CurrentFrame);
            }
        }

        public void SetFrame(float frame)
        {
            SetAnimationsToFrame(frame);
        }

        private void SetAnimationsToFrame(float frameNum)
        {
            CurrentFrame = frameNum;
            foreach (var anim in CurrentAnimations)
            {
                if (anim.Loop || ForceLoop)
                {
                    var lastFrame = anim.FrameCount;
                    while (frameNum > lastFrame)
                        frameNum -= lastFrame + 1;
                }

                // if (!anim.CanPlay || !anim.Loop && (frameNum < anim.StartFrame || frameNum > anim.FrameCount))
                //    continue;

                float animFrameNum = frameNum;

                anim.SetFrame(animFrameNum);
                anim.NextFrame();
            }
        }

        public enum AnimPlayerState
        {
            Playing,
            Pause,
            Stop,
        }

        public class AnimationGroup
        {
            public List<STAnimation> Animations = new List<STAnimation>();
        }
    }
}
