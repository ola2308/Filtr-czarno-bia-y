using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using BibliotekaCS1;

namespace Filtr_czarno_biały
{
    public class ImageProcessor
    {
        private const int SIMD_ALIGNMENT = 32;
        private const int CACHE_LINE_SIZE = 64;

        public async Task<ProcessingResult> ProcessImageWithParamsAsync(
            byte[] inputBuffer,
            int threadCount,
            float brightness,
            int pixelCount)
        {
            return await Task.Run(() =>
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();

                // Obliczenie optymalnego rozmiaru bloku dla każdego wątku
                int optimalBlockSize = CalculateOptimalBlockSize(pixelCount, threadCount);
                int pixelsPerThread = (pixelCount / threadCount / optimalBlockSize) * optimalBlockSize;
                int remainingPixels = pixelCount - (pixelsPerThread * (threadCount - 1));

                // Alokacja bufora wyjściowego z wyrównaniem do granicy cache
                byte[] outputBuffer = new byte[inputBuffer.Length];
                GCHandle outputHandle = GCHandle.Alloc(outputBuffer, GCHandleType.Pinned);

                var tasks = new Task[threadCount];
                var taskTimes = new List<long>();
                var processingDetails = new ProcessingDetails
                {
                    PixelsPerThread = pixelsPerThread,
                    RemainingPixels = remainingPixels,
                    TotalThreads = threadCount
                };

                try
                {
                    // Uruchomienie zadań dla każdego wątku
                    for (int i = 0; i < threadCount; i++)
                    {
                        int threadIndex = i;
                        tasks[i] = Task.Run(() =>
                        {
                            var taskWatch = System.Diagnostics.Stopwatch.StartNew();
                            ProcessThreadSegment(
                                inputBuffer,
                                outputBuffer,
                                threadIndex,
                                pixelsPerThread,
                                remainingPixels,
                                threadCount,
                                brightness);
                            taskWatch.Stop();
                            lock (taskTimes)
                            {
                                taskTimes.Add(taskWatch.ElapsedMilliseconds);
                            }
                        });
                    }

                    Task.WaitAll(tasks);
                    watch.Stop();

                    // Obliczenie statystyk wydajności
                    CalculatePerformanceMetrics(
                        watch.ElapsedMilliseconds,
                        taskTimes,
                        pixelCount,
                        threadCount,
                        processingDetails);

                    return new ProcessingResult
                    {
                        ExecutionTime = watch.ElapsedMilliseconds,
                        ThreadCount = threadCount,
                        OutputBuffer = outputBuffer,
                        ProcessingDetails = processingDetails
                    };
                }
                finally
                {
                    if (outputHandle.IsAllocated)
                        outputHandle.Free();
                }
            });
        }

        private int CalculateOptimalBlockSize(int pixelCount, int threadCount)
        {
            // Obliczenie rozmiaru bloku optymalnego dla SIMD i cache
            int baseBlockSize = CACHE_LINE_SIZE / 3; // 3 bajty na piksel
            int pixelsPerThread = pixelCount / threadCount;

            // Zaokrąglenie do wielokrotności SIMD_ALIGNMENT
            return ((baseBlockSize + SIMD_ALIGNMENT - 1) / SIMD_ALIGNMENT) * SIMD_ALIGNMENT;
        }

        private void ProcessThreadSegment(
            byte[] inputBuffer,
            byte[] outputBuffer,
            int threadIndex,
            int pixelsPerThread,
            int remainingPixels,
            int totalThreads,
            float brightness)
        {
            int startPixel = threadIndex * pixelsPerThread;
            int pixelsToProcess = (threadIndex == totalThreads - 1)
                ? remainingPixels
                : pixelsPerThread;

            int startOffset = startPixel * 3;
            int length = pixelsToProcess * 3;

            // Alokacja buforów tymczasowych z wyrównaniem
            byte[] alignedInput = new byte[length + SIMD_ALIGNMENT];
            byte[] alignedOutput = new byte[length + SIMD_ALIGNMENT];

            try
            {
                // Kopiowanie danych do wyrównanego bufora
                Buffer.BlockCopy(inputBuffer, startOffset, alignedInput, 0, length);

                // Przetwarzanie z użyciem wybranej biblioteki
                if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
                {
                    NativeMethods.GrayscaleFilter(alignedInput, alignedOutput, pixelsToProcess, brightness);
                }
                else
                {
                    CSLibrary.GrayscaleFilter(alignedInput, alignedOutput, pixelsToProcess, brightness);
                }

                // Kopiowanie wyników z powrotem
                Buffer.BlockCopy(alignedOutput, 0, outputBuffer, startOffset, length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd w wątku {threadIndex}: {ex.Message}");
                throw;
            }
        }

        private void CalculatePerformanceMetrics(
            long totalTime,
            List<long> taskTimes,
            int pixelCount,
            int threadCount,
            ProcessingDetails details)
        {
            float averageTaskTime = taskTimes.Count > 0 ? (float)taskTimes.Average() : 0;
            details.AverageTimePerPixel = averageTaskTime / pixelCount;
            details.PixelsPerMillisecond = pixelCount / (float)totalTime;

            if (threadCount > 1)
            {
                details.Speedup = taskTimes.Min() / (float)totalTime;
                details.ThreadEfficiency = details.Speedup / threadCount;
            }
            else
            {
                details.Speedup = 1.0f;
                details.ThreadEfficiency = 1.0f;
            }
        }

        public long ProcessImageCS(byte[] inputBuffer, int pixelCount, int threadCount, float brightness)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            byte[] outputBuffer = new byte[inputBuffer.Length];

            // Dla małych obrazów lub pojedynczego wątku używamy prostego przetwarzania
            if (threadCount == 1 || pixelCount < 1000)
            {
                NativeMethods.GrayscaleFilter(inputBuffer, outputBuffer, pixelCount, brightness);
            }
            else
            {
                // Dla większych obrazów używamy równoległego przetwarzania
                Parallel.For(0, threadCount, i =>
                {
                    int startPixel = i * (pixelCount / threadCount);
                    int endPixel = (i == threadCount - 1)
                        ? pixelCount
                        : (i + 1) * (pixelCount / threadCount);

                    int startOffset = startPixel * 3;
                    int length = (endPixel - startPixel) * 3;

                    byte[] segmentInput = new byte[length];
                    byte[] segmentOutput = new byte[length];

                    // Kopiowanie segmentu danych wejściowych
                    Buffer.BlockCopy(inputBuffer, startOffset, segmentInput, 0, length);

                    // Przetwarzanie segmentu
                    CSLibrary.GrayscaleFilter(
                        segmentInput,
                        segmentOutput,
                        endPixel - startPixel,
                        brightness);

                    // Kopiowanie wyników z powrotem do głównego bufora
                    Buffer.BlockCopy(segmentOutput, 0, outputBuffer, startOffset, length);
                });
            }

            watch.Stop();
            return watch.ElapsedMilliseconds;
        }
    }
}