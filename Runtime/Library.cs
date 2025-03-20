using System.Runtime.InteropServices;

namespace Elfenlabs.Text
{
    public static class FontLibrary
    {
        [DllImport("fontlib", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Initialize();
        [DllImport("fontlib", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Shutdown();
        [DllImport("fontlib", CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe int LoadFont(byte[] fontData, int fontDataSize);
        [DllImport("fontlib", CallingConvention = CallingConvention.Cdecl)]
        public static extern void UnloadFont(int fontIndex);
    }
}