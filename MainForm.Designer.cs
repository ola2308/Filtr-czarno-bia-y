using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Filtr_czarno_biały
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        // Deklaracje kontrolek
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
        private ComboBox threadsComboBox;
        private Label brightnessLabel;
        private TrackBar brightnessTrackBar;
        private Button processButton;
        private Button saveButton;
        private ProgressBar progressBar;
        private Label executionTimeLabel;
        private Label statusLabel;

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
                // Inicjalizacja komponentów
                components = new System.ComponentModel.Container();

                // Wczytanie ikon
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                Bitmap loadIcon = null;
                Bitmap processIcon = null;
                Bitmap saveIcon = null;

                try
                {
                    using (var tempBmp = new Bitmap(Path.Combine(baseDirectory, "load_icon.png")))
                        loadIcon = new Bitmap(tempBmp);
                    using (var tempBmp = new Bitmap(Path.Combine(baseDirectory, "process_icon.png")))
                        processIcon = new Bitmap(tempBmp);
                    using (var tempBmp = new Bitmap(Path.Combine(baseDirectory, "save_icon.png")))
                        saveIcon = new Bitmap(tempBmp);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Nie można załadować ikon: {ex.Message}",
                                   "Ostrzeżenie",
                                   MessageBoxButtons.OK,
                                   MessageBoxIcon.Warning);
                }

                // Ustawienia formularza
                this.Text = "Filtr czarno-biały";
                this.Size = new System.Drawing.Size(1200, 800);
                this.StartPosition = FormStartPosition.CenterScreen;
                this.Icon = Properties.Resources.app_icon;

                // Główny układ
                mainLayout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 2,
                    RowCount = 1,
                    Padding = new Padding(10)
                };
                mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 75F));
                mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

                // Panel obrazów
                imagePanel = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 2,
                    RowCount = 1,
                    Padding = new Padding(5)
                };
                imagePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                imagePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

                // PictureBox dla oryginalnego obrazu
                originalPictureBox = new PictureBox
                {
                    Dock = DockStyle.Fill,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BorderStyle = BorderStyle.FixedSingle
                };

                // PictureBox dla przetworzonego obrazu
                processedPictureBox = new PictureBox
                {
                    Dock = DockStyle.Fill,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BorderStyle = BorderStyle.FixedSingle
                };

                // Panel kontrolny
                controlPanel = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 1,
                    RowCount = 11,
                    Padding = new Padding(5)
                };

                for (int i = 0; i < 11; i++)
                {
                    controlPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
                }
                controlPanel.RowStyles[1] = new RowStyle(SizeType.Absolute, 80F);

                // Przycisk wczytywania
                loadButton = new Button
                {
                    Text = "Wczytaj obraz",
                    Image = loadIcon,
                    Dock = DockStyle.Fill,
                    Height = 40,
                    Padding = new Padding(10, 0, 0, 0),
                    Margin = new Padding(3, 3, 3, 10),
                    ImageAlign = ContentAlignment.MiddleLeft,
                    TextAlign = ContentAlignment.MiddleRight,
                    TextImageRelation = TextImageRelation.ImageBeforeText
                };
                loadButton.Click += new EventHandler(LoadButton_Click);

                // GroupBox wyboru biblioteki
                libraryGroupBox = new GroupBox
                {
                    Text = "Wybór biblioteki",
                    Dock = DockStyle.Fill,
                    Margin = new Padding(3, 3, 3, 10)
                };

                // RadioButton dla ASM
                asmRadioButton = new RadioButton
                {
                    Text = "ASM x64",
                    Location = new Point(10, 20),
                    Size = new Size(150, 20),
                    Checked = true
                };
                asmRadioButton.CheckedChanged += new EventHandler(LibraryRadioButton_CheckedChanged);

                // RadioButton dla C#
                csRadioButton = new RadioButton
                {
                    Text = "C#",
                    Location = new Point(10, 40),
                    Size = new Size(150, 20)
                };
                csRadioButton.CheckedChanged += new EventHandler(LibraryRadioButton_CheckedChanged);

                libraryGroupBox.Controls.Add(asmRadioButton);
                libraryGroupBox.Controls.Add(csRadioButton);

                // Etykieta liczby wątków
                threadLabel = new Label
                {
                    Text = "Liczba wątków:",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft
                };

                // ComboBox wątków
                threadsComboBox = new ComboBox
                {
                    Dock = DockStyle.Fill,
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Margin = new Padding(3, 3, 3, 10)
                };

                // Etykieta jasności
                brightnessLabel = new Label
                {
                    Text = "Jasność:",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft
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
                    Image = processIcon,
                    Dock = DockStyle.Fill,
                    Height = 40,
                    Padding = new Padding(10, 0, 0, 0),
                    Margin = new Padding(3, 3, 3, 10),
                    Enabled = false,
                    ImageAlign = ContentAlignment.MiddleLeft,
                    TextAlign = ContentAlignment.MiddleRight,
                    TextImageRelation = TextImageRelation.ImageBeforeText
                };
                processButton.Click += new EventHandler(ProcessButton_Click);

                // Przycisk zapisu
                saveButton = new Button
                {
                    Text = "Zapisz obraz",
                    Image = saveIcon,
                    Dock = DockStyle.Fill,
                    Height = 40,
                    Padding = new Padding(10, 0, 0, 0),
                    Margin = new Padding(3, 3, 3, 10),
                    Enabled = false,
                    ImageAlign = ContentAlignment.MiddleLeft,
                    TextAlign = ContentAlignment.MiddleRight,
                    TextImageRelation = TextImageRelation.ImageBeforeText
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
                    Margin = new Padding(3, 3, 3, 5)
                };

                // Etykieta statusu
                statusLabel = new Label
                {
                    Text = "Status: Gotowy",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft
                };

                // Dodawanie kontrolek do paneli
                imagePanel.Controls.Add(originalPictureBox, 0, 0);
                imagePanel.Controls.Add(processedPictureBox, 1, 0);

                // Dodawanie kontrolek do panelu kontrolnego
                controlPanel.Controls.Add(loadButton, 0, 0);
                controlPanel.Controls.Add(libraryGroupBox, 0, 1);
                controlPanel.Controls.Add(threadLabel, 0, 2);
                controlPanel.Controls.Add(threadsComboBox, 0, 3);
                controlPanel.Controls.Add(brightnessLabel, 0, 4);
                controlPanel.Controls.Add(brightnessTrackBar, 0, 5);
                controlPanel.Controls.Add(processButton, 0, 6);
                controlPanel.Controls.Add(saveButton, 0, 7);
                controlPanel.Controls.Add(progressBar, 0, 8);
                controlPanel.Controls.Add(executionTimeLabel, 0, 9);
                controlPanel.Controls.Add(statusLabel, 0, 10);

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