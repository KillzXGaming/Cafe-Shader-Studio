using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CafeStudio.UI
{
    public interface IPropertyUI
    {
        Type GetTypeUI();
        void OnLoadUI(object uiInstance);
        void OnRenderUI(object uiInstance);
    }
}
