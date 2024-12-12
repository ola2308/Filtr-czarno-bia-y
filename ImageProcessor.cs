using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BibliotekaCS1;

namespace Filtr_czarno_biały
{
    public class ImageProcessor
    {
        public async Task<ProcessingResult> ProcessImageWithParamsAsync(
            byte[] inputBuffer,
            int threadCount,
            float brightness,
            int pixelCount,
            bool useASM = true)
        {
            return await Task.Run(() =>
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();

                // Oblicz liczbę pikseli na wątek
                int pixelsPerThread = pixelCount / threadCount;
                int remainingPixels = pixelCount % threadCount; // Pozostałe piksele dla ostatniego wątku

                byte[] outputBuffer = new byte[inputBuffer.Length];
                var tasks = new Task[threadCount];

                for (int i = 0; i < threadCount; i++)
                {
                    int threadIndex = i;
                    int startPixel = threadIndex * pixelsPerThread;

                    // Ostatni wątek przetwarza wszystkie pozostałe piksele
                    int pixelsToProcess = (threadIndex == threadCount - 1)
                        ? pixelsPerThread + remainingPixels
                        : pixelsPerThread;

                    tasks[i] = Task.Run(() =>
                    {
                        int startOffset = startPixel * 3;
                        int length = pixelsToProcess * 3;

                        if (length <= 0 || length > int.MaxValue / 3)
                        {
                            throw new ArgumentException("Nieprawidłowy rozmiar bufora");
                        }

                        // Stwórz bufory tymczasowe
                        byte[] threadInput = new byte[length];
                        byte[] threadOutput = new byte[length];

                        // Skopiuj dane do przetworzenia
                        Buffer.BlockCopy(inputBuffer, startOffset, threadInput, 0, length);

                        if (useASM)
                        {
                            NativeMethods.GrayscaleFilter(threadInput, threadOutput, pixelsToProcess, brightness);
                        }
                        else
                        {
                            CSLibrary.GrayscaleFilter(threadInput, threadOutput, pixelsToProcess, brightness);
                        }

                        // Skopiuj wyniki
                        Buffer.BlockCopy(threadOutput, 0, outputBuffer, startOffset, length);
                    });
                }

                Task.WaitAll(tasks);
                watch.Stop();

                return new ProcessingResult
                {
                    ExecutionTime = watch.ElapsedMilliseconds,
                    ThreadCount = threadCount,
                    OutputBuffer = outputBuffer,
                    ProcessingDetails = new ProcessingDetails
                    {
                        PixelsPerThread = pixelsPerThread,
                        TotalThreads = threadCount,
                        PixelsPerMillisecond = pixelCount / (float)watch.ElapsedMilliseconds
                    }
                };
            });
        }

        public long ProcessImageCS(byte[] inputBuffer, int pixelCount, int threadCount, float brightness)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            byte[] outputBuffer = new byte[inputBuffer.Length];

            if (threadCount > 1)
            {
                Parallel.For(0, threadCount, i =>
                {
                    int startPixel = i * (pixelCount / threadCount);
                    int endPixel = (i == threadCount - 1) ? pixelCount : (i + 1) * (pixelCount / threadCount);
                    int startOffset = startPixel * 3;
                    int length = (endPixel - startPixel) * 3;

                    byte[] segmentInput = new byte[length];
                    byte[] segmentOutput = new byte[length];

                    Buffer.BlockCopy(inputBuffer, startOffset, segmentInput, 0, length);
                    CSLibrary.GrayscaleFilter(segmentInput, segmentOutput, endPixel - startPixel, brightness);
                    Buffer.BlockCopy(segmentOutput, 0, outputBuffer, startOffset, length);
                });
            }
            else
            {
                CSLibrary.GrayscaleFilter(inputBuffer, outputBuffer, pixelCount, brightness);
            }

            watch.Stop();
            return watch.ElapsedMilliseconds;
        }
    }
}