using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using KademeliEgitimPlatformu.Models;
using Microsoft.Data.Sqlite;

namespace KademeliEgitimPlatformu.Services
{
    /// <summary>
    /// Soru havuzunu yerel SQLite dosyasında (Data/sorular.db) tutar.
    /// Her açılışta Data/Sorular.json içindeki sorular veritabanıyla senkronize edilir:
    /// yeni sorular eklenir, başlığı aynı olan sorular güncellenir.
    /// Tablo: Sorular(Id, Baslik UNIQUE, Disiplin, Zorluk, Veri)
    ///   - Disiplin/Zorluk: filtreleme için ayrı, indeksli sütunlar
    ///   - Veri: sorunun tamamı (şıklar, kod bloğu, yanlış cevap açıklamaları) JSON olarak
    /// </summary>
    public class SoruServisi
    {
        private const long MaksPaketBoyutu = 5 * 1024 * 1024; // 5 MB
        private const string JsonHashAnahtari = "jsonHash";

        private static readonly JsonSerializerOptions JsonSecenekleri = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            Converters = { new JsonStringEnumConverter() }
        };

        private readonly string _jsonYolu;
        private readonly string _baglantiMetni;

        /// <summary>Son DisaridanPaketYukle çağrısında geçersiz olduğu için atlanan soru sayısı.</summary>
        public int SonPaketAtlananSayisi { get; private set; }

        public SoruServisi(string? jsonYolu = null, string? veritabaniYolu = null)
        {
            _jsonYolu = jsonYolu ?? VeriYolu.SorularJsonYolu;
            string dbYolu = veritabaniYolu ?? VeriYolu.SorularVeritabaniYolu;

            string? dbKlasoru = Path.GetDirectoryName(dbYolu);
            if (!string.IsNullOrEmpty(dbKlasoru))
                Directory.CreateDirectory(dbKlasoru);

            _baglantiMetni = new SqliteConnectionStringBuilder
            {
                DataSource = dbYolu
            }.ToString();

            TabloyuHazirla();
            JsondanSenkronizeEt();
        }

        // ==================== OKUMA ====================
        /// <summary>
        /// Yalnızca soru sayısını döndürür. Ana menüde tüm soruları okuyup
        /// JSON'dan çözmek yerine SQLite'ın COUNT(*) sorgusu kullanılır.
        /// </summary>
        public int SoruSayisi()
        {
            using var baglanti = SqliteYardimcisi.Ac(_baglantiMetni);
            using var komut = baglanti.CreateCommand();
            komut.CommandText = "SELECT COUNT(*) FROM Sorular;";
            return Convert.ToInt32(komut.ExecuteScalar());
        }

        public List<Soru> TumSorulariGetir(ZorlukSeviyesi? zorluk = null)
        {
            using var baglanti = SqliteYardimcisi.Ac(_baglantiMetni);
            using var komut = baglanti.CreateCommand();

            komut.CommandText = "SELECT Id, Veri FROM Sorular"
                              + (zorluk.HasValue ? " WHERE Zorluk = $zorluk" : "")
                              + " ORDER BY Id;";
            if (zorluk.HasValue)
                komut.Parameters.AddWithValue("$zorluk", (int)zorluk.Value);

            return Oku(komut);
        }

        public List<Soru> SorulariFiltrele(DisiplinTuru disiplin, ZorlukSeviyesi? zorluk = null)
        {
            using var baglanti = SqliteYardimcisi.Ac(_baglantiMetni);
            using var komut = baglanti.CreateCommand();

            komut.CommandText = "SELECT Id, Veri FROM Sorular WHERE Disiplin = $disiplin"
                              + (zorluk.HasValue ? " AND Zorluk = $zorluk" : "")
                              + " ORDER BY Id;";
            komut.Parameters.AddWithValue("$disiplin", (int)disiplin);
            if (zorluk.HasValue)
                komut.Parameters.AddWithValue("$zorluk", (int)zorluk.Value);

            return Oku(komut);
        }

