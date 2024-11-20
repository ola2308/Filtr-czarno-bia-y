using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace FiltrCzarnoBialy
{
    public class Program
    {
        [DllImport(@"C:\Users\olani\source\repos\Filtr czarno-biały\x64\Debug\ASM.dll")]
        public static extern void GrayscaleFilter(IntPtr inputPixels, IntPtr outputPixels, int pixelCount);

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public class MainForm : Form
    {
        private PictureBox originalPictureBox;
        private PictureBox processedPictureBox;
        private Label instructionLabel;

        public MainForm()
        {
            // Ustawienia okna
            this.Text = "Filtr Czarno-Biały";
            this.Width = 1000;
            this.Height = 600;
            this.AllowDrop = true; // Włącz obsługę przeciągania i upuszczania

            // Instrukcja
            instructionLabel = new Label
            {
                Text = "Przeciągnij i upuść obraz tutaj.",
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Arial", 12)
            };
            this.Controls.Add(instructionLabel);

            // Pole do wyświetlania oryginalnego obrazu
            originalPictureBox = new PictureBox
            {
                BorderStyle = BorderStyle.FixedSingle,
                Width = this.Width / 2 - 20,
                Height = this.Height - 100,
                Left = 10,
                Top = 50,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            this.Controls.Add(originalPictureBox);

            // Pole do wyświetlania przetworzonego obrazu
            processedPictureBox = new PictureBox
            {
                BorderStyle = BorderStyle.FixedSingle,
                Width = this.Width / 2 - 20,
                Height = this.Height - 100,
                Left = this.Width / 2 + 10,
                Top = 50,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            this.Controls.Add(processedPictureBox);

            // Obsługa przeciągania i upuszczania
            this.DragEnter += MainForm_DragEnter;
            this.DragDrop += MainForm_DragDrop;
        }

        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            if (files.Length > 0)
            {
                string inputPath = files[0];
                string outputPath = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(inputPath),
                    "output.jpg"
                );

                try
                {
                    // Wyświetl oryginalny obraz
                    originalPictureBox.Image = Image.FromFile(inputPath);

                    // Przetwórz obraz
                    RunGrayscaleFilter(inputPath, outputPath);

                    // Wyświetl przetworzony obraz
                    processedPictureBox.Image = Image.FromFile(outputPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Wystąpił błąd: {ex.Message}", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void RunGrayscaleFilter(string inputPath, string outputPath)
        {
            using (Bitmap inputBitmap = new Bitmap(inputPath))
            using (Bitmap outputBitmap = new Bitmap(inputBitmap.Width, inputBitmap.Height, PixelFormat.Format24bppRgb))
            {
                Rectangle rect = new Rectangle(0, 0, inputBitmap.Width, inputBitmap.Height);
                BitmapData inputData = inputBitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

                int pixelCount = inputBitmap.Width * inputBitmap.Height;
                int stride = inputData.Stride;

                IntPtr inputPixels = Marshal.AllocHGlobal(stride * inputBitmap.Height);
                IntPtr outputPixels = Marshal.AllocHGlobal(stride * inputBitmap.Height);

                try
                {
                    byte[] inputArray = new byte[stride * inputBitmap.Height];
                    Marshal.Copy(inputData.Scan0, inputArray, 0, inputArray.Length);
                    Marshal.Copy(inputArray, 0, inputPixels, inputArray.Length);
                    inputBitmap.UnlockBits(inputData);

                    // Wywołaj funkcję DLL
                    Program.GrayscaleFilter(inputPixels, outputPixels, pixelCount);

                    BitmapData outputData = outputBitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
                    byte[] outputArray = new byte[stride * outputBitmap.Height];
                    Marshal.Copy(outputPixels, outputArray, 0, outputArray.Length);
                    Marshal.Copy(outputArray, 0, outputData.Scan0, outputArray.Length);
                    outputBitmap.UnlockBits(outputData);

                    outputBitmap.Save(outputPath, ImageFormat.Jpeg);
                }
                finally
                {
                    Marshal.FreeHGlobal(inputPixels);
                    Marshal.FreeHGlobal(outputPixels);
                }
            }
        }
    }
}
