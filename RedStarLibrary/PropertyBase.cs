using System;
using System.Collections.Generic;
using System.Numerics;

namespace RedStarLibrary
{
    public class PropertyBase
    {
        public Dictionary<string, dynamic> Prop { get; set; } = new Dictionary<string, dynamic>();

        /// <summary>
        /// Gets or sets the dictionary of properties stored for this class.
        /// </summary>
        /// <returns></returns>
        public dynamic this[string name]
        {
            get
            {
                if (Prop.ContainsKey(name)) return Prop[name];
                else return null;
            }
            set
            {
                if (Prop.ContainsKey(name)) Prop[name] = value;
                else Prop.Add(name, value);
            }
        }

        /// <summary>
        /// Gets a common Vector3 type from a dictionary of XYZ keys.
        /// </summary>
        /// <returns></returns>
        public Vector3 GetVector3(string key, float defaultValue = 0)
        {
            if (this[key] == null)
                return new Vector3(defaultValue);

            return new Vector3(
              this[key]["X"] != null ? this[key]["X"] : defaultValue,
              this[key]["Y"] != null ? this[key]["Y"] : defaultValue,
              this[key]["Z"] != null ? this[key]["Z"] : defaultValue);
        }

        /// <summary>
        /// Sets a common Vector3 type from a dictionary of XYZ keys.
        /// </summary>
        /// <returns></returns>
        public void SetVector3(string key, Vector3 value)
        {
            if (this[key] == null)
            {
                Dictionary<string, dynamic> rotate = new Dictionary<string, dynamic>();
                rotate.Add("X", (float)0.0f);
                rotate.Add("Y", (float)0.0f);
                rotate.Add("Z", (float)0.0f);
                Prop.Add(key, rotate);
            }
            this[key]["X"] = value.X;
            this[key]["Y"] = value.Y;
            this[key]["Z"] = value.Z;
        }
    }
}