        // ==================== USB / KLASÖRDEN PAKET YÜKLEME ====================
        /// <summary>
        /// Dışarıdan .json soru paketi yükler. Geçersiz sorular atlanır (SonPaketAtlananSayisi),
        /// başlığı zaten havuzda olan sorular tekrar eklenmez. Eklenen soru sayısını döndürür.
        /// </summary>
        public int DisaridanPaketYukle(string secilenDosyaYolu)
        {
            SonPaketAtlananSayisi = 0;

            if (!File.Exists(secilenDosyaYolu))
                throw new FileNotFoundException("Seçilen paket dosyası bulunamadı.", secilenDosyaYolu);

            // Girdiye güvenme: aşırı büyük dosyaları baştan reddet
            if (new FileInfo(secilenDosyaYolu).Length > MaksPaketBoyutu)
                throw new InvalidDataException("Paket dosyası 5 MB sınırını aşıyor.");

            string yeniJson = File.ReadAllText(secilenDosyaYolu);

            List<Soru> yeniSorular;
            try
            {
                yeniSorular = JsonSerializer.Deserialize<List<Soru>>(yeniJson, JsonSecenekleri)
                              ?? new List<Soru>();
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException(
                    "Paket dosyası geçerli bir soru paketi değil (JSON biçimi hatalı).", ex);
            }

            if (yeniSorular.Count == 0)
                return 0;

            int eklenen = 0;

            using var baglanti = SqliteYardimcisi.Ac(_baglantiMetni);
            using var islem = baglanti.BeginTransaction();

            foreach (var soru in yeniSorular)
            {
                if (!SoruGecerliMi(Normallestir(soru)))
                {
                    SonPaketAtlananSayisi++;
                    continue;
                }

                if (SoruEkle(baglanti, islem, soru))
                    eklenen++;
            }

            islem.Commit(); // Ya hepsi yazılır ya hiçbiri
            return eklenen;
        }

        /// <summary>Bozuk/eksik soruların uygulamayı çökertmesini engelleyen doğrulama.</summary>
        public static bool SoruGecerliMi(Soru? s)
        {
            if (s == null || string.IsNullOrWhiteSpace(s.Baslik))
                return false;

            // Tam 4 şık olmalı ve hiçbiri boş olmamalı
            if (s.Secenekler == null || s.Secenekler.Count != 4 || s.Secenekler.Any(string.IsNullOrWhiteSpace))
                return false;

            // Doğru cevap indeksi 0-3 arasında olmalı
            if (s.DogruSecenekIndex < 0 || s.DogruSecenekIndex > 3)
                return false;

            // JSON'dan gelen enum değerleri tanımlı olmalı (örn. Disiplin = 7 geçersiz)
            if (!Enum.IsDefined(typeof(DisiplinTuru), s.Disiplin) || !Enum.IsDefined(typeof(ZorlukSeviyesi), s.Zorluk))
                return false;

            // Yanlış cevap açıklamalarının anahtarları şık indeksi (0-3) olmalı
            if (s.YanlisCevapAciklamalari != null && s.YanlisCevapAciklamalari.Keys.Any(k => k < 0 || k > 3))
                return false;

            return true;
        }

        // ==================== İÇ METOTLAR ====================
        /// <summary>
        /// JSON'dan gelen boş alanları güvenli hale getirir. Özellikle
        /// "YanlisCevapAciklamalari": null gelen paketlerin arayüzde
        /// NullReferenceException üretmesini engeller.
        /// </summary>
        private static Soru Normallestir(Soru soru)
        {
            soru.Baslik = (soru.Baslik ?? string.Empty).Trim();
            soru.Secenekler ??= new List<string>();
            soru.YanlisCevapAciklamalari ??= new Dictionary<int, string>();

            var bosAciklamalar = soru.YanlisCevapAciklamalari
                .Where(kv => string.IsNullOrWhiteSpace(kv.Value))
                .Select(kv => kv.Key)
                .ToList();
            foreach (int anahtar in bosAciklamalar)
                soru.YanlisCevapAciklamalari.Remove(anahtar);

            return soru;
        }

        private void TabloyuHazirla()
        {
            using var baglanti = SqliteYardimcisi.Ac(_baglantiMetni);
            SqliteYardimcisi.WalEtkinlestir(baglanti);

            using var komut = baglanti.CreateCommand();
            komut.CommandText =
                "CREATE TABLE IF NOT EXISTS Sorular (" +
                "  Id       INTEGER PRIMARY KEY AUTOINCREMENT," +
                "  Baslik   TEXT    NOT NULL UNIQUE," +
                "  Disiplin INTEGER NOT NULL," +
                "  Zorluk   INTEGER NOT NULL," +
                "  Veri     TEXT    NOT NULL);" +
                "CREATE INDEX IF NOT EXISTS IX_Sorular_Disiplin_Zorluk ON Sorular (Disiplin, Zorluk);" +
                "CREATE TABLE IF NOT EXISTS Meta (" +
                "  Anahtar TEXT PRIMARY KEY," +
                "  Deger   TEXT NOT NULL);";
            komut.ExecuteNonQuery();
        }

