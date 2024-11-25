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
using BibliotekaCS1;

namespace Filtr_czarno_biały
{
    public partial class MainForm : Form
    {
        private const int DEFAULT_THREAD_COUNT = 4;
        private Bitmap originalImage;
        private Bitmap processedImage;
        private byte[] inputBuffer;
        private int pixelCount;
        private List<ProcessingResult> benchmarkResults;
        private ProcessingResult baselineResult;
        private readonly UIHandler uiHandler;
        private readonly ImageProcessor imageProcessor;

        public MainForm()
        {
            InitializeComponent();
            uiHandler = new UIHandler(this);
            uiHandler.InitializeThreadComboBox(threadsComboBox, DEFAULT_THREAD_COUNT);
            benchmarkResults = new List<ProcessingResult>();
            imageProcessor = new ImageProcessor();

            asmRadioButton.CheckedChanged += LibraryRadioButton_CheckedChanged;
            csRadioButton.CheckedChanged += LibraryRadioButton_CheckedChanged;
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
                        byte[] fileData = FileHandler.LoadFile(openFileDialog.FileName);
                        using (var ms = new MemoryStream(fileData))
                        {
                            originalImage = new Bitmap(ms);
                        }
                        originalPictureBox.Image = originalImage;

                        pixelCount = originalImage.Width * originalImage.Height;
                        inputBuffer = GetImageBuffer(originalImage);

                        statusLabel.Text = $"Status: Obraz wczytany ({originalImage.Width}x{originalImage.Height})";
                        SetControlsEnabled(true);

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

        private void LibraryRadioButton_CheckedChanged(object sender, EventArgs e)
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

                int[] threadCounts = { 1, 2, 4, 8, 16, 32, 64 };
                int totalTests = threadCounts.Length * 5;
                int currentTest = 0;

                progressBar.Maximum = totalTests;
                progressBar.Value = 0;

                var results = new StringBuilder();
                results.AppendLine("Wyniki testów wydajności:");
                results.AppendLine("Liczba wątków | Średni czas ASM (ms) | Min czas ASM (ms) | Max czas ASM (ms) | Średni czas C# (ms) | Min czas C# (ms) | Max czas C# (ms)");
                results.AppendLine(new string('-', 120));

                float brightness = (brightnessTrackBar.Value + 100) / 200f;

                foreach (int threadCount in threadCounts)
                {
                    List<ProcessingResult> threadResults = new List<ProcessingResult>();
                    List<long> csTimes = new List<long>();

                    for (int i = 0; i < 5; i++)
                    {
                        statusLabel.Text = $"Status: Test {currentTest + 1}/{totalTests} (wątki: {threadCount})";

                        // Test ASM
                        var result = await imageProcessor.ProcessImageWithParamsAsync(
                            inputBuffer,
                            threadCount,
                            brightness,
                            pixelCount,
                            true);
                        threadResults.Add(result);

                        // Test C#
                        long csTime = imageProcessor.ProcessImageCS(inputBuffer, pixelCount, threadCount, brightness);
                        csTimes.Add(csTime);

                        currentTest++;
                        progressBar.Value = currentTest;
                        Application.DoEvents();
                    }

                    double avgTime = threadResults.Average(r => r.ExecutionTime);
                    double minTime = threadResults.Min(r => r.ExecutionTime);
                    double maxTime = threadResults.Max(r => r.ExecutionTime);

                    double csAvgTime = csTimes.Average();
                    long csMinTime = csTimes.Min();
                    long csMaxTime = csTimes.Max();

                    results.AppendFormat("{0,12} | {1,18:F3} | {2,15:F3} | {3,15:F3} | {4,17:F3} | {5,14:F3} | {6,14:F3}",
                        threadCount,
                        avgTime,
                        minTime,
                        maxTime,
                        csAvgTime,
                        csMinTime,
                        csMaxTime);
                    results.AppendLine();

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

        public void SaveButton_Click(object sender, EventArgs e)
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
                saveDialog.DefaultExt = "png";

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string extension = Path.GetExtension(saveDialog.FileName).ToLower();
                        ImageFormat format = ImageFormat.Png;

                        switch (extension)
                        {
                            case ".jpg":
                            case ".jpeg":
                                format = ImageFormat.Jpeg;
                                break;
                            case ".bmp":
                                format = ImageFormat.Bmp;
                                break;
                        }

                        processedImage.Save(saveDialog.FileName, format);

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
            int bytesPerPixel = 3;
            int bufferSize = width * height * bytesPerPixel;
            byte[] buffer = new byte[bufferSize];

            BitmapData bmpData = image.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format24bppRgb);

            try
            {
                int stride = bmpData.Stride;
                for (int y = 0; y < height; y++)
                {
                    IntPtr scan0 = bmpData.Scan0 + (y * stride);
                    Marshal.Copy(scan0, buffer, y * width * bytesPerPixel, width * bytesPerPixel);
                }
                return buffer;
            }
            finally
            {
                image.UnlockBits(bmpData);
            }
        }

        private async Task ProcessImageAsync()
        {
            if (originalImage == null) return;

            try
            {
                int threadCount = int.Parse(threadsComboBox.SelectedItem.ToString());
                float brightness = (brightnessTrackBar.Value + 100) / 200f;

                ProcessingResult result;
                if (asmRadioButton.Checked)
                {
                    result = await imageProcessor.ProcessImageWithParamsAsync(
                        inputBuffer,
                        threadCount,
                        brightness,
                        pixelCount,
                        true);
                }
                else
                {
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    byte[] outputBuffer = new byte[inputBuffer.Length];
                    CSLibrary.GrayscaleFilter(inputBuffer, outputBuffer, pixelCount, brightness);
                    watch.Stop();

                    result = new ProcessingResult
                    {
                        ExecutionTime = watch.ElapsedMilliseconds,
                        ThreadCount = 1,
                        OutputBuffer = outputBuffer,
                        ProcessingDetails = new ProcessingDetails
                        {
                            PixelsPerThread = pixelCount,
                            TotalThreads = 1,
                            PixelsPerMillisecond = pixelCount / (float)watch.ElapsedMilliseconds
                        }
                    };
                }

                processedImage = CreateBitmapFromBuffer(result.OutputBuffer, originalImage.Width, originalImage.Height);
                processedPictureBox.Image = processedImage;
                executionTimeLabel.Text = $"Czas wykonania: {result.ExecutionTime}ms\n" +
                                        $"Wydajność: {result.ProcessingDetails.PixelsPerMillisecond:F2} pikseli/ms";
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
            uiHandler.ShowResults(results, originalImage, pixelCount, inputBuffer);
        }

        private void SetControlsEnabled(bool enabled)
        {
            uiHandler.SetControlsEnabled(new Control[] { threadsComboBox, brightnessTrackBar, processButton, saveButton }, enabled);
        }

        private Bitmap CreateBitmapFromBuffer(byte[] buffer, int width, int height)
        {
            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            BitmapData bmpData = bitmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format24bppRgb);

            try
            {
                int stride = bmpData.Stride;
                for (int y = 0; y < height; y++)
                {
                    IntPtr scan0 = bmpData.Scan0 + (y * stride);
                    Marshal.Copy(buffer, y * width * 3, scan0, width * 3);
                }
                return bitmap;
            }
            finally
            {
                bitmap.UnlockBits(bmpData);
            }
        }
    }
}