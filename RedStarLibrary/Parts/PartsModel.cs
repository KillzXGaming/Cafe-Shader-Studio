using System;
using System.Collections.Generic;
using System.Numerics;

namespace RedStarLibrary
{
    public class PartsModel : PropertyBase
    {
        public PartsModel(dynamic bymlNode)
        {
            if (bymlNode is Dictionary<string, dynamic>) Prop = (Dictionary<string, dynamic>)bymlNode;
            else throw new Exception("Not a dictionary");
        }

        /// <summary>
        /// The bone joint to bind the part to.
        /// </summary>
        public string JointName
        {
            get { return this["JointName"]; }
            set { this["JointName"] = value; }
        }

        /// <summary>
        /// The local rotation relative to the bone joint in eluer degrees.
        /// </summary>
        public Vector3 LocalRotate
        {
            get { return GetVector3("LocalRotate"); }
            set  { SetVector3("LocalRotate", value); }
        }

        /// <summary>
        /// The local scale relative to the bone joint.
        /// </summary>
        public Vector3 LocalScale
        {
            get { return GetVector3("LocalScale", 1.0f); }
            set { SetVector3("LocalScale", value); }
        }

        /// <summary>
        /// The local translation relative to the bone joint.
        /// </summary>
        public Vector3 LocalTranslate
        {
            get { return GetVector3("LocalTrans"); }
            set { SetVector3("LocalTrans", value); }
        }

        /// <summary>
        /// Determines to follow the scale matrix of the part model.
        /// </summary>
        public bool UseFollowMtxScale
        {
            get { return this["UseFollowMtxScale"]; }
            set { this["UseFollowMtxScale"] = value; }
        }

        /// <summary>
        /// Determines to use the local scale property.
        /// </summary>
        public bool UseLocalScale
        {
            get { return this["UseLocalScale"]; }
            set { this["UseLocalScale"] = value; }
        }
    }
}
