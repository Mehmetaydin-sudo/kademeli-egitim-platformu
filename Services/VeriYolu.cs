using System;
using System.IO;

namespace KademeliEgitimPlatformu.Services
{
    /// <summary>
    /// Uygulama salt-okunur yapılandırma/şema dosyalarının (AppContext.BaseDirectory)
    /// ve kullanıcıya ait dışa aktarım/günlük dosyalarının (Belgelerim / Masaüstü)
    /// yollarını merkezi olarak yönetir.
    /// Program Files altına kurulumda UnauthorizedAccessException oluşmaması için
    /// hiçbir çalışma zamanı çıktısı uygulama dizinine yazılmaz.
    /// </summary>
    public static class VeriYolu
    {
        private const string UygulamaAdi = "KademeliEgitimPlatformu";
        private static string? _veriKlasoru;

        /// <summary>
        /// Dışa aktarım (.xlsx / .csv), hata günlüğü (hata.log) ve geçici dosyaların
        /// tutulacağı kullanıcıya özel yazılabilir klasör (Belgelerim\KademeliEgitimPlatformu).
        /// </summary>
        public static string VeriKlasoru => _veriKlasoru ??= KullaniciYazilabilirKlasorunuBelirle();

        /// <summary>Uygulama dizinindeki salt-okunur yapılandırma dosyası (appsettings.json) yolu.</summary>
        public static string AyarDosyasiYolu =>
            Path.Combine(AppContext.BaseDirectory, "appsettings.json");

        /// <summary>Uygulama dizinindeki salt-okunur örnek yapılandırma dosyası (appsettings.example.json) yolu.</summary>
        public static string OrnekAyarDosyasiYolu =>
            Path.Combine(AppContext.BaseDirectory, "appsettings.example.json");

        /// <summary>Uygulama dizinindeki salt-okunur PostgreSQL şema dosyası (database/schema.sql) yolu.</summary>
        public static string SemaDosyasiYolu =>
            Path.Combine(AppContext.BaseDirectory, "database", "schema.sql");

        /// <summary>
        /// Göreli veya salt-okunur (Program Files) bir çıktı dosya yolunu güvenli şekilde
        /// kullanıcının Belgelerim\KademeliEgitimPlatformu klasörüne yönlendirir.
        /// </summary>
        public static string GuvenliCiktiYolu(string dosyaYolu)
        {
            if (string.IsNullOrWhiteSpace(dosyaYolu))
                throw new ArgumentException("Çıktı dosya yolu boş olamaz.", nameof(dosyaYolu));

            // Yalnızca dosya adı verildiyse doğrudan Belgelerim\KademeliEgitimPlatformu altına yönlendir.
            if (!Path.IsPathRooted(dosyaYolu) && string.IsNullOrEmpty(Path.GetDirectoryName(dosyaYolu)))
            {
                return Path.Combine(VeriKlasoru, Path.GetFileName(dosyaYolu));
            }

            string tamYol = Path.GetFullPath(dosyaYolu);
            string? hedefKlasor = Path.GetDirectoryName(tamYol);

            // Hedef klasör uygulama dizini (Program Files) içindeyse ve yazılamıyorsa Belgelerim'e yönlendir.
            if (!string.IsNullOrEmpty(hedefKlasor) && !YazilabilirMi(hedefKlasor))
            {
                return Path.Combine(VeriKlasoru, Path.GetFileName(tamYol));
            }

            return tamYol;
        }

        private static string KullaniciYazilabilirKlasorunuBelirle()
        {
            string[] adayKokler =
            {
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                Path.GetTempPath()
            };

            foreach (string kok in adayKokler)
            {
                if (string.IsNullOrWhiteSpace(kok))
                    continue;

                string aday = Path.Combine(kok, UygulamaAdi);
                if (YazilabilirMi(aday))
                    return aday;
            }

            return Path.GetTempPath();
        }

        public static bool YazilabilirMi(string klasor)
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
