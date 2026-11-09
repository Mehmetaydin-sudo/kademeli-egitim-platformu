using System;
using System.IO;
using System.Windows.Forms;
using KademeliEgitimPlatformu.Services;

namespace KademeliEgitimPlatformu
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            // Beklenmeyen hatalarda uygulamanın sessizce kapanması yerine
            // kullanıcıya anlaşılır bir mesaj göster ve hatayı günlüğe yaz.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) => HataGoster(e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (s, e) => HataGoster(e.ExceptionObject as Exception);

            try
            {
                Application.Run(new Form1());
            }
            catch (Exception ex)
            {
                HataGoster(ex);
            }
        }

        private static void HataGoster(Exception? ex)
        {
            string ayrinti = ex?.ToString() ?? "Bilinmeyen hata";

            try
            {
                string gunlukYolu = Path.Combine(VeriYolu.VeriKlasoru, "hata.log");
                File.AppendAllText(gunlukYolu,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ayrinti}{Environment.NewLine}{Environment.NewLine}");
            }
            catch
            {
                // Günlük yazılamasa bile kullanıcıya mesaj gösterilir.
            }

            MessageBox.Show(
                "Beklenmeyen bir hata oluştu.\n\n" +
                (ex?.Message ?? "Bilinmeyen hata") +
                "\n\nAyrıntılar hata.log dosyasına yazıldı.",
                "Kademeli Eğitim Platformu — Hata",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
