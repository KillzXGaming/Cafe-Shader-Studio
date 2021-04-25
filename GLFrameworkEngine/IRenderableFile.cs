using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLFrameworkEngine
{
    public interface IRenderableFile
    {
        GenericRenderer Renderer { get; set; }
    }
}
