﻿using System;
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
                int totalTests = threadCounts.Length * 3;
                int currentTest = 0;

                progressBar.Maximum = totalTests;
                progressBar.Value = 0;

                var results = new StringBuilder();

                float brightness = (brightnessTrackBar.Value + 100) / 200f;

                // Przetwarzanie dla 1 wątku jako bazowy wynik
                List<ProcessingResult> baselineResults = new List<ProcessingResult>();
                List<long> csBaselineTimes = new List<long>();
                for (int i = 0; i < 3; i++)
                {
                    statusLabel.Text = $"Status: Test {currentTest + 1}/{totalTests} (wątki: 1)";
                    var result = await imageProcessor.ProcessImageWithParamsAsync(inputBuffer, 1, brightness, pixelCount);
                    baselineResults.Add(result);

                    // Dodaj przetwarzanie C# dla 1 wątku
                    long csTime = imageProcessor.ProcessImageCS(inputBuffer, pixelCount, 1, brightness);
                    csBaselineTimes.Add(csTime);

                    currentTest++;
                    progressBar.Value = currentTest;
                    Application.DoEvents();
                }
                double baselineTime = baselineResults.Average(r => r.ExecutionTime);
                double csBaselineAvgTime = csBaselineTimes.Average();
                long csBaselineMinTime = csBaselineTimes.Min();
                long csBaselineMaxTime = csBaselineTimes.Max();

                benchmarkResults.Add(new ProcessingResult
                {
                    ExecutionTime = (long)baselineTime,
                    ThreadCount = 1
                });

                results.AppendLine($"{1,12} | {baselineTime,14:F2} | {baselineResults.Min(r => r.ExecutionTime),11} | {baselineResults.Max(r => r.ExecutionTime),11} | 1.00x | 100,00% | {csBaselineAvgTime:F2} | {csBaselineMinTime} | {csBaselineMaxTime}");

                // Przetwarzanie dla pozostałych liczb wątków
                foreach (int threadCount in threadCounts.Skip(1))
                {
                    List<ProcessingResult> threadResults = new List<ProcessingResult>();
                    List<long> csTimes = new List<long>();

                    for (int i = 0; i < 3; i++)
                    {
                        statusLabel.Text = $"Status: Test {currentTest + 1}/{totalTests} (wątki: {threadCount})";
                        var result = await imageProcessor.ProcessImageWithParamsAsync(inputBuffer, threadCount, brightness, pixelCount);
                        threadResults.Add(result);

                        // Dodaj przetwarzanie C# dla każdej liczby wątków
                        long csTime = imageProcessor.ProcessImageCS(inputBuffer, pixelCount, threadCount, brightness);
                        csTimes.Add(csTime);

                        currentTest++;
                        progressBar.Value = currentTest;
                        Application.DoEvents();
                    }

                    double avgTime = threadResults.Average(r => r.ExecutionTime);
                    long minTime = threadResults.Min(r => r.ExecutionTime);
                    long maxTime = threadResults.Max(r => r.ExecutionTime);
                    double speedup = baselineTime / avgTime;
                    double efficiency = speedup / threadCount;

                    double csAvgTime = csTimes.Average();
                    long csMinTime = csTimes.Min();
                    long csMaxTime = csTimes.Max();

                    results.AppendLine($"{threadCount,12} | {avgTime,14:F2} | {minTime,11} | {maxTime,11} | {speedup,13:F2}x | {efficiency,10:P2} | {csAvgTime:F2} | {csMinTime} | {csMaxTime}");

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

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        using (var ms = new MemoryStream())
                        {
                            processedImage.Save(ms, processedImage.RawFormat);
                            FileHandler.SaveFile(saveDialog.FileName, ms.ToArray());
                        }
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
            int bytesPerPixel = 3; // Format24bppRgb = 3 bytes per pixel
            int bufferSize = width * height * bytesPerPixel;
            byte[] buffer = new byte[bufferSize];

            BitmapData bmpData = image.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format24bppRgb);

            try
            {
                int stride = bmpData.Stride;
                byte[] row = new byte[stride];
                IntPtr scan0 = bmpData.Scan0;

                for (int y = 0; y < height; y++)
                {
                    Marshal.Copy(scan0 + y * stride, row, 0, stride);
                    Buffer.BlockCopy(row, 0, buffer, y * width * bytesPerPixel, width * bytesPerPixel);
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

                var result = await imageProcessor.ProcessImageWithParamsAsync(inputBuffer, threadCount, brightness, pixelCount);

                processedImage = CreateBitmapFromBuffer(result.OutputBuffer, originalImage.Width, originalImage.Height);
                processedPictureBox.Image = processedImage;
                executionTimeLabel.Text = $"Czas wykonania: {result.ExecutionTime}ms\nWydajność: {result.ProcessingDetails.PixelsPerMillisecond:F2} pikseli/ms";
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
                byte[] row = new byte[stride];
                IntPtr scan0 = bmpData.Scan0;

                for (int y = 0; y < height; y++)
                {
                    Buffer.BlockCopy(buffer, y * width * 3, row, 0, width * 3);
                    Marshal.Copy(row, 0, scan0 + y * stride, stride);
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
