using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Filtr_czarno_biały
{
    public class UIHandler
    {
        private readonly MainForm mainForm;

        public UIHandler(MainForm form)
        {
            this.mainForm = form;
        }

        // Metoda inicjalizująca ComboBox do wyboru liczby wątków
        public void InitializeThreadComboBox(ComboBox threadsComboBox, int defaultThreadCount)
        {
            int[] threadOptions = { 1, 2, 4, 8, 16, 32, 64 };
            threadsComboBox.Items.AddRange(Array.ConvertAll(threadOptions, x => x.ToString()));
            threadsComboBox.SelectedItem = defaultThreadCount.ToString();
        }

        // Metoda do ustawiania stanu kontrolek
        public void SetControlsEnabled(Control[] controls, bool enabled)
        {
            foreach (var control in controls)
            {
                control.Enabled = enabled;
            }
        }

        // Metoda do wyświetlania wyników testów w nowym oknie
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
                RowCount = 2,
                Padding = new Padding(10)
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 70));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 30));

            var formattedResults = new StringBuilder();
            formattedResults.AppendLine("Wyniki testów wydajności:");
            formattedResults.AppendLine("Liczba wątków | Średni czas ASM (ms) | Min czas ASM (ms) | Max czas ASM (ms) | Przyspieszenie | Efektywność | Średni czas C# (ms) | Min czas C# (ms) | Max czas C# (ms)");
            formattedResults.AppendLine(new string('-', 145));

            // Rozdzielenie i sformatowanie wyników
            var lines = results.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var columns = line.Split('|');
                if (columns.Length == 9)
                {
                    formattedResults.AppendLine(
                        $"{columns[0],12} | {columns[1],20} | {columns[2],15} | {columns[3],15} | {columns[4],15} | {columns[5],15} | {columns[6],15} | {columns[7],15} | {columns[8],15}"
                    );
                }
                else
                {
                    formattedResults.AppendLine(line);
                }
            }

            formattedResults.AppendLine("\n\nSzczegółowa analiza:");
            formattedResults.AppendLine($"Rozmiar obrazu: {originalImage.Width}x{originalImage.Height} pikseli");
            formattedResults.AppendLine($"Całkowita liczba pikseli: {pixelCount}");
            formattedResults.AppendLine($"Rozmiar danych: {inputBuffer.Length} bajtów");
            formattedResults.AppendLine("\nLegenda:");
            formattedResults.AppendLine("- Przyspieszenie: stosunek czasu wykonania dla 1 wątku do czasu dla N wątków");
            formattedResults.AppendLine("- Efektywność: przyspieszenie podzielone przez liczbę wątków");
            formattedResults.AppendLine("- Średni czas: średnia z 3 prób dla każdej konfiguracji");
            formattedResults.AppendLine("- Min/Max czas: najlepszy i najgorszy wynik z prób");

            var resultTextBox = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 10),
                Text = formattedResults.ToString()
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

            var saveResultsButton = new Button
            {
                Text = "Zapisz wyniki do pliku",
                Dock = DockStyle.Fill,
                Margin = new Padding(5)
            };

            var saveImageButton = new Button
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
                            FileHandler.SaveFile(saveDialog.FileName, Encoding.UTF8.GetBytes(resultTextBox.Text));
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

            saveImageButton.Click += mainForm.SaveButton_Click;

            buttonPanel.Controls.Add(saveResultsButton, 0, 0);
            buttonPanel.Controls.Add(saveImageButton, 1, 0);

            mainLayout.Controls.Add(resultTextBox, 0, 0);
            mainLayout.Controls.Add(buttonPanel, 0, 1);

            resultForm.Controls.Add(mainLayout);
            resultForm.ShowDialog();
        }
    }
}
