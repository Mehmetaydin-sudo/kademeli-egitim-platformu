using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace KademeliEgitimPlatformu.Services
{
    /// <summary>
    /// PostgreSQL 18 (kademeli_egitim) veritabanı bağlantılarını merkezi olarak yönetir.
    /// Bağlantı dizesini appsettings.json içindeki "ConnectionStrings:DefaultConnection"
    /// anahtarından Microsoft.Extensions.Configuration.Json ile okur.
    /// </summary>
    public static class VeritabaniBaglantisi
    {
        private const string VarsayilanBaglantiAdi = "DefaultConnection";
        private static readonly object Kilit = new();
        private static bool _semaHazir;

        /// <summary>
        /// appsettings.json dosyasından DefaultConnection bağlantı dizesini okur.
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

            IConfigurationRoot yapilandirma = new ConfigurationBuilder()
                .SetBasePath(ayarKlasoru)
                .AddJsonFile("appsettings.example.json", optional: true, reloadOnChange: false)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .Build();

            string? baglantiDizesi = yapilandirma.GetConnectionString(VarsayilanBaglantiAdi);
            if (string.IsNullOrWhiteSpace(baglantiDizesi))
            {
                throw new InvalidOperationException(
                    "Veritabanı bağlantı dizesi ('ConnectionStrings:DefaultConnection') " +
                    "appsettings.json içinde bulunamadı.");
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
            baglanti.Open();
            return baglanti;
        }

        /// <summary>
        /// PostgreSQL tablolarının (database/schema.sql ile uyumlu) mevcut olduğundan emin olur.
        /// İlk bağlantıda bir kez çalışır; tablolar zaten varsa hiçbir veriyi ezmez.
        /// </summary>
        public static void TablolariHazirla(string? ozelBaglantiDizesi = null)
        {
            if (_semaHazir && string.IsNullOrWhiteSpace(ozelBaglantiDizesi))
                return;

            lock (Kilit)
            {
                if (_semaHazir && string.IsNullOrWhiteSpace(ozelBaglantiDizesi))
                    return;

                using var baglanti = Ac(ozelBaglantiDizesi);
                using var komut = new NpgsqlCommand(SemaSql, baglanti);
                komut.ExecuteNonQuery();

                if (string.IsNullOrWhiteSpace(ozelBaglantiDizesi))
                    _semaHazir = true;
            }
        }

        private static string AyarKlasorunuBul()
        {
            string[] adaylar =
            {
                AppContext.BaseDirectory,
                Directory.GetCurrentDirectory()
            };

            // Önce gerçek appsettings.json dosyasını ara
            foreach (string baslangic in adaylar)
            {
                DirectoryInfo? dizin = new(baslangic);
                while (dizin != null)
                {
                    if (File.Exists(Path.Combine(dizin.FullName, "appsettings.json")))
                        return dizin.FullName;

                    dizin = dizin.Parent;
                }
            }

            // Bulunamazsa şablon appsettings.example.json dosyasını ara
            foreach (string baslangic in adaylar)
            {
                DirectoryInfo? dizin = new(baslangic);
                while (dizin != null)
                {
                    if (File.Exists(Path.Combine(dizin.FullName, "appsettings.example.json")))
                        return dizin.FullName;

                    dizin = dizin.Parent;
                }
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
);";
    }
}
