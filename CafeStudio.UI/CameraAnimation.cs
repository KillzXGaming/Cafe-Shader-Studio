using System;
using System.Collections.Generic;
using System.Text;
using Toolbox.Core.Animations;
using GLFrameworkEngine;

namespace CafeStudio.UI
{
    public class CameraAnimation : STAnimation
    {
        public override void NextFrame() {
            NextFrame(GLContext.ActiveContext);
        }

        public virtual void NextFrame(GLContext context)
        {

        }
    }
}
