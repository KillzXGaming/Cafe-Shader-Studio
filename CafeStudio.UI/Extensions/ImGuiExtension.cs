using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;

namespace CafeStudio.UI
{
    public static class ImGuiExtension
    {
        public static unsafe bool IsValid(this ImGuiPayloadPtr payload) {
            return payload.NativePtr != null;
        }
    }
}
