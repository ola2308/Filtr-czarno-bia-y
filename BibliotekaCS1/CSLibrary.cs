using System;
using System.Runtime.CompilerServices;

namespace BibliotekaCS1
{
    public static class CSLibrary
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GrayscaleFilter(byte[] inputBuffer, byte[] outputBuffer, int pixelCount, float brightness)
        {
            // Wagi dla przekształcenia RGB na skalę szarości
            float weightR = 0.299f * brightness;
            float weightG = 0.587f * brightness;
            float weightB = 0.114f * brightness;

            // Przetwarzanie 4 pikseli na raz
            int blocksOf4 = pixelCount >> 2;
            int i = 0;

            for (int block = 0; block < blocksOf4; block++)
            {
                for (int pixel = 0; pixel < 4; pixel++)
                {
                    int offset = i * 3;
                    byte b = inputBuffer[offset];
                    byte g = inputBuffer[offset + 1];
                    byte r = inputBuffer[offset + 2];

                    // Obliczanie wartości szarości
                    byte gray = (byte)(r * weightR + g * weightG + b * weightB);

                    // Zapisanie wartości do bufora wyjściowego
                    outputBuffer[offset] = gray;
                    outputBuffer[offset + 1] = gray;
                    outputBuffer[offset + 2] = gray;

                    i++;
                }
            }

            // Przetwarzanie pozostałych pikseli
            for (; i < pixelCount; i++)
            {
                int offset = i * 3;
                byte b = inputBuffer[offset];
                byte g = inputBuffer[offset + 1];
                byte r = inputBuffer[offset + 2];

                // Obliczanie wartości szarości
                byte gray = (byte)(r * weightR + g * weightG + b * weightB);

                // Zapisanie wartości do bufora wyjściowego
                outputBuffer[offset] = gray;
                outputBuffer[offset + 1] = gray;
                outputBuffer[offset + 2] = gray;
            }
        }
    }
}