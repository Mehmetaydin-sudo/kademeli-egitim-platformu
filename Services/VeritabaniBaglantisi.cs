using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace KademeliEgitimPlatformu.Services
{
    /// <summary>
    /// PostgreSQL 18 (kademeli_egitim) veritabanı bağlantılarını ve otomatik şema kurulumunu (Auto-Migration)
    /// merkezi olarak yönetir. Bağlantı dizesini AppContext.BaseDirectory altındaki appsettings.json içinden
    /// "ConnectionStrings:DefaultConnection" anahtarıyla Microsoft.Extensions.Configuration.Json ile okur.
    /// </summary>
    public static class VeritabaniBaglantisi
    {
        private const string VarsayilanBaglantiAdi = "DefaultConnection";
        private static readonly object Kilit = new();
        private static bool _semaHazir;

        /// <summary>database/schema.sql içinde yer alan ve uygulamada zorunlu olan 7 PostgreSQL tablosu.</summary>
        public static IReadOnlyList<string> ZorunluTablolar { get; } = new[]
        {
            "kullanicilar",
            "sorular",
            "kullanici_ilerleme",
            "ilerleme",
            "seviye_gecis_sinavlari",
            "yanlis_cevaplar",
            "meta"
        };

        /// <summary>
        /// AppContext.BaseDirectory üzerinden appsettings.json dosyasındaki DefaultConnection bağlantı dizesini okur.
        /// Özel bir bağlantı dizesi verilirse (ör. test ortamı) doğrudan onu döndürür.
        /// </summary>
        public static string BaglantiDizesiniAl(string? ozelBaglantiDizesi = null)
        {
            if (!string.IsNullOrWhiteSpace(ozelBaglantiDizesi))
                return ozelBaglantiDizesi;

            string? ortamBaglantisi = Environment.GetEnvironmentVariable("KEP_POSTGRES_CONNECTION");
            if (!string.IsNullOrWhiteSpace(ortamBaglantisi))
                return ortamBaglantisi;

            string ayarKlasoru = AyarKlasorunuBul();
            string ayarDosyasi = Path.Combine(ayarKlasoru, "appsettings.json");
            string ornekAyarDosyasi = Path.Combine(ayarKlasoru, "appsettings.example.json");

            if (!File.Exists(ayarDosyasi) && !File.Exists(ornekAyarDosyasi))
            {
                throw new InvalidOperationException(
                    $"Uygulama dizininde ({AppContext.BaseDirectory}) 'appsettings.json' yapılandırma dosyası bulunamadı. " +
                    "Lütfen 'appsettings.example.json' dosyasını 'appsettings.json' adıyla kopyalayıp PostgreSQL bağlantı bilgilerinizi girin.");
            }

            IConfigurationRoot yapilandirma;
            try
            {
                var builder = new ConfigurationBuilder()
                    .SetBasePath(ayarKlasoru)
                    .AddJsonFile("appsettings.example.json", optional: true, reloadOnChange: false)
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);

                yapilandirma = builder.Build();
            }
            catch (Exception ex) when (ex is InvalidDataException || ex is FormatException || ex is JsonException)
            {
                throw new InvalidOperationException(
                    "'appsettings.json' yapılandırma dosyası bozuk veya geçersiz JSON biçimi içeriyor. " +
                    "Lütfen dosya içeriğini ve tırnak/virgül işaretlerini kontrol edin.",
                    ex);
            }

            string? baglantiDizesi = yapilandirma.GetConnectionString(VarsayilanBaglantiAdi);
            if (string.IsNullOrWhiteSpace(baglantiDizesi))
            {
                throw new InvalidOperationException(
                    "Veritabanı bağlantı dizesi ('ConnectionStrings:DefaultConnection') " +
                    "appsettings.json içinde bulunamadı veya boş bırakılmış.");
            }

            return baglantiDizesi;
        }

        /// <summary>
        /// Yeni bir NpgsqlConnection açar ve döndürür.
        /// Çağıran kod bağlantıyı mutlaka 'using' bloğu içinde kullanmalıdır.
        /// </summary>
        public static NpgsqlConnection Ac(string? ozelBaglantiDizesi = null)
        {
            string baglantiMetni = BaglantiDizesiniAl(ozelBaglantiDizesi);
            var baglanti = new NpgsqlConnection(baglantiMetni);
            try
            {
                baglanti.Open();
                return baglanti;
            }
            catch
            {
                baglanti.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Veritabanında zorunlu tablolardan herhangi birinin eksik olup olmadığını kontrol eder.
        /// </summary>
        public static IReadOnlyList<string> EksikTablolariGetir(NpgsqlConnection baglanti)
        {
            ArgumentNullException.ThrowIfNull(baglanti);

            var mevcutTablolar = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            const string sql = @"
                SELECT table_name
                FROM information_schema.tables
                WHERE table_schema = 'public';";

            using (var komut = new NpgsqlCommand(sql, baglanti))
            using (var okuyucu = komut.ExecuteReader())
            {
                while (okuyucu.Read())
                {
                    mevcutTablolar.Add(okuyucu.GetString(0));
                }
            }

            var eksikler = new List<string>();
            foreach (string tablo in ZorunluTablolar)
            {
                if (!mevcutTablolar.Contains(tablo))
                    eksikler.Add(tablo);
            }

            return eksikler;
        }

        /// <summary>
        /// Sıfır kurulumda 'kademeli_egitim' veritabanı yoksa oluşturmayı dener,
        /// eksik tablo olup olmadığını kontrol eder ve database/schema.sql (CREATE TABLE IF NOT EXISTS ...)
        /// betiğini güvenli şekilde çalıştırarak tüm tabloları hazırlar.
        /// </summary>
        public static void TablolariHazirla(string? ozelBaglantiDizesi = null)
        {
            if (_semaHazir && string.IsNullOrWhiteSpace(ozelBaglantiDizesi))
                return;

            lock (Kilit)
            {
                if (_semaHazir && string.IsNullOrWhiteSpace(ozelBaglantiDizesi))
                    return;

                string baglantiMetni = BaglantiDizesiniAl(ozelBaglantiDizesi);

                NpgsqlConnection? baglanti = null;
                try
                {
                    try
                    {
                        baglanti = new NpgsqlConnection(baglantiMetni);
                        baglanti.Open();
                    }
                    catch (PostgresException pgEx) when (pgEx.SqlState == "3D000")
                    {
                        baglanti?.Dispose();
                        VeritabaniYoksaOlustur(baglantiMetni);
                        baglanti = new NpgsqlConnection(baglantiMetni);
                        baglanti.Open();
                    }

                    IReadOnlyList<string> eksikTablolar = EksikTablolariGetir(baglanti);
                    if (eksikTablolar.Count > 0 || !_semaHazir)
                    {
                        string calistirilacakSema = SemaMetniniYukle();
                        using var komut = new NpgsqlCommand(calistirilacakSema, baglanti);
                        komut.ExecuteNonQuery();
                    }

                    if (string.IsNullOrWhiteSpace(ozelBaglantiDizesi))
                        _semaHazir = true;
                }
                finally
                {
                    baglanti?.Dispose();
                }
            }
        }

        /// <summary>
        /// Veritabanı ve yapılandırma hatalarını son kullanıcının anlayacağı Türkçe açıklamaya dönüştürür.
        /// </summary>
        public static string KullaniciDostuHataMesaji(Exception ex)
        {
            if (ex == null)
                return "Bilinmeyen bir veritabanı hatası oluştu.";

            if (ex is PostgresException pgEx)
            {
                return pgEx.SqlState switch
                {
                    "28P01" or "28000" =>
                        "PostgreSQL kullanıcı adı veya şifresi hatalı.\n\n" +
                        "Çözüm: Uygulama klasöründeki 'appsettings.json' dosyasında yer alan " +
                        "'ConnectionStrings:DefaultConnection' içindeki Password değerini yerel PostgreSQL şifrenizle güncelleyin.",
                    "3D000" =>
                        "PostgreSQL sunucusunda 'kademeli_egitim' veritabanı bulunamadı ve otomatik oluşturulamadı.\n\n" +
                        "Çözüm: pgAdmin veya psql üzerinden 'CREATE DATABASE kademeli_egitim;' komutunu çalıştırın.",
                    _ =>
                        $"PostgreSQL veritabanı işlemi sırasında hata oluştu (Kod: {pgEx.SqlState}).\n\nAyrıntı: {pgEx.MessageText}"
                };
            }

            if (ex is JsonException || ex is InvalidDataException || ex is FormatException ||
                (ex is InvalidOperationException && ex.Message.Contains("appsettings", StringComparison.OrdinalIgnoreCase)))
            {
                return "Uygulama yapılandırma dosyası ('appsettings.json') eksik veya bozuk JSON içeriyor.\n\n" +
                       "Çözüm: Uygulama dizinindeki 'appsettings.example.json' dosyasını 'appsettings.json' olarak kopyalayıp " +
                       "PostgreSQL bağlantı bilgilerinizi (Host, Port, Username, Password) kontrol edin.\n\n" +
                       $"Ayrıntı: {ex.Message}";
            }

            if (ex is NpgsqlException || ex.InnerException is SocketException || ex is SocketException || ex is TimeoutException)
            {
                return "Yerel PostgreSQL 18 servisine bağlanılamadı.\n\n" +
                       "Lütfen şu adımları kontrol edin:\n" +
                       "1) Windows Hizmetler (services.msc) üzerinden 'postgresql-x64-18' servisinin çalıştığından emin olun.\n" +
                       "2) PostgreSQL'in 5432 portunu dinlediğini kontrol edin.\n" +
                       "3) 'appsettings.json' dosyasındaki bağlantı ayarlarının doğru olduğunu doğrulayın.\n\n" +
                       $"Teknik Ayrıntı: {ex.Message}";
            }

            return ex.Message;
        }

        private static void VeritabaniYoksaOlustur(string baglantiMetni)
        {
            var kurucu = new NpgsqlConnectionStringBuilder(baglantiMetni);
            string? hedefVeritabani = kurucu.Database;
            if (string.IsNullOrWhiteSpace(hedefVeritabani))
                return;

            // SQL injection önlemi: yalnızca harf, rakam ve alt çizgi içeren veritabanı adlarına izin ver.
            foreach (char c in hedefVeritabani)
            {
                if (!char.IsLetterOrDigit(c) && c != '_')
                    return;
            }

            kurucu.Database = "postgres";
            using var yonetimBaglantisi = new NpgsqlConnection(kurucu.ConnectionString);
            yonetimBaglantisi.Open();

            using var kontrolKomutu = new NpgsqlCommand(
                "SELECT 1 FROM pg_database WHERE datname = @ad;",
                yonetimBaglantisi);
            kontrolKomutu.Parameters.AddWithValue("@ad", hedefVeritabani);

            object? mevcut = kontrolKomutu.ExecuteScalar();
            if (mevcut == null)
            {
                using var olusturKomutu = new NpgsqlCommand(
                    $"CREATE DATABASE \"{hedefVeritabani}\" WITH ENCODING 'UTF8';",
                    yonetimBaglantisi);
                olusturKomutu.ExecuteNonQuery();
            }
        }

        private static string SemaMetniniYukle()
        {
            string? semaDosyaYolu = SemaDosyasiYolunuBul();
            if (!string.IsNullOrEmpty(semaDosyaYolu) && File.Exists(semaDosyaYolu))
            {
                try
                {
                    string dosyaIcerigi = File.ReadAllText(semaDosyaYolu);
                    if (!string.IsNullOrWhiteSpace(dosyaIcerigi))
                        return dosyaIcerigi;
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    // Dosya okunamazsa gömülü şema yedek olarak kullanılır.
                }
            }

            return SemaSql;
        }

        private static string? SemaDosyasiYolunuBul()
        {
            // Önce doğrudan AppContext.BaseDirectory\database\schema.sql kontrol edilir.
            string dogrudanYol = Path.Combine(AppContext.BaseDirectory, "database", "schema.sql");
            if (File.Exists(dogrudanYol))
                return dogrudanYol;

            DirectoryInfo? dizin = new(AppContext.BaseDirectory);
            while (dizin != null)
            {
                string aday = Path.Combine(dizin.FullName, "database", "schema.sql");
                if (File.Exists(aday))
                    return aday;

                dizin = dizin.Parent;
            }

            return null;
        }

        private static string AyarKlasorunuBul()
        {
            // Salt-okunur yapılandırma dosyaları her zaman AppContext.BaseDirectory tabanlı aranır.
            DirectoryInfo? dizin = new(AppContext.BaseDirectory);
            while (dizin != null)
            {
                if (File.Exists(Path.Combine(dizin.FullName, "appsettings.json")))
                    return dizin.FullName;

                dizin = dizin.Parent;
            }

            dizin = new DirectoryInfo(AppContext.BaseDirectory);
            while (dizin != null)
            {
                if (File.Exists(Path.Combine(dizin.FullName, "appsettings.example.json")))
                    return dizin.FullName;

                dizin = dizin.Parent;
            }

            return AppContext.BaseDirectory;
        }

        private const string SemaSql = @"
CREATE TABLE IF NOT EXISTS kullanicilar (
    id               SERIAL PRIMARY KEY,
    kullanici_adi    VARCHAR(50)  NOT NULL UNIQUE,
    ad_soyad         VARCHAR(100) NOT NULL DEFAULT '',
    sifre_hash       VARCHAR(72)  NOT NULL,
    rol              VARCHAR(20)  NOT NULL DEFAULT 'Ogrenci'
                     CHECK (rol IN ('Ogrenci', 'Egitmen', 'Yonetici')),
    aktif_mi         BOOLEAN      NOT NULL DEFAULT TRUE,
    olusturma_tarihi TIMESTAMPTZ  NOT NULL DEFAULT CURRENT_TIMESTAMP,
    son_giris_tarihi TIMESTAMPTZ  NULL
);

CREATE INDEX IF NOT EXISTS ix_kullanicilar_kullanici_adi
    ON kullanicilar (kullanici_adi);

CREATE TABLE IF NOT EXISTS sorular (
    id                  SERIAL PRIMARY KEY,
    disiplin            SMALLINT     NOT NULL CHECK (disiplin BETWEEN 0 AND 2),
    zorluk              SMALLINT     NOT NULL CHECK (zorluk BETWEEN 0 AND 2),
    etiket              VARCHAR(50)  NULL,
    baslik              TEXT         NOT NULL UNIQUE,
    kod_blogu           TEXT         NULL,
    secenek_a           TEXT         NOT NULL,
    secenek_b           TEXT         NOT NULL,
    secenek_c           TEXT         NOT NULL,
    secenek_d           TEXT         NOT NULL,
    dogru_secenek_index SMALLINT     NOT NULL CHECK (dogru_secenek_index BETWEEN 0 AND 3),
    aciklama_a          TEXT         NULL,
    aciklama_b          TEXT         NULL,
    aciklama_c          TEXT         NULL,
    aciklama_d          TEXT         NULL,
    olusturma_tarihi    TIMESTAMPTZ  NOT NULL DEFAULT CURRENT_TIMESTAMP,
    guncelleme_tarihi   TIMESTAMPTZ  NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS ix_sorular_disiplin_zorluk
    ON sorular (disiplin, zorluk);

CREATE INDEX IF NOT EXISTS ix_sorular_etiket
    ON sorular (etiket);

CREATE TABLE IF NOT EXISTS kullanici_ilerleme (
    id                          SERIAL PRIMARY KEY,
    kullanici_id                INTEGER      NULL UNIQUE REFERENCES kullanicilar(id) ON DELETE CASCADE,
    gorsel_basari               INTEGER      NOT NULL DEFAULT 0 CHECK (gorsel_basari BETWEEN 0 AND 100),
    veritabani_basari           INTEGER      NOT NULL DEFAULT 0 CHECK (veritabani_basari BETWEEN 0 AND 100),
    siber_basari                INTEGER      NOT NULL DEFAULT 0 CHECK (siber_basari BETWEEN 0 AND 100),
    ipucu_jetonu                INTEGER      NOT NULL DEFAULT 3 CHECK (ipucu_jetonu >= 0),
    hata_avcisi_kazanildi       BOOLEAN      NOT NULL DEFAULT FALSE,
    gorsel_ara_sinav_gecti      BOOLEAN      NOT NULL DEFAULT FALSE,
    veritabani_ara_sinav_gecti  BOOLEAN      NOT NULL DEFAULT FALSE,
    siber_ara_sinav_gecti       BOOLEAN      NOT NULL DEFAULT FALSE,
    kolay_orta_ara_sinav_gecti  BOOLEAN      NOT NULL DEFAULT FALSE,
    orta_zor_ara_sinav_gecti    BOOLEAN      NOT NULL DEFAULT FALSE,
    orta_mod_gecti              BOOLEAN      NOT NULL DEFAULT FALSE,
    aktif_kademe                SMALLINT     NOT NULL DEFAULT 0 CHECK (aktif_kademe BETWEEN 0 AND 2),
    son_guncelleme              TIMESTAMPTZ  NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS ilerleme (
    anahtar VARCHAR(100) PRIMARY KEY,
    deger   INTEGER      NOT NULL
);

CREATE TABLE IF NOT EXISTS seviye_gecis_sinavlari (
    id                SERIAL PRIMARY KEY,
    kullanici_id      INTEGER      NULL REFERENCES kullanicilar(id) ON DELETE SET NULL,
    gecis_turu        VARCHAR(30)  NOT NULL
                      CHECK (gecis_turu IN ('KolaydanOrtaya', 'OrtadanZora', 'ModulAraSinav', 'NormalSinav')),
    disiplin          SMALLINT     NULL CHECK (disiplin IS NULL OR (disiplin BETWEEN 0 AND 2)),
    hedef_zorluk      SMALLINT     NOT NULL CHECK (hedef_zorluk BETWEEN 0 AND 2),
    toplam_soru       INTEGER      NOT NULL CHECK (toplam_soru > 0),
    dogru_sayisi      INTEGER      NOT NULL CHECK (dogru_sayisi >= 0 AND dogru_sayisi <= toplam_soru),
    basari_yuzdesi    INTEGER      NOT NULL CHECK (basari_yuzdesi BETWEEN 0 AND 100),
    baraj_yuzdesi     INTEGER      NOT NULL DEFAULT 70 CHECK (baraj_yuzdesi BETWEEN 0 AND 100),
    basarili_mi       BOOLEAN      NOT NULL,
    gecen_sure_saniye INTEGER      NOT NULL DEFAULT 0 CHECK (gecen_sure_saniye >= 0),
    sinav_tarihi      TIMESTAMPTZ  NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS ix_seviye_gecis_sinavlari_kullanici_tarih
    ON seviye_gecis_sinavlari (kullanici_id, sinav_tarihi DESC);

CREATE TABLE IF NOT EXISTS yanlis_cevaplar (
    id            SERIAL PRIMARY KEY,
    kullanici_id  INTEGER      NULL REFERENCES kullanicilar(id) ON DELETE CASCADE,
    soru_id       INTEGER      NULL REFERENCES sorular(id) ON DELETE SET NULL,
    tarih         TIMESTAMPTZ  NOT NULL DEFAULT CURRENT_TIMESTAMP,
    disiplin      SMALLINT     NOT NULL CHECK (disiplin BETWEEN 0 AND 2),
    zorluk        SMALLINT     NOT NULL CHECK (zorluk BETWEEN 0 AND 2),
    baslik        TEXT         NOT NULL,
    secilen_cevap TEXT         NOT NULL,
    dogru_cevap   TEXT         NOT NULL,
    aciklama      TEXT         NOT NULL DEFAULT ''
);

CREATE INDEX IF NOT EXISTS ix_yanlis_cevaplar_tarih
    ON yanlis_cevaplar (tarih DESC, id DESC);

CREATE TABLE IF NOT EXISTS meta (
    anahtar VARCHAR(100) PRIMARY KEY,
    deger   TEXT         NOT NULL
);

INSERT INTO meta (anahtar, deger)
VALUES ('semaSurumu', '3')
ON CONFLICT (anahtar) DO NOTHING;";
    }
}
