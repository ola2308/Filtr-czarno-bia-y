using System;
using System.Threading.Tasks;

namespace Filtr_czarno_biały
{
    internal class CSMethods
    {
        // Metoda do zastosowania filtra czarno-białego z uwzględnieniem jasności
        public static void GrayscaleFilter(byte[] inputBuffer, byte[] outputBuffer, int pixelCount, float brightness)
        {
            // Wagi dla przekształcenia RGB na skalę szarości, z uwzględnieniem jasności
            float weightR = 0.299f * brightness;
            float weightG = 0.587f * brightness;
            float weightB = 0.114f * brightness;

            Parallel.For(0, pixelCount, i =>
            {
                int offset = i * 3;

                // Pobieramy wartości RGB
                byte b = inputBuffer[offset];
                byte g = inputBuffer[offset + 1];
                byte r = inputBuffer[offset + 2];

                // Obliczanie wartości szarości
                float grayValue = r * weightR + g * weightG + b * weightB;

                // Upewniamy się, że wartość mieści się w zakresie [0, 255]
                byte gray = (byte)Clamp((int)grayValue, 0, 255);

                // Przypisujemy wartość szarości do wszystkich kanałów RGB
                outputBuffer[offset] = gray;
                outputBuffer[offset + 1] = gray;
                outputBuffer[offset + 2] = gray;
            });
        }

        // Metoda pomocnicza do zapewnienia, że wartość jest w zakresie [min, max]
        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
