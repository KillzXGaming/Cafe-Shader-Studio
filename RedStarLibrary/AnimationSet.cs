using System;
using System.Collections.Generic;
using System.Text;
using Toolbox.Core.Animations;
using Toolbox.Core;

namespace RedStarLibrary
{
    public class AnimationSet : IAnimationContainer
    {
        /// <summary>
        /// The set of animations used to batch play multiple animation files at once.
        /// </summary>
        public List<STAnimation> Animations = new List<STAnimation>();

        public IEnumerable<STAnimation> AnimationList => Animations;
    }
}
