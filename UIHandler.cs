using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Globalization;
using System.Linq;

namespace Filtr_czarno_biały
{
    public class UIHandler
    {
        private readonly MainForm mainForm;

        public UIHandler(MainForm form)
        {
            this.mainForm = form;
        }

        public void InitializeThreadTrackBar(TrackBar threadsTrackBar, int defaultThreadCount)
        {
            if (threadsTrackBar == null)
                return;

            try
            {
                // Ograniczamy domyślną liczbę wątków do zakresu TrackBara
                int initialValue = Math.Min(Math.Max(defaultThreadCount, threadsTrackBar.Minimum), threadsTrackBar.Maximum);
                threadsTrackBar.Value = initialValue;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas inicjalizacji TrackBar: {ex.Message}",
                              "Błąd",
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Warning);
            }
        }

        public void SetControlsEnabled(Control[] controls, bool enabled)
        {
            foreach (var control in controls)
            {
                if (control != null && !control.IsDisposed)
                {
                    control.Enabled = enabled;
                }
            }
        }

        public void ShowResults(string results, Bitmap originalImage, int pixelCount, byte[] inputBuffer)
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
                RowCount = 3,
                Padding = new Padding(10)
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 30));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 10));

            // Panel wyników
            var resultTextBox = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 10),
                Text = results,
                ScrollBars = ScrollBars.Both,
                WordWrap = false
            };

            // Panel statystyk
            var statsPanel = new Panel
            {
                Dock = DockStyle.Fill
            };

            var statsTextBox = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 10),
                Text = GenerateStatisticsText(results, originalImage, pixelCount),
                ScrollBars = ScrollBars.Both,
                WordWrap = false
            };

            // Panel przycisków
            var buttonPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(0, 10, 0, 0)
            };
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            var saveResultsButton = new Button
            {
                Text = "Zapisz wyniki do pliku",
                Dock = DockStyle.Fill,
                Margin = new Padding(5)
            };

            var closeButton = new Button
            {
                Text = "Zamknij",
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
                            StringBuilder fullResults = new StringBuilder();
                            fullResults.AppendLine("WYNIKI TESTÓW WYDAJNOŚCI");
                            fullResults.AppendLine("=======================");
                            fullResults.AppendLine();
                            fullResults.AppendLine("Parametry obrazu:");
                            fullResults.AppendLine($"- Wymiary: {originalImage.Width}x{originalImage.Height}");
                            fullResults.AppendLine($"- Liczba pikseli: {pixelCount:N0}");
                            fullResults.AppendLine();
                            fullResults.AppendLine("Szczegółowe wyniki:");
                            fullResults.AppendLine(results);
                            fullResults.AppendLine();
                            fullResults.AppendLine("Statystyki:");
                            fullResults.AppendLine(statsTextBox.Text);

                            File.WriteAllText(saveDialog.FileName, fullResults.ToString());

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

            closeButton.Click += (s, e) => resultForm.Close();

            buttonPanel.Controls.Add(saveResultsButton, 0, 0);
            buttonPanel.Controls.Add(closeButton, 1, 0);

            mainLayout.Controls.Add(resultTextBox, 0, 0);
            mainLayout.Controls.Add(statsTextBox, 0, 1);
            mainLayout.Controls.Add(buttonPanel, 0, 2);

            resultForm.Controls.Add(mainLayout);
            resultForm.ShowDialog();
        }

        private string GenerateStatisticsText(string results, Bitmap originalImage, int pixelCount)
        {
            var stats = new StringBuilder();
            stats.AppendLine("Analiza statystyczna:");
            stats.AppendLine("====================");

            try
            {
                stats.AppendLine("\nInformacje o obrazie:");
                stats.AppendLine($"Rozmiar: {originalImage.Width}x{originalImage.Height} pikseli");
                stats.AppendLine($"Liczba pikseli: {pixelCount:N0}");
            }
            catch (Exception ex)
            {
                return $"Błąd podczas generowania statystyk: {ex.Message}";
            }

            return stats.ToString();
        }
    }
}