using System;
using System.Collections.Generic;
using System.Text;

namespace RedStarLibrary
{
    public class ActorCreate : PropertyBase
    {
        public ActorCreate(dynamic bymlNode) 
        {
            if (bymlNode is Dictionary<string, dynamic>) Prop = (Dictionary<string, dynamic>)bymlNode;
            else throw new Exception("Not a dictionary");
        }

        /// <summary>
        /// The class the actor belongs to.
        /// </summary>
        public string ClassName
        {
            get { return this["ClassName"]; }
            set { this["ClassName"] = value; }
        }

        /// <summary>
        /// The suffix for part fix files.
        /// </summary>
        public string FixFileSuffixName
        {
            get { return this["FixFileSuffixName"]; }
            set { this["FixFileSuffixName"] = value; }
        }

        /// <summary>
        /// The suffix for init files.
        /// </summary>
        public string InitFileSuffixName
        {
            get { return this["InitFileSuffixName"]; }
            set { this["InitFileSuffixName"] = value; }
        }

        /// <summary>
        /// Determines if the actor is currently alive.
        /// </summary>
        public bool IsAlive
        {
            get { return this["IsAlive"]; }
            set { this["IsAlive"] = value; }
        }

        /// <summary>
        /// Determines to sync appearing.
        /// </summary>
        public bool IsSyncAppear
        {
            get { return this["IsSyncAppear"]; }
            set { this["IsSyncAppear"] = value; }
        }

        /// <summary>
        /// Determines to sync clipping.
        /// </summary>
        public bool IsSyncClipping
        {
            get { return this["IsSyncClipping"]; }
            set { this["IsSyncClipping"] = value; }
        }

        /// <summary>
        /// The model file name of the actor.
        /// </summary>
        public string ModelName
        {
            get { return this["ModelName"]; }
            set { this["ModelName"] = value; }
        }

        /// <summary>
        /// The object name of the actor.
        /// </summary>
        public string ObjectName
        {
            get { return this["ObjectName"]; }
            set { this["ObjectName"] = value; }
        }
    }
}
