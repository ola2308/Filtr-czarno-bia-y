using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace Filtr_czarno_biały
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        private TableLayoutPanel mainLayout;
        private TableLayoutPanel imagePanel;
        private PictureBox originalPictureBox;
        private PictureBox processedPictureBox;
        private TableLayoutPanel controlPanel;
        private Button loadButton;
        private GroupBox libraryGroupBox;
        private RadioButton asmRadioButton;
        private RadioButton csRadioButton;
        private Label threadLabel;
        private TrackBar threadsTrackBar;  // Zmienione z ComboBox na TrackBar
        private Label threadCountLabel;     // Nowy Label do wyświetlania liczby wątków
        private Label brightnessLabel;
        private TrackBar brightnessTrackBar;
        private Button processButton;
        private Button saveButton;
        private ProgressBar progressBar;
        private Label executionTimeLabel;
        private Label statusLabel;
        private Label dragDropLabel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            try
            {
                components = new System.ComponentModel.Container();

                // Ustawienia formularza
                this.Text = "Filtr czarno-biały";
                this.Size = new System.Drawing.Size(1200, 800);
                this.StartPosition = FormStartPosition.CenterScreen;
                try
                {
                    this.Icon = new Icon("app_icon.ico");
                }
                catch { }

                // Główny układ
                mainLayout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 2,
                    RowCount = 1,
                    Padding = new Padding(10),
                    BackColor = Color.WhiteSmoke
                };
                mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 75F));
                mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

                // Panel obrazów
                imagePanel = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 2,
                    RowCount = 1,
                    Padding = new Padding(5),
                    BackColor = Color.White
                };
                imagePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                imagePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

                // PictureBox dla oryginalnego obrazu
                originalPictureBox = new PictureBox
                {
                    Dock = DockStyle.Fill,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BorderStyle = BorderStyle.FixedSingle,
                    BackColor = Color.White,
                    AllowDrop = true
                };

                // Label dla drag & drop
                dragDropLabel = new Label
                {
                    Text = "Przeciągnij zdjęcie tutaj",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Color.Transparent,
                    Font = new Font("Segoe UI", 12F, FontStyle.Regular),
                    AutoSize = false
                };
                originalPictureBox.Controls.Add(dragDropLabel);

                // PictureBox dla przetworzonego obrazu
                processedPictureBox = new PictureBox
                {
                    Dock = DockStyle.Fill,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BorderStyle = BorderStyle.FixedSingle,
                    BackColor = Color.White
                };

                // Panel kontrolny
                controlPanel = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 1,
                    RowCount = 12,  // Zwiększono o 1 dla nowego Label
                    Padding = new Padding(5),
                    BackColor = Color.White
                };

                for (int i = 0; i < 12; i++)
                {
                    controlPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
                }
                controlPanel.RowStyles[1] = new RowStyle(SizeType.Absolute, 80F);

                // Przycisk wczytywania
                loadButton = new Button
                {
                    Text = "Wczytaj obraz",
                    Dock = DockStyle.Fill,
                    Height = 40,
                    Padding = new Padding(10, 0, 0, 0),
                    Margin = new Padding(3, 3, 3, 10),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                    UseVisualStyleBackColor = true
                };
                loadButton.Click += new EventHandler(LoadButton_Click);

                // GroupBox wyboru biblioteki
                libraryGroupBox = new GroupBox
                {
                    Text = "Wybór biblioteki",
                    Dock = DockStyle.Fill,
                    Margin = new Padding(3, 3, 3, 10),
                    Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                    BackColor = Color.White
                };

                // RadioButton dla ASM
                asmRadioButton = new RadioButton
                {
                    Text = "ASM x64",
                    Location = new Point(10, 20),
                    Size = new Size(150, 20),
                    Checked = true,
                    Font = new Font("Segoe UI", 9F, FontStyle.Regular)
                };
                asmRadioButton.CheckedChanged += new EventHandler(LibraryRadioButton_CheckedChanged);

                // RadioButton dla C#
                csRadioButton = new RadioButton
                {
                    Text = "C#",
                    Location = new Point(10, 40),
                    Size = new Size(150, 20),
                    Font = new Font("Segoe UI", 9F, FontStyle.Regular)
                };
                csRadioButton.CheckedChanged += new EventHandler(LibraryRadioButton_CheckedChanged);

                libraryGroupBox.Controls.Add(asmRadioButton);
                libraryGroupBox.Controls.Add(csRadioButton);

                // Etykieta liczby wątków
                threadLabel = new Label
                {
                    Text = "Liczba wątków:",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = new Font("Segoe UI", 9F, FontStyle.Regular)
                };

                // TrackBar liczby wątków
                threadsTrackBar = new TrackBar
                {
                    Dock = DockStyle.Fill,
                    Minimum = 1,     // Minimum 1 wątek
                    Maximum = 64,    // Maximum 64 wątki
                    TickStyle = TickStyle.Both,
                    TickFrequency = 4,  // Znaczniki co 4 wątki
                    Margin = new Padding(3, 3, 3, 10)
                };
                threadsTrackBar.ValueChanged += new EventHandler(ThreadsTrackBar_ValueChanged);

                // Label wyświetlający aktualną liczbę wątków
                threadCountLabel = new Label
                {
                    Text = "4 wątki",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                    Margin = new Padding(3, 3, 3, 10)
                };

                // Etykieta jasności
                brightnessLabel = new Label
                {
                    Text = "Jasność:",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = new Font("Segoe UI", 9F, FontStyle.Regular)
                };

                // TrackBar jasności
                brightnessTrackBar = new TrackBar
                {
                    Minimum = -50,
                    Maximum = 100,
                    Value = 0,
                    Dock = DockStyle.Fill,
                    TickFrequency = 25,
                    TickStyle = TickStyle.Both,
                    Margin = new Padding(3, 3, 3, 10)
                };
                brightnessTrackBar.ValueChanged += new EventHandler(BrightnessTrackBar_ValueChanged);

                // Przycisk przetwarzania
                processButton = new Button
                {
                    Text = "Wykonaj testy wydajności",
                    Dock = DockStyle.Fill,
                    Height = 40,
                    Padding = new Padding(10, 0, 0, 0),
                    Margin = new Padding(3, 3, 3, 10),
                    Enabled = false,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                    UseVisualStyleBackColor = true
                };
                processButton.Click += new EventHandler(ProcessButton_Click);

                // Przycisk zapisu
                saveButton = new Button
                {
                    Text = "Zapisz obraz",
                    Dock = DockStyle.Fill,
                    Height = 40,
                    Padding = new Padding(10, 0, 0, 0),
                    Margin = new Padding(3, 3, 3, 10),
                    Enabled = false,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                    UseVisualStyleBackColor = true
                };
                saveButton.Click += new EventHandler(SaveButton_Click);

                // ProgressBar
                progressBar = new ProgressBar
                {
                    Dock = DockStyle.Fill,
                    Height = 20,
                    Style = ProgressBarStyle.Continuous,
                    Margin = new Padding(3, 3, 3, 10)
                };

                // Etykieta czasu wykonania
                executionTimeLabel = new Label
                {
                    Text = "Czas wykonania: -",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Margin = new Padding(3, 3, 3, 5),
                    Font = new Font("Segoe UI", 9F, FontStyle.Regular)
                };

                // Etykieta statusu
                statusLabel = new Label
                {
                    Text = "Status: Gotowy",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = new Font("Segoe UI", 9F, FontStyle.Regular)
                };

                // Dodawanie kontrolek do paneli
                imagePanel.Controls.Add(originalPictureBox, 0, 0);
                imagePanel.Controls.Add(processedPictureBox, 1, 0);

                // Dodawanie kontrolek do panelu kontrolnego
                controlPanel.Controls.Add(loadButton, 0, 0);
                controlPanel.Controls.Add(libraryGroupBox, 0, 1);
                controlPanel.Controls.Add(threadLabel, 0, 2);
                controlPanel.Controls.Add(threadsTrackBar, 0, 3);
                controlPanel.Controls.Add(threadCountLabel, 0, 4);
                controlPanel.Controls.Add(brightnessLabel, 0, 5);
                controlPanel.Controls.Add(brightnessTrackBar, 0, 6);
                controlPanel.Controls.Add(processButton, 0, 7);
                controlPanel.Controls.Add(saveButton, 0, 8);
                controlPanel.Controls.Add(progressBar, 0, 9);
                controlPanel.Controls.Add(executionTimeLabel, 0, 10);
                controlPanel.Controls.Add(statusLabel, 0, 11);

                // Składanie głównego układu
                mainLayout.Controls.Add(imagePanel, 0, 0);
                mainLayout.Controls.Add(controlPanel, 1, 0);

                // Dodawanie głównego układu do formularza
                this.Controls.Add(mainLayout);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas inicjalizacji interfejsu: {ex.Message}",
                              "Błąd",
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Error);
            }
        }
    }
}