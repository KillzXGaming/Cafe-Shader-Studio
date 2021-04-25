using System;
using System.Collections.Generic;
using System.Text;

namespace RedStarLibrary
{
    public class InitModel : PropertyBase
    {
        public InitModel(dynamic bymlNode)
        {
            if (bymlNode is Dictionary<string, dynamic>) Prop = (Dictionary<string, dynamic>)bymlNode;
        }

        /// <summary>
        /// The external animation file of the model.
        /// </summary>
        public string ExternalAnimationFile
        {
            get { return this["AnimArc"]; }
            set { this["AnimArc"] = value; }
        }

        /// <summary>
        /// The external texture file of the model.
        /// </summary>
        public string ExternalTextureFile
        {
            get { return this["TextureArc"]; }
            set { this["TextureArc"] = value; }
        }

        /// <summary>
        /// The max amount of animations that can be blended.
        /// </summary>
        public int BlendAnimMax
        {
            get { return this["BlendAnimMax"]; }
            set { this["BlendAnimMax"] = value; }
        }
    }
}
