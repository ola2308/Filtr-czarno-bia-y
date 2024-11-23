using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Filtr_czarno_biały
{
    public class ImageProcessor
    {
        // Metoda do przetwarzania obrazu z podanymi parametrami (wielowątkowo)
        public async Task<ProcessingResult> ProcessImageWithParamsAsync(byte[] inputBuffer, int threadCount, float brightness, int pixelCount)
        {
            return await Task.Run(() =>
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();

                // Podziel piksele na wątki i rozpoczęcie przetwarzania
                int pixelsPerThread = pixelCount / threadCount;
                int remainingPixels = pixelCount % threadCount;
                byte[] outputBuffer = new byte[inputBuffer.Length];

                // Przetwarzanie wielowątkowe
                var tasks = new Task[threadCount];
                var taskTimes = new List<long>();

                for (int i = 0; i < threadCount; i++)
                {
                    int threadIndex = i;
                    tasks[i] = Task.Run(() =>
                    {
                        var taskWatch = System.Diagnostics.Stopwatch.StartNew();

                        int startPixel = threadIndex * pixelsPerThread;
                        int pixelsForThread = (threadIndex == threadCount - 1)
                            ? pixelsPerThread + remainingPixels
                            : pixelsPerThread;

                        int startOffset = startPixel * 3; // 3 bajty na piksel (RGB)
                        int endOffset = startOffset + (pixelsForThread * 3);

                        Console.WriteLine($"Thread {threadIndex}: processing pixels from {startPixel} to {startPixel + pixelsForThread - 1}");

                        byte[] inputSegment = new byte[endOffset - startOffset];
                        Array.Copy(inputBuffer, startOffset, inputSegment, 0, inputSegment.Length);

                        byte[] outputSegment = new byte[endOffset - startOffset];

                        // Wywołaj funkcję asemblerową dla segmentu obrazu, można zmienić na CSMethods
                        NativeMethods.GrayscaleFilter(
                            inputSegment,
                            outputSegment,
                            pixelsForThread,
                            brightness);

                        // Przepisz dane wyjściowe do odpowiedniej części bufora
                        Array.Copy(outputSegment, 0, outputBuffer, startOffset, outputSegment.Length);

                        taskWatch.Stop();
                        taskTimes.Add(taskWatch.ElapsedMilliseconds);

                        Console.WriteLine($"Thread {threadIndex} finished in {taskWatch.ElapsedMilliseconds} ms");
                    });
                }

                // Poczekaj na zakończenie wszystkich wątków
                Task.WaitAll(tasks);
                watch.Stop();

                // Oblicz statystyki wydajności
                float timeInMs = watch.ElapsedMilliseconds;
                float averageTaskTime = taskTimes.Count > 0 ? (float)taskTimes.Average() : 0;
                float pixelsPerMs = pixelCount / timeInMs;
                float speedup = 1.0f;
                float threadEfficiency = 1.0f;

                if (threadCount > 1)
                {
                    speedup = (float)taskTimes.Min() / timeInMs;
                    threadEfficiency = speedup / threadCount;
                }

                return new ProcessingResult
                {
                    ExecutionTime = watch.ElapsedMilliseconds,
                    ThreadCount = threadCount,
                    OutputBuffer = outputBuffer,
                    ProcessingDetails = new ProcessingDetails
                    {
                        PixelsPerThread = pixelsPerThread,
                        RemainingPixels = remainingPixels,
                        TotalThreads = threadCount,
                        AverageTimePerPixel = averageTaskTime / pixelCount,
                        PixelsPerMillisecond = pixelsPerMs,
                        ThreadEfficiency = threadEfficiency,
                        Speedup = speedup
                    }
                };
            });
        }

        // Metoda do przetwarzania obrazu przy użyciu C#
        public long ProcessImageCS(byte[] inputBuffer, int pixelCount, int threadCount, float brightness)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            // Przygotuj bufor wyjściowy
            byte[] outputBuffer = new byte[inputBuffer.Length];

            // Przetwarzanie przy użyciu C# (bez asemblera)
            CSMethods.GrayscaleFilter(inputBuffer, outputBuffer, pixelCount, brightness);

            watch.Stop();
            Console.WriteLine($"Processing using C# finished in {watch.ElapsedMilliseconds} ms");
            return watch.ElapsedMilliseconds;
        }
    }
}
