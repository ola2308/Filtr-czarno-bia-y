using System;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Button = System.Windows.Forms.Button;
using TextBox = System.Windows.Forms.TextBox;
using Form = System.Windows.Forms.Form;
using TableLayoutPanel = System.Windows.Forms.TableLayoutPanel;
using Label = System.Windows.Forms.Label;
using PictureBox = System.Windows.Forms.PictureBox;
using TrackBar = System.Windows.Forms.TrackBar;
using ProgressBar = System.Windows.Forms.ProgressBar;
using ComboBox = System.Windows.Forms.ComboBox;

namespace Filtr_czarno_biały
{
    // Klasa do przechowywania szczegółowych informacji o wydajności
    public class ProcessingDetails
    {
        public int PixelsPerThread { get; set; }
        public int RemainingPixels { get; set; }
        public int TotalThreads { get; set; }
        public float AverageTimePerPixel { get; set; }
        public float PixelsPerMillisecond { get; set; }
        public float ThreadEfficiency { get; set; }
        public float Speedup { get; set; }

        public override string ToString()
        {
            return $"Pikseli na wątek: {PixelsPerThread}\n" +
                   $"Pozostałe piksele: {RemainingPixels}\n" +
                   $"Liczba wątków: {TotalThreads}\n" +
                   $"Średni czas na piksel: {AverageTimePerPixel:F6} ms\n" +
                   $"Wydajność: {PixelsPerMillisecond:F2} pikseli/ms\n" +
                   $"Efektywność wątków: {ThreadEfficiency:P2}\n" +
                   $"Przyspieszenie: {Speedup:F2}x";
        }
    }

    public partial class MainForm : Form
    {
        private const int DEFAULT_THREAD_COUNT = 4;
        private Bitmap originalImage;
        private Bitmap processedImage;
        private byte[] inputBuffer;
        private byte[] outputBuffer;
        private int pixelCount;
        private List<ProcessingResult> benchmarkResults;
        private ProcessingResult baselineResult; // Wynik dla jednego wątku jako punkt odniesienia

        public MainForm()
        {
            InitializeComponent();
            InitializeThreadComboBox();
            benchmarkResults = new List<ProcessingResult>();
        }

        private void InitializeThreadComboBox()
        {
            int[] threadOptions = { 1, 2, 4, 8, 16, 32, 64 };
            threadsComboBox.Items.AddRange(Array.ConvertAll(threadOptions, x => x.ToString()));
            threadsComboBox.SelectedItem = DEFAULT_THREAD_COUNT.ToString();
        }

        private async void LoadButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Pliki obrazów|*.jpg;*.jpeg;*.png;*.bmp";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        originalImage = new Bitmap(openFileDialog.FileName);
                        originalPictureBox.Image = originalImage;

                        pixelCount = originalImage.Width * originalImage.Height;
                        inputBuffer = GetImageBuffer(originalImage);
                        outputBuffer = new byte[inputBuffer.Length];

                        statusLabel.Text = $"Status: Obraz wczytany ({originalImage.Width}x{originalImage.Height})";
                        saveButton.Enabled = true;
                        processButton.Enabled = true;
                        brightnessTrackBar.Enabled = true;
                        threadsComboBox.Enabled = true;

