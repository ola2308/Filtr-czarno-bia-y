using System.Runtime.InteropServices;

namespace Filtr_czarno_biały
{
    public class NativeMethods
    {
        [DllImport(@"C:\Users\olani\source\repos\Filtr czarno-biały\x64\Debug\ASM.dll",
                   CallingConvention = CallingConvention.Cdecl)]
        public static extern void GrayscaleFilter(
            [In] byte[] inputBuffer,
            [Out] byte[] outputBuffer,
            int pixelCount,
            float brightness = 1.0f);

        // Stałe dla operacji SIMD
        public const int BytesPerPixel = 3;      // Format RGB
        public const int SIMDAlignment = 16;     // Wyrównanie dla operacji SIMD
        public const int PixelsPerBlock = 8;     // Liczba pikseli przetwarzanych w jednym bloku SIMD
    }
}