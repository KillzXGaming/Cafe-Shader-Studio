using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Syroot.NintenTools.NSW.Bntx;
using CafeStudio.UI;
using Toolbox.Core;

namespace BfresEditor
{
    public class BntxWrapper : IPropertyDisplay
    {
        public BntxFile BntxFile;

        public object PropertyDisplay => BntxFile;

        public BntxWrapper(BntxFile bntxFile) {
            BntxFile = bntxFile;
        }
    }
}