        /// <summary>
        /// Data/Sorular.json içeriğini veritabanıyla senkronize eder.
        /// Dosyanın SHA-256 özeti değişmediyse senkronizasyon tamamen atlanır;
        /// böylece her açılışta gereksiz JSON çözümleme ve yazma yapılmaz.
        /// </summary>
        private void JsondanSenkronizeEt()
        {
            if (!File.Exists(_jsonYolu))
                return;

            try
            {
                string json = File.ReadAllText(_jsonYolu);
                string hash = Convert.ToHexString(
                    SHA256.HashData(Encoding.UTF8.GetBytes(json)));

                using var baglanti = SqliteYardimcisi.Ac(_baglantiMetni);
                if (HashAyniMi(baglanti, hash))
                    return;

                var sorular = JsonSerializer.Deserialize<List<Soru>>(json, JsonSecenekleri)
                              ?? new List<Soru>();

                using (var islem = baglanti.BeginTransaction())
                {
                    foreach (var soru in sorular)
                    {
                        if (SoruGecerliMi(Normallestir(soru)))
                            SoruEkleVeyaGuncelle(baglanti, islem, soru);
                    }
                    islem.Commit();
                }

                HashKaydet(baglanti, hash);
            }
            catch (Exception ex) when (ex is JsonException || ex is IOException || ex is UnauthorizedAccessException)
            {
                // Bozuk veya okunamayan başlangıç dosyası: havuz mevcut haliyle devam eder, uygulama çökmez.
            }
        }

        private static bool HashAyniMi(SqliteConnection baglanti, string hash)
        {
            using var komut = baglanti.CreateCommand();
            komut.CommandText = "SELECT Deger FROM Meta WHERE Anahtar = $anahtar;";
            komut.Parameters.AddWithValue("$anahtar", JsonHashAnahtari);
            return string.Equals(komut.ExecuteScalar() as string, hash, StringComparison.Ordinal);
        }

        private static void HashKaydet(SqliteConnection baglanti, string hash)
        {
            using var komut = baglanti.CreateCommand();
            komut.CommandText =
                "INSERT INTO Meta (Anahtar, Deger) VALUES ($anahtar, $deger) " +
                "ON CONFLICT(Anahtar) DO UPDATE SET Deger = excluded.Deger;";
            komut.Parameters.AddWithValue("$anahtar", JsonHashAnahtari);
            komut.Parameters.AddWithValue("$deger", hash);
            komut.ExecuteNonQuery();
        }

        // Parametreli sorgu: SQL injection'a karşı güvenli.
        // INSERT OR IGNORE: aynı başlıklı soru zaten varsa eklenmez (UNIQUE kısıtı).
        private static bool SoruEkle(SqliteConnection baglanti, SqliteTransaction islem, Soru soru)
        {
            using var komut = baglanti.CreateCommand();
            komut.Transaction = islem;
            komut.CommandText =
                "INSERT OR IGNORE INTO Sorular (Baslik, Disiplin, Zorluk, Veri) " +
                "VALUES ($baslik, $disiplin, $zorluk, $veri);";
            komut.Parameters.AddWithValue("$baslik", soru.Baslik);
            komut.Parameters.AddWithValue("$disiplin", (int)soru.Disiplin);
            komut.Parameters.AddWithValue("$zorluk", (int)soru.Zorluk);
            komut.Parameters.AddWithValue("$veri", JsonSerializer.Serialize(soru, JsonSecenekleri));
            return komut.ExecuteNonQuery() > 0;
        }

        // JSON senkronizasyonu: başlık aynıysa kaydı günceller, yoksa ekler.
        private static void SoruEkleVeyaGuncelle(SqliteConnection baglanti, SqliteTransaction islem, Soru soru)
        {
            using var komut = baglanti.CreateCommand();
            komut.Transaction = islem;
            komut.CommandText =
                "INSERT INTO Sorular (Baslik, Disiplin, Zorluk, Veri) " +
                "VALUES ($baslik, $disiplin, $zorluk, $veri) " +
                "ON CONFLICT(Baslik) DO UPDATE SET " +
                "  Disiplin = excluded.Disiplin," +
                "  Zorluk   = excluded.Zorluk," +
                "  Veri     = excluded.Veri;";
            komut.Parameters.AddWithValue("$baslik", soru.Baslik);
            komut.Parameters.AddWithValue("$disiplin", (int)soru.Disiplin);
            komut.Parameters.AddWithValue("$zorluk", (int)soru.Zorluk);
            komut.Parameters.AddWithValue("$veri", JsonSerializer.Serialize(soru, JsonSecenekleri));
            komut.ExecuteNonQuery();
        }

        private static List<Soru> Oku(SqliteCommand komut)
        {
            var sonuc = new List<Soru>();
            using var okuyucu = komut.ExecuteReader();
            while (okuyucu.Read())
            {
                var soru = JsonSerializer.Deserialize<Soru>(okuyucu.GetString(1), JsonSecenekleri);
                if (soru == null) continue;

                soru.Id = okuyucu.GetInt32(0); // Kimlik veritabanından gelir
                sonuc.Add(Normallestir(soru));
            }
            return sonuc;
        }
    }
}
