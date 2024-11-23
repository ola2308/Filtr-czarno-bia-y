using System;
using System.Collections.Generic;
using System.Linq;

namespace Filtr_czarno_biały
{
    public class ProcessingResult
    {
        // Podstawowe właściwości
        public long ExecutionTime { get; set; }
        public int ThreadCount { get; set; }
        public byte[] OutputBuffer { get; set; }
        public ProcessingDetails ProcessingDetails { get; set; }

        // Konstruktor domyślny
        public ProcessingResult()
        {
            ProcessingDetails = new ProcessingDetails();
        }

        // Konstruktor z parametrami
        public ProcessingResult(long executionTime, int threadCount, byte[] outputBuffer, ProcessingDetails details = null)
        {
            ExecutionTime = executionTime;
            ThreadCount = threadCount;
            OutputBuffer = outputBuffer;
            ProcessingDetails = details ?? new ProcessingDetails();
        }

        // Metoda do generowania podsumowania wyników
        public string GetSummary()
        {
            return $"Wyniki przetwarzania:\n" +
                   $"Czas wykonania: {ExecutionTime}ms\n" +
                   $"Liczba wątków: {ThreadCount}\n" +
                   $"Rozmiar bufora: {OutputBuffer?.Length ?? 0} bajtów\n\n" +
                   $"Szczegóły wydajności:\n{ProcessingDetails}";
        }

        // Metoda do porównywania wyników
        public static string CompareResults(ProcessingResult baseline, ProcessingResult current)
        {
            if (baseline == null || current == null)
                return "Brak danych do porównania";

            double speedup = (double)baseline.ExecutionTime / current.ExecutionTime;
            double efficiency = speedup / current.ThreadCount;
            double timeImprovement = ((double)baseline.ExecutionTime - current.ExecutionTime) / baseline.ExecutionTime * 100;

            return $"Porównanie wyników:\n" +
                   $"Bazowy czas wykonania (1 wątek): {baseline.ExecutionTime}ms\n" +
                   $"Aktualny czas wykonania ({current.ThreadCount} wątków): {current.ExecutionTime}ms\n" +
                   $"Przyspieszenie: {speedup:F2}x\n" +
                   $"Efektywność wątków: {efficiency:P2}\n" +
                   $"Poprawa czasu: {timeImprovement:F2}%";
        }

        // Metoda do obliczania statystyk dla wielu prób
        public static ProcessingResult CalculateStatistics(IEnumerable<ProcessingResult> results)
        {
            var resultsList = results.ToList();
            if (!resultsList.Any())
                return null;

            var avgTime = (long)resultsList.Average(r => r.ExecutionTime);
            var threadCount = resultsList.First().ThreadCount;
            var lastBuffer = resultsList.Last().OutputBuffer;

            var details = new ProcessingDetails
            {
                TotalThreads = threadCount,
                PixelsPerThread = resultsList.First().ProcessingDetails.PixelsPerThread,
                RemainingPixels = resultsList.First().ProcessingDetails.RemainingPixels,
                AverageTimePerPixel = resultsList.Average(r => r.ProcessingDetails.AverageTimePerPixel),
                PixelsPerMillisecond = resultsList.Average(r => r.ProcessingDetails.PixelsPerMillisecond),
                ThreadEfficiency = resultsList.Average(r => r.ProcessingDetails.ThreadEfficiency),
                Speedup = resultsList.Average(r => r.ProcessingDetails.Speedup)
            };

            return new ProcessingResult(avgTime, threadCount, lastBuffer, details);
        }

        // Metoda do walidacji wyników
        public bool IsValid()
        {
            return ExecutionTime > 0 &&
                   ThreadCount > 0 &&
                   OutputBuffer != null &&
                   OutputBuffer.Length > 0 &&
                   ProcessingDetails != null;
        }

        // Metoda do tworzenia kopii wyniku
        public ProcessingResult Clone()
        {
            return new ProcessingResult
            {
                ExecutionTime = this.ExecutionTime,
                ThreadCount = this.ThreadCount,
                OutputBuffer = this.OutputBuffer?.ToArray(),
                ProcessingDetails = new ProcessingDetails
                {
                    PixelsPerThread = this.ProcessingDetails.PixelsPerThread,
                    RemainingPixels = this.ProcessingDetails.RemainingPixels,
                    TotalThreads = this.ProcessingDetails.TotalThreads,
                    AverageTimePerPixel = this.ProcessingDetails.AverageTimePerPixel,
                    PixelsPerMillisecond = this.ProcessingDetails.PixelsPerMillisecond,
                    ThreadEfficiency = this.ProcessingDetails.ThreadEfficiency,
                    Speedup = this.ProcessingDetails.Speedup
                }
            };
        }
    }
}