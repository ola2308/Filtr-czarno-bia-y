using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Filtr_czarno_biały
{
    internal class CSMethods
    {
        public static void GrayscaleFilter(byte[] inputBuffer, byte[] outputBuffer, int pixelCount, float brightness = 1.0f)
        {
            // Wagi dla przekształcenia RGB na skalę szarości
            float weightR = 0.299f * brightness;
            float weightG = 0.587f * brightness;
            float weightB = 0.114f * brightness;

            Parallel.For(0, pixelCount, i =>
            {
                int offset = i * 3;
                byte r = inputBuffer[offset + 2];
                byte g = inputBuffer[offset + 1];
                byte b = inputBuffer[offset];

                // Oblicz wartość szarości
                float gray = r * weightR + g * weightG + b * weightB;

                // Upewnij się, że wartość mieści się w przedziale [0, 255]
                byte grayByte = (byte)Clamp((int)gray, 0, 255);

                // Zapisz wartość szarości do wszystkich trzech kanałów (RGB)
                outputBuffer[offset] = grayByte;
                outputBuffer[offset + 1] = grayByte;
                outputBuffer[offset + 2] = grayByte;
            });
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
