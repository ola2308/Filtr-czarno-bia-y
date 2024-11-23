using System;
using System.IO;

namespace Filtr_czarno_biały
{
    public static class FileHandler
    {
        // Metoda do odczytu danych z pliku
        public static byte[] LoadFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("Ścieżka pliku nie może być pusta.");

            try
            {
                return File.ReadAllBytes(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas odczytu pliku: {ex.Message}");
                throw;
            }
        }

        // Metoda do zapisu danych do pliku
        public static void SaveFile(string filePath, byte[] data)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("Ścieżka pliku nie może być pusta.");

            if (data == null || data.Length == 0)
                throw new ArgumentException("Dane do zapisu nie mogą być puste.");

            try
            {
                File.WriteAllBytes(filePath, data);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas zapisu pliku: {ex.Message}");
                throw;
            }
        }
    }
}
