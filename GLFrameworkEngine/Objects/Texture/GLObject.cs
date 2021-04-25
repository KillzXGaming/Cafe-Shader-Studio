using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLFrameworkEngine
{
    public class GLObject
    {
        public int ID { get; private set; } = -1;

        public GLObject(int id)
        {
            ID = id;
        }
    }
}
