using Ryujinx.Graphics.Shader;
using Ryujinx.Graphics.Shader.Translation;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace BfresEditor
{
    public class TegraShaderTranslator
    {
        private class GpuAccessor : IGpuAccessor
        {
            private readonly byte[] _data;

            public GpuAccessor(byte[] data)
            {
                _data = data;
            }

            public T MemoryRead<T>(ulong address) where T : unmanaged
            {
                return MemoryMarshal.Cast<byte, T>(new ReadOnlySpan<byte>(_data).Slice((int)address))[0];
            }
        }

        public static string Translate(byte[] data)
        {
            TranslationFlags flags = TranslationFlags.DebugMode;

            return Translator.CreateContext(0,
                 new GpuAccessor(data), flags).Translate(out _).Code;
        }
    }
}
