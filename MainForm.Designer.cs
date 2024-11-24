using System;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Filtr_czarno_biały
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

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
            this.components = new System.ComponentModel.Container();
            this.Text = "Filtr czarno-biały";
            this.Size = new System.Drawing.Size(1200, 800);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;

            // Główny układ
            TableLayoutPanel mainLayout = new System.Windows.Forms.TableLayoutPanel();
            mainLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            mainLayout.ColumnCount = 2;
            mainLayout.RowCount = 1;
            mainLayout.Padding = new System.Windows.Forms.Padding(10);
            mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 75F));
            mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));

            // Panel obrazów
            this.imagePanel = new System.Windows.Forms.TableLayoutPanel();
            this.imagePanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.imagePanel.ColumnCount = 2;
            this.imagePanel.RowCount = 1;
            this.imagePanel.Padding = new System.Windows.Forms.Padding(5);
            this.imagePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.imagePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));

            // PictureBox dla obrazów
            this.originalPictureBox = new System.Windows.Forms.PictureBox();
            this.originalPictureBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.originalPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.originalPictureBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;

            this.processedPictureBox = new System.Windows.Forms.PictureBox();
            this.processedPictureBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.processedPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.processedPictureBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;

            // Panel kontrolny
            System.Windows.Forms.TableLayoutPanel controlPanel = new System.Windows.Forms.TableLayoutPanel();
            controlPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            controlPanel.ColumnCount = 1;
            controlPanel.RowCount = 11;  // Zwiększone dla GroupBox wyboru biblioteki
            controlPanel.Padding = new System.Windows.Forms.Padding(5);

            // Ustawienie równych odstępów między kontrolkami w panelu kontrolnym
            for (int i = 0; i < 11; i++)
            {
                controlPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            }

            // Przycisk wczytywania
            System.Windows.Forms.Button loadButton = new System.Windows.Forms.Button();
            loadButton.Text = "Wczytaj obraz";
            loadButton.Dock = System.Windows.Forms.DockStyle.Fill;
            loadButton.Height = 40;
            loadButton.Margin = new Padding(3, 3, 3, 10);
            loadButton.Click += new System.EventHandler(this.LoadButton_Click);

            // W InitializeComponent()

            // W metodzie InitializeComponent():

            // GroupBox wyboru biblioteki
            this.libraryGroupBox = new System.Windows.Forms.GroupBox();
            this.asmRadioButton = new System.Windows.Forms.RadioButton();
            this.csRadioButton = new System.Windows.Forms.RadioButton();

            // Konfiguracja GroupBox
            this.libraryGroupBox.Text = "Wybór biblioteki";
            this.libraryGroupBox.Size = new Size(200, 70);
            this.libraryGroupBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.libraryGroupBox.Margin = new Padding(3, 3, 3, 10);
            this.libraryGroupBox.Height = 70;

            // Konfiguracja RadioButtons
            this.asmRadioButton.Text = "ASM x64";
            this.asmRadioButton.Location = new Point(10, 20);
            this.asmRadioButton.Size = new Size(150, 20);
            this.asmRadioButton.Checked = true;

            this.csRadioButton.Text = "C#";
            this.csRadioButton.Location = new Point(10, 40);
            this.csRadioButton.Size = new Size(150, 20);

            // Dodanie RadioButtons do GroupBox
            this.libraryGroupBox.Controls.Add(this.csRadioButton);
            this.libraryGroupBox.Controls.Add(this.asmRadioButton);

            // A przy dodawaniu do controlPanel:
            controlPanel.Controls.Add(loadButton, 0, 0);
            controlPanel.Controls.Add(this.libraryGroupBox, 0, 1);

            // Modyfikacja rozmiaru wiersza dla GroupBox w controlPanel
            controlPanel.RowStyles[1] = new RowStyle(SizeType.Absolute, 80F);

            // Label liczby wątków
            System.Windows.Forms.Label threadLabel = new System.Windows.Forms.Label();
            threadLabel.Text = "Liczba wątków:";
            threadLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            threadLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // ComboBox wątków
            this.threadsComboBox = new System.Windows.Forms.ComboBox();
            this.threadsComboBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.threadsComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.threadsComboBox.Margin = new Padding(3, 3, 3, 10);

            // Label jasności
            System.Windows.Forms.Label brightnessLabel = new System.Windows.Forms.Label();
            brightnessLabel.Text = "Jasność:";
            brightnessLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            brightnessLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // TrackBar jasności
            this.brightnessTrackBar = new System.Windows.Forms.TrackBar();
            this.brightnessTrackBar.Minimum = -50;
            this.brightnessTrackBar.Maximum = 50;
            this.brightnessTrackBar.Value = 0;
            this.brightnessTrackBar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.brightnessTrackBar.TickFrequency = 25;
            this.brightnessTrackBar.TickStyle = System.Windows.Forms.TickStyle.Both;
            this.brightnessTrackBar.Margin = new Padding(3, 3, 3, 10);
            this.brightnessTrackBar.ValueChanged += new System.EventHandler(this.BrightnessTrackBar_ValueChanged);

            // Przycisk przetwarzania
            this.processButton = new System.Windows.Forms.Button();
            this.processButton.Text = "Wykonaj testy wydajności";
            this.processButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.processButton.Height = 40;
            this.processButton.Enabled = false;
            this.processButton.Margin = new Padding(3, 3, 3, 10);
            this.processButton.Click += new System.EventHandler(this.ProcessButton_Click);

            // Przycisk zapisu
            this.saveButton = new System.Windows.Forms.Button();
            this.saveButton.Text = "Zapisz obraz";
            this.saveButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.saveButton.Height = 40;
            this.saveButton.Enabled = false;
            this.saveButton.Margin = new Padding(3, 3, 3, 10);
            this.saveButton.Click += new System.EventHandler(this.SaveButton_Click);

            // Progress Bar
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.progressBar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.progressBar.Height = 20;
            this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.progressBar.Margin = new Padding(3, 3, 3, 10);

            // Label czasu wykonania
            this.executionTimeLabel = new System.Windows.Forms.Label();
            this.executionTimeLabel.Text = "Czas wykonania: -";
            this.executionTimeLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.executionTimeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.executionTimeLabel.Margin = new Padding(3, 3, 3, 5);

            // Label statusu
            this.statusLabel = new System.Windows.Forms.Label();
            this.statusLabel.Text = "Status: Gotowy";
            this.statusLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // Dodawanie kontrolek do paneli
            this.imagePanel.Controls.Add(this.originalPictureBox, 0, 0);
            this.imagePanel.Controls.Add(this.processedPictureBox, 1, 0);

            // Dodawanie kontrolek do panelu kontrolnego
            controlPanel.Controls.Add(loadButton, 0, 0);
            controlPanel.Controls.Add(this.libraryGroupBox, 0, 1);
            controlPanel.Controls.Add(threadLabel, 0, 2);
            controlPanel.Controls.Add(this.threadsComboBox, 0, 3);
            controlPanel.Controls.Add(brightnessLabel, 0, 4);
            controlPanel.Controls.Add(this.brightnessTrackBar, 0, 5);
            controlPanel.Controls.Add(this.processButton, 0, 6);
            controlPanel.Controls.Add(this.saveButton, 0, 7);
            controlPanel.Controls.Add(this.progressBar, 0, 8);
            controlPanel.Controls.Add(this.executionTimeLabel, 0, 9);
            controlPanel.Controls.Add(this.statusLabel, 0, 10);

            mainLayout.Controls.Add(this.imagePanel, 0, 0);
            mainLayout.Controls.Add(controlPanel, 1, 0);

            this.Controls.Add(mainLayout);
        }

        private System.Windows.Forms.TableLayoutPanel imagePanel;
        private System.Windows.Forms.PictureBox originalPictureBox;
        private System.Windows.Forms.PictureBox processedPictureBox;
        private System.Windows.Forms.ComboBox threadsComboBox;
        private System.Windows.Forms.TrackBar brightnessTrackBar;
        private System.Windows.Forms.Label executionTimeLabel;
        private System.Windows.Forms.Button processButton;
        private System.Windows.Forms.Button saveButton;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.GroupBox libraryGroupBox;
        private System.Windows.Forms.RadioButton asmRadioButton;
        private System.Windows.Forms.RadioButton csRadioButton;
    }
}