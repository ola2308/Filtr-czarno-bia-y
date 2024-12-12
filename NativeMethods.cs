using System.Runtime.InteropServices;

namespace Filtr_czarno_biały
{
    public class NativeMethods
    {
        [DllImport(@"C:\Users\olani\source\repos\Filtr-czarno-bia-y\x64\Debug\ASM.dll",
                   CallingConvention = CallingConvention.Cdecl)]
        public static extern void GrayscaleFilter(
            [In] byte[] inputBuffer,
            [Out] byte[] outputBuffer,
            int pixelCount,
            float brightness = 1.0f);
    }
}