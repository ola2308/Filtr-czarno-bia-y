using System;
using System.Runtime.CompilerServices;

namespace BibliotekaCS1
{
    public static class CSLibrary
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GrayscaleFilter(byte[] inputBuffer, byte[] outputBuffer, int pixelCount, float brightness)
        {
            float weightR = 0.299f * brightness;
            float weightG = 0.587f * brightness;
            float weightB = 0.114f * brightness;

            int blocksOf4 = pixelCount >> 2;

            for (int block = 0; block < blocksOf4; block++)
            {
                int baseOffset = block * 12;

                for (int j = 0; j < 12; j += 3)
                {
                    int offset = baseOffset + j;
                    byte b = inputBuffer[offset];
                    byte g = inputBuffer[offset + 1];
                    byte r = inputBuffer[offset + 2];

                    float grayValue = r * weightR + g * weightG + b * weightB + 0.5f;

                    if (grayValue < 0f) grayValue = 0f;
                    else if (grayValue > 255f) grayValue = 255f;

                    byte gray = (byte)grayValue;

                    outputBuffer[offset] = gray;
                    outputBuffer[offset + 1] = gray;
                    outputBuffer[offset + 2] = gray;
                }
            }

            // Pozostałe piksele
            int remainingOffset = blocksOf4 * 12;
            for (int i = remainingOffset; i < pixelCount * 3; i += 3)
            {
                byte b = inputBuffer[i];
                byte g = inputBuffer[i + 1];
                byte r = inputBuffer[i + 2];

                float grayValue = r * weightR + g * weightG + b * weightB + 0.5f;

                if (grayValue < 0f) grayValue = 0f;
                else if (grayValue > 255f) grayValue = 255f;

                byte gray = (byte)grayValue;

                outputBuffer[i] = gray;
                outputBuffer[i + 1] = gray;
                outputBuffer[i + 2] = gray;
            }
        }
    }
}
