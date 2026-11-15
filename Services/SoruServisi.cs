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
    ///
    /// GÜVENLİK: v0.7.0'dan itibaren soru metinleri düz metin saklanmaz.
    ///   • Paket dosyası (Data/Sorular.dat) AES-256-GCM ile şifrelenir.
    ///   • SQLite'taki Veri sütunu aynı yöntemle şifrelenir.
    ///   • Baslik sütununda düz başlık yerine HMAC-SHA256 özeti tutulur.
    /// Böylece veritabanı bir araçla açılsa bile sorular okunamaz.
    ///
    /// Tablo: Sorular(Id, Baslik UNIQUE, Disiplin, Zorluk, Etiket, Veri)
    ///   - Disiplin/Zorluk: filtreleme için ayrı, indeksli sütunlar
    ///   - Etiket: "AraSinav" olan sorular modül ara sınavlarında kullanılır
    ///   - Veri: sorunun tamamı (şıklar, kod bloğu, açıklamalar) şifreli olarak
    /// </summary>
    public class SoruServisi : ISoruDeposu
    {
        private const long MaksPaketBoyutu = 5 * 1024 * 1024; // 5 MB
        private const string PaketHashAnahtari = "paketHash";
        private const string SemaSurumuAnahtari = "semaSurumu";
        private const int GuncelSemaSurumu = 2;
        private const string AraSinavEtiketi = "AraSinav";

        internal static readonly JsonSerializerOptions JsonSecenekleri = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            Converters = { new JsonStringEnumConverter() }
        };

        private readonly string _paketYolu;
        private readonly string _baglantiMetni;

        /// <summary>Son DisaridanPaketYukle çağrısında geçersiz olduğu için atlanan soru sayısı.</summary>
        public int SonPaketAtlananSayisi { get; private set; }

        public SoruServisi(string? jsonYolu = null, string? veritabaniYolu = null)
        {
            _paketYolu = jsonYolu ?? VeriYolu.SorularPaketYolu;
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

        /// <summary>Belirtilen modülün "AraSinav" etiketli sorularını getirir.</summary>
        public List<Soru> AraSinavSorulariniGetir(DisiplinTuru disiplin)
        {
            using var baglanti = SqliteYardimcisi.Ac(_baglantiMetni);
            using var komut = baglanti.CreateCommand();

            komut.CommandText =
                "SELECT Id, Veri FROM Sorular WHERE Disiplin = $disiplin AND Etiket = $etiket ORDER BY Id;";
            komut.Parameters.AddWithValue("$disiplin", (int)disiplin);
            komut.Parameters.AddWithValue("$etiket", AraSinavEtiketi);

            return Oku(komut);
        }

        // ==================== USB / KLASÖRDEN PAKET YÜKLEME ====================
        /// <summary>
        /// Dışarıdan .json veya şifreli .dat soru paketi yükler. Geçersiz sorular
        /// atlanır (SonPaketAtlananSayisi), başlığı zaten havuzda olan sorular tekrar
        /// eklenmez. Eklenen soru sayısını döndürür.
        /// </summary>
        public int DisaridanPaketYukle(string secilenDosyaYolu)
        {
            SonPaketAtlananSayisi = 0;

            if (!File.Exists(secilenDosyaYolu))
                throw new FileNotFoundException("Seçilen paket dosyası bulunamadı.", secilenDosyaYolu);

            // Girdiye güvenme: aşırı büyük dosyaları baştan reddet
            if (new FileInfo(secilenDosyaYolu).Length > MaksPaketBoyutu)
                throw new InvalidDataException("Paket dosyası 5 MB sınırını aşıyor.");

            string yeniJson;
            try
            {
                yeniJson = SoruKoruyucu.PaketOku(secilenDosyaYolu);
            }
            catch (CryptographicException ex)
            {
                throw new InvalidDataException(
                    "Paket şifresi çözülemedi. Dosya bu uygulama sürümüyle uyumlu değil.", ex);
            }
            catch (FormatException ex)
            {
                throw new InvalidDataException("Paket içeriği bozuk veya okunamıyor.", ex);
            }

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

            // Etiket çok uzun olmamalı (veritabanı ve arayüz taşmasın)
            if (s.Etiket != null && s.Etiket.Length > 50)
                return false;

            return true;
        }

        // ==================== PAKET ÜRETİMİ ====================
        /// <summary>
        /// Düz JSON soru dosyasını doğrular ve şifreli .dat paketine dönüştürür.
        /// Geliştirici aracı (tools/SoruPaketleyici) tarafından kullanılır.
        /// Geçersiz soru varsa sessizce atlamaz; hata fırlatır.
        /// </summary>
        public static int PaketOlustur(string jsonYolu, string ciktiYolu)
        {
            if (!File.Exists(jsonYolu))
                throw new FileNotFoundException("Kaynak JSON dosyası bulunamadı.", jsonYolu);

            string json = File.ReadAllText(jsonYolu);

            List<Soru>? sorular;
            try
            {
                sorular = JsonSerializer.Deserialize<List<Soru>>(json, JsonSecenekleri);
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException("Kaynak JSON biçimi hatalı.", ex);
            }

            if (sorular == null || sorular.Count == 0)
                throw new InvalidDataException("Kaynak JSON boş bir soru listesi içeriyor.");

            var gecerliSorular = new List<Soru>(sorular.Count);
            foreach (Soru? soru in sorular)
            {
                if (!SoruGecerliMi(Normallestir(soru!)))
                    throw new InvalidDataException($"Geçersiz soru: {soru?.Baslik ?? "(başlıksız)"}");

                gecerliSorular.Add(soru!);
            }

            var yazmaSecenekleri = new JsonSerializerOptions(JsonSecenekleri)
            {
                WriteIndented = true
            };
            string temizJson = JsonSerializer.Serialize(gecerliSorular, yazmaSecenekleri);
            byte[] paket = SoruKoruyucu.PaketSifrele(Encoding.UTF8.GetBytes(temizJson));

            string? klasor = Path.GetDirectoryName(Path.GetFullPath(ciktiYolu));
            if (!string.IsNullOrEmpty(klasor))
                Directory.CreateDirectory(klasor);

            File.WriteAllBytes(ciktiYolu, paket);
            return gecerliSorular.Count;
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
            soru.Etiket = string.IsNullOrWhiteSpace(soru.Etiket) ? null : soru.Etiket.Trim();
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

            using (var komut = baglanti.CreateCommand())
            {
                komut.CommandText =
                    "CREATE TABLE IF NOT EXISTS Sorular (" +
                    "  Id       INTEGER PRIMARY KEY AUTOINCREMENT," +
                    "  Baslik   TEXT    NOT NULL UNIQUE," +
                    "  Disiplin INTEGER NOT NULL," +
                    "  Zorluk   INTEGER NOT NULL," +
                    "  Etiket   TEXT    NULL," +
                    "  Veri     TEXT    NOT NULL);" +
                    "CREATE INDEX IF NOT EXISTS IX_Sorular_Disiplin_Zorluk ON Sorular (Disiplin, Zorluk);" +
                    "CREATE TABLE IF NOT EXISTS Meta (" +
                    "  Anahtar TEXT PRIMARY KEY," +
                    "  Deger   TEXT NOT NULL);";
                komut.ExecuteNonQuery();
            }

            EksikKolonlariEkle(baglanti);

            // Etiket indeksi, eski veritabanlarına sütun eklendikten SONRA kurulmalı.
            using (var komut = baglanti.CreateCommand())
            {
                komut.CommandText = "CREATE INDEX IF NOT EXISTS IX_Sorular_Etiket ON Sorular (Etiket);";
                komut.ExecuteNonQuery();
            }

            SemayiYukselt(baglanti);
        }

        private static void EksikKolonlariEkle(SqliteConnection baglanti)
        {
            if (!KolonVarMi(baglanti, "Sorular", "Etiket"))
            {
                using var komut = baglanti.CreateCommand();
                komut.CommandText = "ALTER TABLE Sorular ADD COLUMN Etiket TEXT;";
                komut.ExecuteNonQuery();
            }
        }

        private static bool KolonVarMi(SqliteConnection baglanti, string tablo, string kolon)
        {
            using var komut = baglanti.CreateCommand();
            komut.CommandText = $"PRAGMA table_info({tablo});";
            using var okuyucu = komut.ExecuteReader();
            while (okuyucu.Read())
            {
                if (string.Equals(okuyucu.GetString(1), kolon, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// v0.6 ve öncesindeki düz metin veritabanını şifreli biçime yükseltir.
        /// USB ile eklenmiş sorular korunur; hiçbir kayıt silinmez.
        /// </summary>
        private static void SemayiYukselt(SqliteConnection baglanti)
        {
            int surum = MetaOkuInt(baglanti, SemaSurumuAnahtari, 1);
            if (surum >= GuncelSemaSurumu)
                return;

            var satirlar = new List<(long Id, string Baslik, string Veri)>();
            using (var komut = baglanti.CreateCommand())
            {
                komut.CommandText = "SELECT Id, Baslik, Veri FROM Sorular;";
                using var okuyucu = komut.ExecuteReader();
                while (okuyucu.Read())
                    satirlar.Add((okuyucu.GetInt64(0), okuyucu.GetString(1), okuyucu.GetString(2)));
            }

            using (var islem = baglanti.BeginTransaction())
            {
                foreach (var satir in satirlar)
                {
                    string baslik = SoruKoruyucu.OzetMi(satir.Baslik)
                        ? satir.Baslik
                        : SoruKoruyucu.BaslikOzeti(satir.Baslik);

                    string veri = SoruKoruyucu.SifreliMi(satir.Veri)
                        ? satir.Veri
                        : SoruKoruyucu.MetinSifrele(satir.Veri);

                    if (baslik == satir.Baslik && veri == satir.Veri)
                        continue;

                    using var komut = baglanti.CreateCommand();
                    komut.Transaction = islem;
                    komut.CommandText = "UPDATE Sorular SET Baslik = $baslik, Veri = $veri WHERE Id = $id;";
                    komut.Parameters.AddWithValue("$baslik", baslik);
                    komut.Parameters.AddWithValue("$veri", veri);
                    komut.Parameters.AddWithValue("$id", satir.Id);
                    komut.ExecuteNonQuery();
                }

                MetaYaz(baglanti, islem, SemaSurumuAnahtari, GuncelSemaSurumu.ToString());
                islem.Commit();
            }
        }

        private static int MetaOkuInt(SqliteConnection baglanti, string anahtar, int varsayilan)
        {
            using var komut = baglanti.CreateCommand();
            komut.CommandText = "SELECT Deger FROM Meta WHERE Anahtar = $anahtar;";
            komut.Parameters.AddWithValue("$anahtar", anahtar);
            return int.TryParse(komut.ExecuteScalar() as string, out int deger) ? deger : varsayilan;
        }

        /// <summary>
        /// Şifreli paket dosyasını veritabanıyla senkronize eder.
        /// Paketin HMAC-SHA256 özeti değişmediyse senkronizasyon tamamen atlanır.
        /// </summary>
        private void JsondanSenkronizeEt()
        {
            string? paketYolu = PaketDosyasi();
            if (paketYolu == null)
                return;

            try
            {
                byte[] paketVerisi = File.ReadAllBytes(paketYolu);
                string hash = SoruKoruyucu.PaketOzeti(paketVerisi);

                using var baglanti = SqliteYardimcisi.Ac(_baglantiMetni);
                if (HashAyniMi(baglanti, hash))
                    return;

                string json = SoruKoruyucu.PaketOku(paketYolu);
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
            catch (Exception ex) when (ex is JsonException || ex is IOException ||
                                       ex is UnauthorizedAccessException || ex is InvalidDataException ||
                                       ex is CryptographicException || ex is FormatException)
            {
                // Bozuk/okunamayan başlangıç paketi: havuz mevcut haliyle devam eder,
                // uygulama çökmez.
            }
        }

        /// <summary>
        /// Önce şifreli .dat paketini, yoksa geliştirici kolaylığı için aynı
        /// klasördeki .json dosyasını kullanır. Yayın çıktısında yalnızca .dat bulunur.
        /// </summary>
        private string? PaketDosyasi()
        {
            if (File.Exists(_paketYolu))
                return _paketYolu;

            string jsonYedek = Path.ChangeExtension(_paketYolu, ".json");
            if (!string.Equals(jsonYedek, _paketYolu, StringComparison.OrdinalIgnoreCase) &&
                File.Exists(jsonYedek))
                return jsonYedek;

            return null;
        }

        private static bool HashAyniMi(SqliteConnection baglanti, string hash)
        {
            using var komut = baglanti.CreateCommand();
            komut.CommandText = "SELECT Deger FROM Meta WHERE Anahtar = $anahtar;";
            komut.Parameters.AddWithValue("$anahtar", PaketHashAnahtari);
            return string.Equals(komut.ExecuteScalar() as string, hash, StringComparison.Ordinal);
        }

        private static void HashKaydet(SqliteConnection baglanti, string hash)
        {
            using var komut = baglanti.CreateCommand();
            komut.CommandText =
                "INSERT INTO Meta (Anahtar, Deger) VALUES ($anahtar, $deger) " +
                "ON CONFLICT(Anahtar) DO UPDATE SET Deger = excluded.Deger;";
            komut.Parameters.AddWithValue("$anahtar", PaketHashAnahtari);
            komut.Parameters.AddWithValue("$deger", hash);
            komut.ExecuteNonQuery();
        }

        private static void MetaYaz(SqliteConnection baglanti, SqliteTransaction islem, string anahtar, string deger)
        {
            using var komut = baglanti.CreateCommand();
            komut.Transaction = islem;
            komut.CommandText =
                "INSERT INTO Meta (Anahtar, Deger) VALUES ($anahtar, $deger) " +
                "ON CONFLICT(Anahtar) DO UPDATE SET Deger = excluded.Deger;";
            komut.Parameters.AddWithValue("$anahtar", anahtar);
            komut.Parameters.AddWithValue("$deger", deger);
            komut.ExecuteNonQuery();
        }

        // Parametreli sorgu: SQL injection'a karşı güvenli.
        // INSERT OR IGNORE: aynı başlıklı soru zaten varsa eklenmez (UNIQUE kısıtı).
        private static bool SoruEkle(SqliteConnection baglanti, SqliteTransaction islem, Soru soru)
        {
            using var komut = baglanti.CreateCommand();
            komut.Transaction = islem;
            komut.CommandText =
                "INSERT OR IGNORE INTO Sorular (Baslik, Disiplin, Zorluk, Etiket, Veri) " +
                "VALUES ($baslik, $disiplin, $zorluk, $etiket, $veri);";
            komut.Parameters.AddWithValue("$baslik", SoruKoruyucu.BaslikOzeti(soru.Baslik));
            komut.Parameters.AddWithValue("$disiplin", (int)soru.Disiplin);
            komut.Parameters.AddWithValue("$zorluk", (int)soru.Zorluk);
            komut.Parameters.AddWithValue("$etiket", (object?)soru.Etiket ?? DBNull.Value);
            komut.Parameters.AddWithValue("$veri",
                SoruKoruyucu.MetinSifrele(JsonSerializer.Serialize(soru, JsonSecenekleri)));
            return komut.ExecuteNonQuery() > 0;
        }

        // JSON senkronizasyonu: başlık aynıysa kaydı günceller, yoksa ekler.
        private static void SoruEkleVeyaGuncelle(SqliteConnection baglanti, SqliteTransaction islem, Soru soru)
        {
            using var komut = baglanti.CreateCommand();
            komut.Transaction = islem;
            komut.CommandText =
                "INSERT INTO Sorular (Baslik, Disiplin, Zorluk, Etiket, Veri) " +
                "VALUES ($baslik, $disiplin, $zorluk, $etiket, $veri) " +
                "ON CONFLICT(Baslik) DO UPDATE SET " +
                "  Disiplin = excluded.Disiplin," +
                "  Zorluk   = excluded.Zorluk," +
                "  Etiket   = excluded.Etiket," +
                "  Veri     = excluded.Veri;";
            komut.Parameters.AddWithValue("$baslik", SoruKoruyucu.BaslikOzeti(soru.Baslik));
            komut.Parameters.AddWithValue("$disiplin", (int)soru.Disiplin);
            komut.Parameters.AddWithValue("$zorluk", (int)soru.Zorluk);
            komut.Parameters.AddWithValue("$etiket", (object?)soru.Etiket ?? DBNull.Value);
            komut.Parameters.AddWithValue("$veri",
                SoruKoruyucu.MetinSifrele(JsonSerializer.Serialize(soru, JsonSecenekleri)));
            komut.ExecuteNonQuery();
        }

        private static List<Soru> Oku(SqliteCommand komut)
        {
            var sonuc = new List<Soru>();
            using var okuyucu = komut.ExecuteReader();
            while (okuyucu.Read())
            {
                Soru? soru;
                try
                {
                    string json = SoruKoruyucu.MetinCoz(okuyucu.GetString(1));
                    soru = JsonSerializer.Deserialize<Soru>(json, JsonSecenekleri);
                }
                catch (Exception ex) when (ex is JsonException || ex is CryptographicException ||
                                           ex is FormatException)
                {
                    // Bozuk tek bir kayıt tüm havuzu kullanılamaz hale getirmesin.
                    continue;
                }

                if (soru == null)
                    continue;

                soru.Id = okuyucu.GetInt32(0); // Kimlik veritabanından gelir
                sonuc.Add(Normallestir(soru));
            }
            return sonuc;
        }
    }
}
