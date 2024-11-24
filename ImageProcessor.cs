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
            bool useASM = true)  // Dodany parametr wyboru biblioteki
        {
            return await Task.Run(() =>
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                byte[] outputBuffer = new byte[inputBuffer.Length];

                if (useASM)
                {
                    // Logika dla ASM
                    int pixelsPerThread = pixelCount / threadCount;
                    var tasks = new Task[threadCount];

                    for (int i = 0; i < threadCount; i++)
                    {
                        int threadIndex = i;
                        int startPixel = threadIndex * pixelsPerThread;
                        int currentPixels = (threadIndex == threadCount - 1)
                            ? pixelCount - startPixel
                            : pixelsPerThread;

                        tasks[i] = Task.Run(() =>
                        {
                            int startOffset = startPixel * 3;
                            int length = currentPixels * 3;
                            byte[] threadInput = new byte[length];
                            byte[] threadOutput = new byte[length];

                            Buffer.BlockCopy(inputBuffer, startOffset, threadInput, 0, length);
                            NativeMethods.GrayscaleFilter(threadInput, threadOutput, currentPixels, brightness);
                            Buffer.BlockCopy(threadOutput, 0, outputBuffer, startOffset, length);
                        });
                    }

                    Task.WaitAll(tasks);
                }
                else
                {
                    // Logika dla C#
                    CSLibrary.GrayscaleFilter(inputBuffer, outputBuffer, pixelCount, brightness);
                }

                watch.Stop();

                return new ProcessingResult
                {
                    ExecutionTime = watch.ElapsedMilliseconds,
                    ThreadCount = threadCount,
                    OutputBuffer = outputBuffer,
                    ProcessingDetails = new ProcessingDetails
                    {
                        PixelsPerThread = pixelCount / threadCount,
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