                        // Wykonaj wstępne przetwarzanie
                        await ProcessImageAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Błąd podczas wczytywania obrazu: {ex.Message}",
                                      "Błąd",
                                      MessageBoxButtons.OK,
                                      MessageBoxIcon.Error);
                    }
                }
            }
        }
        private void BrightnessTrackBar_ValueChanged(object sender, EventArgs e)
        {
            if (originalImage != null)
            {
                ProcessImageAsync().ConfigureAwait(false);
            }
        }

        private async void ProcessButton_Click(object sender, EventArgs e)
        {
            if (originalImage == null)
            {
                MessageBox.Show("Najpierw wybierz obraz!", "Informacja", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                SetControlsEnabled(false);
                benchmarkResults.Clear();

                // Dynamiczne określenie liczby logicznych rdzeni CPU
                int optimalThreadCount = Environment.ProcessorCount;
                int[] threadCounts = { 1, 2, 4, 8, 16, 32, 64 };
                int totalTests = threadCounts.Length * 3; // 3 próby dla każdej liczby wątków
                int currentTest = 0;

                progressBar.Maximum = totalTests;
                progressBar.Value = 0;

                var results = new StringBuilder();
                results.AppendLine("Wyniki testów wydajności:");
                results.AppendLine("Liczba wątków | Średni czas ASM (ms) | Min czas ASM (ms) | Max czas ASM (ms) | Przyspieszenie | Efektywność | Średni czas C# (ms) | Min czas C# (ms) | Max czas C# (ms)");
                results.AppendLine("-----------------------------------------------------------------------------------------------------------");

                float brightness = (brightnessTrackBar.Value + 100) / 200f;

                // Najpierw wykonaj 3 próby dla 1 wątku jako baseline
                List<ProcessingResult> baselineResults = new List<ProcessingResult>();
                for (int i = 0; i < 3; i++)
                {
                    statusLabel.Text = $"Status: Test {currentTest + 1}/{totalTests} (wątki: 1)";
                    var result = await ProcessImageWithParamsAsync(1, brightness);
                    baselineResults.Add(result);
                    currentTest++;
                    progressBar.Value = currentTest;
                    Application.DoEvents();
                }
                double baselineTime = baselineResults.Average(r => r.ExecutionTime);
                benchmarkResults.Add(new ProcessingResult
                {
                    ExecutionTime = (long)baselineTime,
                    ThreadCount = 1
                });

                // Test dla C#
                List<long> csBaselineTimes = new List<long>();
                for (int i = 0; i < 3; i++)
                {
                    csBaselineTimes.Add(ProcessImageCS(1, brightness));
                }
                double csBaselineAvgTime = csBaselineTimes.Average();
                long csBaselineMinTime = csBaselineTimes.Min();
                long csBaselineMaxTime = csBaselineTimes.Max();

                results.AppendLine(
                    $"{1,12} | {baselineTime,14:F2} | {baselineResults.Min(r => r.ExecutionTime),11} | " +
                    $"{baselineResults.Max(r => r.ExecutionTime),11} | 1.00x | 100,00% | {csBaselineAvgTime:F2} | {csBaselineMinTime} | {csBaselineMaxTime}");

                // Następnie dla pozostałych liczby wątków
                foreach (int threadCount in threadCounts.Skip(1)) // Pomijamy 1, bo już mamy
                {
                    List<ProcessingResult> threadResults = new List<ProcessingResult>();

                    for (int i = 0; i < 3; i++)
                    {
                        statusLabel.Text = $"Status: Test {currentTest + 1}/{totalTests} (wątki: {threadCount})";
                        var result = await ProcessImageWithParamsAsync(threadCount, brightness);
                        threadResults.Add(result);
                        currentTest++;
                        progressBar.Value = currentTest;
                        Application.DoEvents();
                    }

                    // Oblicz statystyki
                    double avgTime = threadResults.Average(r => r.ExecutionTime);
                    long minTime = threadResults.Min(r => r.ExecutionTime);
                    long maxTime = threadResults.Max(r => r.ExecutionTime);
                    double speedup = baselineTime / avgTime;
                    double efficiency = speedup / threadCount;

                    // Test dla C#
                    List<long> csTimes = new List<long>();
                    for (int i = 0; i < 3; i++)
                    {
                        csTimes.Add(ProcessImageCS(threadCount, brightness));
                    }
                    double csAvgTime = csTimes.Average();
                    long csMinTime = csTimes.Min();
                    long csMaxTime = csTimes.Max();

                    results.AppendLine(
                        $"{threadCount,12} | {avgTime,14:F2} | {minTime,11} | {maxTime,11} | " +
                        $"{speedup,13:F2}x | {efficiency,10:P2} | {csAvgTime:F2} | {csMinTime} | {csMaxTime}");

                    // Zachowaj najlepszy wynik dla tej liczby wątków
                    benchmarkResults.Add(threadResults.OrderBy(r => r.ExecutionTime).First());
                }

                ShowResults(results.ToString());

                SetControlsEnabled(true);
                statusLabel.Text = "Status: Testy zakończone";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas testów: {ex.Message}", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetControlsEnabled(true);
            }
        }

        private long ProcessImageCS(int threadCount, float brightness)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            // Oblicz ile pikseli przypadnie na jeden wątek
            int pixelsPerThread = pixelCount / threadCount;
            int remainingPixels = pixelCount % threadCount;

            // Przygotuj bufory dla każdego wątków
            byte[] testBuffer = new byte[inputBuffer.Length];
            Array.Copy(inputBuffer, testBuffer, inputBuffer.Length);
            byte[] csOutputBuffer = new byte[inputBuffer.Length];

            // Przetwarzanie przy użyciu C#
            CSMethods.GrayscaleFilter(testBuffer, csOutputBuffer, pixelCount, brightness);

            watch.Stop();
            return watch.ElapsedMilliseconds;
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            if (processedImage == null)
            {
                MessageBox.Show("Brak przetworzonego obrazu do zapisania!",
                              "Informacja",
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Information);
                return;
            }

            using (SaveFileDialog saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "PNG Image|*.png|JPEG Image|*.jpg|Bitmap Image|*.bmp";
                saveDialog.Title = "Zapisz przetworzony obraz";

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        processedImage.Save(saveDialog.FileName);
                        MessageBox.Show("Obraz został zapisany pomyślnie!",
                                      "Sukces",
                                      MessageBoxButtons.OK,
                                      MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Błąd podczas zapisywania obrazu: {ex.Message}",
                                      "Błąd",
                                      MessageBoxButtons.OK,
                                      MessageBoxIcon.Error);
                    }
                }
            }
        }

        private byte[] GetImageBuffer(Bitmap image)
        {
            int width = image.Width;
            int height = image.Height;
            int bytesPerPixel = Image.GetPixelFormatSize(PixelFormat.Format24bppRgb) / 8;
            int stride = width * bytesPerPixel;
            byte[] buffer = new byte[stride * height];

            BitmapData bmpData = image.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format24bppRgb);

            for (int y = 0; y < height; y++)
            {
                IntPtr ptr = bmpData.Scan0 + y * bmpData.Stride;
                Marshal.Copy(ptr, buffer, y * stride, stride);
            }

            image.UnlockBits(bmpData);
            return buffer;
        }


        private async Task<ProcessingResult> ProcessImageWithParamsAsync(int threadCount, float brightness)
        {
            return await Task.Run(() =>
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();

                // Oblicz ile pikseli przypadnie na jeden wątek
                int pixelsPerThread = pixelCount / threadCount;
                int remainingPixels = pixelCount % threadCount;

                // Przygotuj bufory dla każdego wątków
                byte[] testBuffer = new byte[inputBuffer.Length];
                Array.Copy(inputBuffer, testBuffer, inputBuffer.Length);

                // Utwórz i uruchom zadania dla wszystkich wątków
                var tasks = new Task[threadCount];

                for (int i = 0; i < threadCount; i++)
                {
                    int threadIndex = i;
                    int startPixel = threadIndex * pixelsPerThread;
                    int pixelsForThread = (threadIndex == threadCount - 1)
                        ? pixelsPerThread + remainingPixels
                        : pixelsPerThread;

                    tasks[i] = Task.Run(() =>
                    {
                        int startOffset = startPixel * 3; // 3 bajty na piksel (RGB)
                        int endOffset = startOffset + (pixelsForThread * 3);

                        // Utwórz podtablicę dla testBuffer
                        byte[] inputSegment = new byte[endOffset - startOffset];
                        Array.Copy(testBuffer, startOffset, inputSegment, 0, inputSegment.Length);

                        // Utwórz podtablicę dla outputBuffer
                        byte[] outputSegment = new byte[endOffset - startOffset];

                        // Wywołaj funkcję asemblerową dla segmentu obrazu
                        NativeMethods.GrayscaleFilter(
                            inputSegment,
                            outputSegment,
                            pixelsForThread,
                            brightness);

                        // Przepisz dane wyjściowe do właściwej części bufora
                        Array.Copy(outputSegment, 0, outputBuffer, startOffset, outputSegment.Length);
                    });
                }

                // Poczekaj na zakończenie wszystkich wątków
                Task.WaitAll(tasks);
                watch.Stop();

                // Oblicz statystyki wydajności
                float timeInMs = watch.ElapsedMilliseconds;
                float pixelsPerMs = pixelCount / timeInMs;
                float threadEfficiency = 1.0f;
                float speedup = 1.0f;

                if (baselineResult != null && threadCount > 1)
                {
                    speedup = (float)baselineResult.ExecutionTime / timeInMs;
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
                        AverageTimePerPixel = timeInMs / pixelCount,
                        PixelsPerMillisecond = pixelsPerMs,
                        ThreadEfficiency = threadEfficiency,
                        Speedup = speedup
                    }
                };
            });
        }


        private async Task ProcessImageAsync()
        {
            if (originalImage == null) return;

            try
            {
                int threadCount = int.Parse(threadsComboBox.SelectedItem.ToString());
                float brightness = (brightnessTrackBar.Value + 100) / 200f;

                var result = await ProcessImageWithParamsAsync(threadCount, brightness);

                processedImage = CreateBitmapFromBuffer(result.OutputBuffer, originalImage.Width, originalImage.Height);
                processedPictureBox.Image = processedImage;
                executionTimeLabel.Text = $"Czas wykonania: {result.ExecutionTime}ms" +
                    $"\nWydajność: {result.ProcessingDetails.PixelsPerMillisecond:F2} pikseli/ms";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas przetwarzania: {ex.Message}",
                              "Błąd",
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Error);
            }
        }
        private void ShowResults(string results)
{
    var resultForm = new Form
    {
        Text = "Wyniki testów wydajności",
        Size = new Size(800, 600),
        StartPosition = FormStartPosition.CenterParent
    };

    var mainLayout = new TableLayoutPanel
    {
        Dock = DockStyle.Fill,
        ColumnCount = 1,
        RowCount = 2,
        Padding = new Padding(10)
    };
    mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 70));
    mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 30));

    var formattedResults = new StringBuilder();
    formattedResults.AppendLine("Wyniki testów wydajności:");
    formattedResults.AppendLine("Liczba wątków | Średni czas ASM (ms) | Min czas ASM (ms) | Max czas ASM (ms) | Przyspieszenie | Efektywność | Średni czas C# (ms) | Min czas C# (ms) | Max czas C# (ms)");
    formattedResults.AppendLine(new string('-', 125));
    formattedResults.AppendLine(results);

    var resultTextBox = new System.Windows.Forms.TextBox
    {
        Multiline = true,
        ReadOnly = true,
        Dock = DockStyle.Fill,
        Font = new Font("Consolas", 10),
        Text = formattedResults.ToString() + "\n\nSzczegółowa analiza:\n" +
               $"Rozmiar obrazu: {originalImage.Width}x{originalImage.Height} pikseli\n" +
               $"Całkowita liczba pikseli: {pixelCount}\n" +
               $"Rozmiar danych: {inputBuffer.Length} bajtów\n\n" +
               "Legenda:\n" +
               "- Przyspieszenie: stosunek czasu wykonania dla 1 wątku do czasu dla N wątków\n" +
               "- Efektywność: przyspieszenie podzielone przez liczbę wątków\n" +
               "- Średni czas: średnia z 3 prób dla każdej konfiguracji\n" +
               "- Min/Max czas: najlepszy i najgorszy wynik z prób\n"
    };

    var buttonPanel = new TableLayoutPanel
    {
        Dock = DockStyle.Fill,
        ColumnCount = 2,
        RowCount = 1,
        Padding = new Padding(0, 10, 0, 0)
    };
    buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
    buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

    var saveResultsButton = new System.Windows.Forms.Button
    {
        Text = "Zapisz wyniki do pliku",
        Dock = DockStyle.Fill,
        Margin = new Padding(5)
    };

    var saveImageButton = new System.Windows.Forms.Button
    {
        Text = "Zapisz przetworzony obraz",
        Dock = DockStyle.Fill,
        Margin = new Padding(5)
    };

    saveResultsButton.Click += (s, e) =>
    {
        using (SaveFileDialog saveDialog = new SaveFileDialog())
        {
            saveDialog.Filter = "Text file|*.txt";
            saveDialog.Title = "Zapisz wyniki testów";
            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    File.WriteAllText(saveDialog.FileName, resultTextBox.Text);
                    MessageBox.Show("Wyniki zostały zapisane pomyślnie!",
                                  "Sukces",
                                  MessageBoxButtons.OK,
                                  MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Błąd podczas zapisywania wyników: {ex.Message}",
                                  "Błąd",
                                  MessageBoxButtons.OK,
                                  MessageBoxIcon.Error);
                }
            }
        }
    };

    saveImageButton.Click += SaveButton_Click;

    buttonPanel.Controls.Add(saveResultsButton, 0, 0);
    buttonPanel.Controls.Add(saveImageButton, 1, 0);

    mainLayout.Controls.Add(resultTextBox, 0, 0);
    mainLayout.Controls.Add(buttonPanel, 0, 1);

    resultForm.Controls.Add(mainLayout);
    resultForm.ShowDialog();
}


        private void SetControlsEnabled(bool enabled)
        {
            threadsComboBox.Enabled = enabled;
            brightnessTrackBar.Enabled = enabled;
            processButton.Enabled = enabled;
            saveButton.Enabled = enabled;
        }

        private Bitmap CreateBitmapFromBuffer(byte[] buffer, int width, int height)
        {
            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            int bytesPerPixel = Image.GetPixelFormatSize(PixelFormat.Format24bppRgb) / 8;
            int stride = width * bytesPerPixel;

            BitmapData bmpData = bitmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format24bppRgb);

            for (int y = 0; y < height; y++)
            {
                IntPtr ptr = bmpData.Scan0 + y * bmpData.Stride;
                Marshal.Copy(buffer, y * stride, ptr, stride);
            }

            bitmap.UnlockBits(bmpData);
            return bitmap;
        }


    }
}
