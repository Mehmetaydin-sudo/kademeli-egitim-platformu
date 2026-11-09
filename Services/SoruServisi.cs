using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using KademeliEgitimPlatformu.Models;
using Microsoft.Data.Sqlite;

namespace KademeliEgitimPlatformu.Services
{
    /// <summary>
    /// Soru havuzunu yerel SQLite dosyasında (Data/sorular.db) tutar.
    /// İlk çalıştırmada Data/Sorular.json içindeki sorular veritabanına aktarılır.
    /// Tablo: Sorular(Id, Baslik UNIQUE, Disiplin, Zorluk, Veri)
    ///   - Disiplin/Zorluk: filtreleme için ayrı, indeksli sütunlar
    ///   - Veri: sorunun tamamı (şıklar, kod bloğu, yanlış cevap açıklamaları) JSON olarak
    /// </summary>
    public class SoruServisi
    {
        private const long MaksPaketBoyutu = 5 * 1024 * 1024; // 5 MB

        private readonly string _jsonYolu;
        private readonly string _baglantiMetni;

        /// <summary>Son DisaridanPaketYukle çağrısında geçersiz olduğu için atlanan soru sayısı.</summary>
        public int SonPaketAtlananSayisi { get; private set; }

        public SoruServisi()
        {
            string veriKlasoru = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            Directory.CreateDirectory(veriKlasoru);

            _jsonYolu = Path.Combine(veriKlasoru, "Sorular.json");
            _baglantiMetni = new SqliteConnectionStringBuilder
            {
                DataSource = Path.Combine(veriKlasoru, "sorular.db")
            }.ToString();

            TabloyuHazirla();
            JsondanIlkAktarim();
        }

        // ==================== OKUMA ====================
        public List<Soru> TumSorulariGetir()
        {
            using var baglanti = Ac();
            using var komut = baglanti.CreateCommand();
            komut.CommandText = "SELECT Id, Veri FROM Sorular ORDER BY Id;";
            return Oku(komut);
        }

        public List<Soru> SorulariFiltrele(DisiplinTuru disiplin, ZorlukSeviyesi? zorluk = null)
        {
            using var baglanti = Ac();
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

            // Girdiye güvenme: aşırı büyük dosyaları baştan reddet
            if (new FileInfo(secilenDosyaYolu).Length > MaksPaketBoyutu)
                throw new InvalidDataException("Paket dosyası 5 MB sınırını aşıyor.");

            string yeniJson = File.ReadAllText(secilenDosyaYolu);
            var yeniSorular = JsonSerializer.Deserialize<List<Soru>>(yeniJson) ?? new List<Soru>();

            if (yeniSorular.Count == 0)
                return 0;

            int eklenen = 0;

            using var baglanti = Ac();
            using var islem = baglanti.BeginTransaction();

            foreach (var soru in yeniSorular)
            {
                if (!SoruGecerliMi(soru))
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
        private SqliteConnection Ac()
        {
            var baglanti = new SqliteConnection(_baglantiMetni);
            baglanti.Open();
            return baglanti;
        }

        private void TabloyuHazirla()
        {
            using var baglanti = Ac();
            using var komut = baglanti.CreateCommand();
            komut.CommandText =
                "CREATE TABLE IF NOT EXISTS Sorular (" +
                "  Id       INTEGER PRIMARY KEY AUTOINCREMENT," +
                "  Baslik   TEXT    NOT NULL UNIQUE," +
                "  Disiplin INTEGER NOT NULL," +
                "  Zorluk   INTEGER NOT NULL," +
                "  Veri     TEXT    NOT NULL);" +
                "CREATE INDEX IF NOT EXISTS IX_Sorular_Disiplin_Zorluk ON Sorular (Disiplin, Zorluk);";
            komut.ExecuteNonQuery();
        }

        /// <summary>Veritabanı boşsa Data/Sorular.json içindeki geçerli soruları aktarır.</summary>
        private void JsondanIlkAktarim()
        {
            using var baglanti = Ac();

            using (var say = baglanti.CreateCommand())
            {
                say.CommandText = "SELECT COUNT(*) FROM Sorular;";
                if (Convert.ToInt32(say.ExecuteScalar()) > 0)
                    return;
            }

            if (!File.Exists(_jsonYolu))
                return;

            try
            {
                var sorular = JsonSerializer.Deserialize<List<Soru>>(File.ReadAllText(_jsonYolu))
                              ?? new List<Soru>();

                using var islem = baglanti.BeginTransaction();
                foreach (var soru in sorular.Where(SoruGecerliMi))
                    SoruEkle(baglanti, islem, soru);
                islem.Commit();
            }
            catch (Exception ex) when (ex is JsonException || ex is IOException)
            {
                // Bozuk veya okunamayan başlangıç dosyası: havuz boş başlar, uygulama çökmez.
            }
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
            komut.Parameters.AddWithValue("$veri", JsonSerializer.Serialize(soru));
            return komut.ExecuteNonQuery() > 0;
        }

        private static List<Soru> Oku(SqliteCommand komut)
        {
            var sonuc = new List<Soru>();
            using var okuyucu = komut.ExecuteReader();
            while (okuyucu.Read())
            {
                var soru = JsonSerializer.Deserialize<Soru>(okuyucu.GetString(1));
                if (soru == null) continue;

                soru.Id = okuyucu.GetInt32(0); // Kimlik veritabanından gelir
                sonuc.Add(soru);
            }
            return sonuc;
        }
    }
}