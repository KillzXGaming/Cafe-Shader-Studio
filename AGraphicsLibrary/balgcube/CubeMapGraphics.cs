using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AampLibraryCSharp;

namespace AGraphicsLibrary
{
    public class CubeMapGraphics
    {
        public List<CubeMapObject> CubeMapObjects = new List<CubeMapObject>();

        public CubeMapGraphics()
        {

        }

        public CubeMapObject GetCubeMapObject(string name)
        {
            for (int i = 0; i < CubeMapObjects.Count; i++)
            {
                if (CubeMapObjects[i].CubeMapUint.Name == name)
                    return CubeMapObjects[i];
            }
            return CubeMapObjects.FirstOrDefault();
        }

        public CubeMapGraphics(AampFile aamp)
        {
            foreach (var obj in aamp.RootNode.childParams)
                CubeMapObjects.Add(new CubeMapObject(obj));
        }
    }
}
