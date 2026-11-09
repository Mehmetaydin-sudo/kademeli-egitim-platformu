using System;
using System.IO;

namespace KademeliEgitimPlatformu.Services
{
    /// <summary>
    /// Uygulama verilerinin (SQLite dosyaları) yazılacağı klasörü belirler.
    /// Önce uygulamanın yanındaki Data klasörü denenir (taşınabilir / USB kullanımı).
    /// Klasör yazılamıyorsa (Program Files, salt okunur medya) %LOCALAPPDATA% altına geçilir.
    /// Böylece uygulama yönetici izni olmadan da açılabilir.
    /// </summary>
    public static class VeriYolu
    {
        private const string UygulamaAdi = "KademeliEgitimPlatformu";
        private static string? _veriKlasoru;

        /// <summary>SQLite dosyalarının tutulacağı yazılabilir klasör.</summary>
        public static string VeriKlasoru => _veriKlasoru ??= KlasoruBelirle();

        /// <summary>Kurulumla birlikte gelen soru havuzu (salt okunur kaynak).</summary>
        public static string SorularJsonYolu =>
            Path.Combine(AppContext.BaseDirectory, "Data", "Sorular.json");

        public static string SorularVeritabaniYolu => Path.Combine(VeriKlasoru, "sorular.db");

        public static string IlerlemeVeritabaniYolu => Path.Combine(VeriKlasoru, "ilerleme.db");

        private static string KlasoruBelirle()
        {
            string uygulamaYani = Path.Combine(AppContext.BaseDirectory, "Data");
            if (YazilabilirMi(uygulamaYani))
                return uygulamaYani;

            string yerelKok = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(yerelKok))
                return uygulamaYani; // Son çare: eski davranış

            string yerel = Path.Combine(yerelKok, UygulamaAdi, "Data");
            Directory.CreateDirectory(yerel);
            return yerel;
        }

        private static bool YazilabilirMi(string klasor)
        {
            try
            {
                Directory.CreateDirectory(klasor);
                string deneme = Path.Combine(klasor, $".yazma-testi-{Guid.NewGuid():N}.tmp");
                using (File.Create(deneme, 1, FileOptions.DeleteOnClose)) { }
                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                return false;
            }
        }
    }
}
