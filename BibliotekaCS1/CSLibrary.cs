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
            int blocksOf4 = pixelCount >> 2;  // dzielenie przez 4

            for (int block = 0; block < blocksOf4; block++)
            {
                int baseOffset = block * 12;  // 4 piksele * 3 bajty

                // Przetwarzanie 4 pikseli jednocześnie
                for (int j = 0; j < 12; j += 3)
                {
                    int offset = baseOffset + j;
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

            // Przetwarzanie pozostałych pikseli
            int remainingOffset = blocksOf4 * 12;
            for (int i = remainingOffset; i < pixelCount * 3; i += 3)
            {
                byte b = inputBuffer[i];
                byte g = inputBuffer[i + 1];
                byte r = inputBuffer[i + 2];

                // Obliczanie wartości szarości
                byte gray = (byte)(r * weightR + g * weightG + b * weightB);

                // Zapisanie wartości do bufora wyjściowego
                outputBuffer[i] = gray;
                outputBuffer[i + 1] = gray;
                outputBuffer[i + 2] = gray;
            }
        }
    }
}