using System;
using System.IO;
using System.Windows.Forms;

namespace Filtr_czarno_biały
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                // Logowanie błędu do pliku tekstowego
                File.WriteAllText("error_log.txt", $"Błąd: {ex.Message}\n{ex.StackTrace}");
                // Wyświetlenie komunikatu o błędzie
                MessageBox.Show($"Krytyczny błąd aplikacji: {ex.Message}",
                                "Błąd",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
        }
    }
}
