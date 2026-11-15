using System;
using System.IO;

namespace KademeliEgitimPlatformu.Services
{
    /// <summary>
    /// Uygulama dışa aktarım ve geçici dosyalarının yazılacağı klasörü belirler.
    /// Veritabanı işlemleri yerel PostgreSQL 18 (kademeli_egitim) üzerinden yürütülür;
    /// yerel .db dosyaları kullanılmaz.
    /// </summary>
    public static class VeriYolu
    {
        private const string UygulamaAdi = "KademeliEgitimPlatformu";
        private static string? _veriKlasoru;

        /// <summary>Dışa aktarım (.xlsx / .csv) ve günlük dosyalarının tutulacağı yazılabilir klasör.</summary>
        public static string VeriKlasoru => _veriKlasoru ??= KlasoruBelirle();

        /// <summary>Uygulama yapılandırma dosyası (appsettings.json) yolu.</summary>
        public static string AyarDosyasiYolu =>
            Path.Combine(AppContext.BaseDirectory, "appsettings.json");

        private static string KlasoruBelirle()
        {
            string uygulamaYani = Path.Combine(AppContext.BaseDirectory, "Data");
            if (YazilabilirMi(uygulamaYani))
                return uygulamaYani;

            string yerelKok = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(yerelKok))
                return uygulamaYani;

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
