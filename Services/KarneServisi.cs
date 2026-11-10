using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using KademeliEgitimPlatformu.Models;
using Microsoft.Data.Sqlite;

namespace KademeliEgitimPlatformu.Services
{
    /// <summary>
    /// Yanlış cevaplanan soruları yerel SQLite veritabanında (ilerleme.db) saklar.
    /// Böylece hata karnesi yalnızca son oturumu değil, geçmişi de gösterebilir.
    /// Tablo: YanlisCevaplar(Id, Tarih, Disiplin, Zorluk, Baslik, SecilenCevap, DogruCevap, Aciklama)
    /// </summary>
    public class KarneServisi
    {
        /// <summary>Veritabanında tutulacak en fazla kayıt sayısı (eskiler silinir).</summary>
        public const int MaksKayitSayisi = 300;

        private readonly string _baglantiMetni;

        public KarneServisi(string? dosyaYolu = null)
        {
            dosyaYolu ??= VeriYolu.IlerlemeVeritabaniYolu;
            Directory.CreateDirectory(Path.GetDirectoryName(dosyaYolu)!);

            _baglantiMetni = new SqliteConnectionStringBuilder { DataSource = dosyaYolu }.ToString();
            TabloyuHazirla();
        }

        private void TabloyuHazirla()
        {
            using var baglanti = new SqliteConnection(_baglantiMetni);
            baglanti.Open();

            using var komut = baglanti.CreateCommand();
            komut.CommandText =
                "CREATE TABLE IF NOT EXISTS YanlisCevaplar (" +
                "  Id            INTEGER PRIMARY KEY AUTOINCREMENT," +
                "  Tarih         TEXT    NOT NULL," +
                "  Disiplin      INTEGER NOT NULL," +
                "  Zorluk        INTEGER NOT NULL," +
                "  Baslik        TEXT    NOT NULL," +
                "  SecilenCevap  TEXT    NOT NULL," +
                "  DogruCevap    TEXT    NOT NULL," +
                "  Aciklama      TEXT    NOT NULL);";
            komut.ExecuteNonQuery();
        }

        /// <summary>Yanlış cevapları tek bir transaction içinde kaydeder ve eski kayıtları budar.</summary>
        public void Kaydet(IEnumerable<YanlisKaydi> kayitlar)
        {
            ArgumentNullException.ThrowIfNull(kayitlar);

            using var baglanti = new SqliteConnection(_baglantiMetni);
            baglanti.Open();
            using var islem = baglanti.BeginTransaction();

            using (var komut = baglanti.CreateCommand())
            {
                komut.Transaction = islem;
                komut.CommandText =
                    "INSERT INTO YanlisCevaplar (Tarih, Disiplin, Zorluk, Baslik, SecilenCevap, DogruCevap, Aciklama) " +
                    "VALUES ($tarih, $disiplin, $zorluk, $baslik, $secilen, $dogru, $aciklama);";

                var tarihParam = komut.Parameters.Add("$tarih", SqliteType.Text);
                var disiplinParam = komut.Parameters.Add("$disiplin", SqliteType.Integer);
                var zorlukParam = komut.Parameters.Add("$zorluk", SqliteType.Integer);
                var baslikParam = komut.Parameters.Add("$baslik", SqliteType.Text);
                var secilenParam = komut.Parameters.Add("$secilen", SqliteType.Text);
                var dogruParam = komut.Parameters.Add("$dogru", SqliteType.Text);
                var aciklamaParam = komut.Parameters.Add("$aciklama", SqliteType.Text);

                foreach (YanlisKaydi kayit in kayitlar)
                {
                    tarihParam.Value = kayit.Tarih.ToString("O", CultureInfo.InvariantCulture);
                    disiplinParam.Value = (int)kayit.Disiplin;
                    zorlukParam.Value = (int)kayit.Zorluk;
                    baslikParam.Value = kayit.Baslik;
                    secilenParam.Value = kayit.SecilenCevap;
                    dogruParam.Value = kayit.DogruCevap;
                    aciklamaParam.Value = kayit.Aciklama;
                    komut.ExecuteNonQuery();
                }
            }

            // Sınırsız büyümeyi engelle: yalnızca en yeni MaksKayitSayisi kaydı tut.
            using (var budama = baglanti.CreateCommand())
            {
                budama.Transaction = islem;
                budama.CommandText =
                    "DELETE FROM YanlisCevaplar WHERE Id NOT IN (" +
                    "  SELECT Id FROM YanlisCevaplar ORDER BY Id DESC LIMIT $maks);";
                budama.Parameters.AddWithValue("$maks", MaksKayitSayisi);
                budama.ExecuteNonQuery();
            }

            islem.Commit();
        }

        /// <summary>En yeni kayıttan geriye doğru okur.</summary>
        public List<YanlisKaydi> SonKayitlar(int adet = 50)
        {
            int sinir = Math.Clamp(adet, 1, MaksKayitSayisi);
            var sonuc = new List<YanlisKaydi>();

            using var baglanti = new SqliteConnection(_baglantiMetni);
            baglanti.Open();

            using var komut = baglanti.CreateCommand();
            komut.CommandText =
                "SELECT Tarih, Disiplin, Zorluk, Baslik, SecilenCevap, DogruCevap, Aciklama " +
                "FROM YanlisCevaplar ORDER BY Id DESC LIMIT $adet;";
            komut.Parameters.AddWithValue("$adet", sinir);

            using var okuyucu = komut.ExecuteReader();
            while (okuyucu.Read())
            {
                DateTime tarih = DateTime.TryParse(
                    okuyucu.GetString(0), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var t)
                    ? t
                    : DateTime.MinValue;

                sonuc.Add(new YanlisKaydi(
                    tarih,
                    (DisiplinTuru)okuyucu.GetInt32(1),
                    (ZorlukSeviyesi)okuyucu.GetInt32(2),
                    okuyucu.GetString(3),
                    okuyucu.GetString(4),
                    okuyucu.GetString(5),
                    okuyucu.GetString(6)));
            }

            return sonuc;
        }

        /// <summary>Tüm yanlış cevap kayıtlarını siler.</summary>
        public void Temizle()
        {
            using var baglanti = new SqliteConnection(_baglantiMetni);
            baglanti.Open();
            using var komut = baglanti.CreateCommand();
            komut.CommandText = "DELETE FROM YanlisCevaplar;";
            komut.ExecuteNonQuery();
        }
    }

    /// <summary>Hata karnesi metnini (saf fonksiyon) üretir; arayüzden bağımsız test edilebilir.</summary>
    public static class KarneRaporu
    {
        public static string Olustur(
            IlerlemeDurumu durum,
            IReadOnlyList<YanlisKaydi> yanlislar,
            TimeSpan? sonSinavSuresi = null,
            DateTime? raporTarihi = null,
            string? surum = null)
        {
            ArgumentNullException.ThrowIfNull(durum);
            ArgumentNullException.ThrowIfNull(yanlislar);

            DateTime tarih = raporTarihi ?? DateTime.Now;
            var sb = new StringBuilder();

            sb.AppendLine("=== KADEMELİ BİLİŞİM VE SİBER GÜVENLİK EĞİTİM PLATFORMU ===");
            sb.AppendLine("Çevrimdışı Hata Karnesi");
            sb.AppendLine($"Rapor Tarihi : {tarih:dd.MM.yyyy HH:mm}");
            if (!string.IsNullOrWhiteSpace(surum))
                sb.AppendLine($"Sürüm        : {surum}");
            sb.AppendLine();

            sb.AppendLine("--- BECERİ DÜZEYLERİ ---");
            sb.AppendLine($"Görsel Programlama : %{durum.GorselBasari}");
            sb.AppendLine($"Veri Tabanları     : %{durum.VeriTabaniBasari}");
            sb.AppendLine($"Siber Güvenlik     : %{durum.SiberBasari}");
            sb.AppendLine($"İpucu Jetonu       : {durum.IpucuJetonu}");
            sb.AppendLine($"Hata Avcısı Rozeti : {(durum.HataAvcisiKazanildi ? "Kazanıldı" : "Henüz kazanılmadı")}");
            sb.AppendLine();

            if (sonSinavSuresi.HasValue)
            {
                sb.AppendLine("--- SON SINAV ---");
                sb.AppendLine($"Süre: {(int)sonSinavSuresi.Value.TotalMinutes:00}:{sonSinavSuresi.Value.Seconds:00}");
                sb.AppendLine();
            }

            sb.AppendLine($"--- YANLIŞ YAPILAN SORULAR (toplam {yanlislar.Count}) ---");
            if (yanlislar.Count == 0)
            {
                sb.AppendLine("Kayıtlı yanlış cevap bulunmuyor. Tebrikler!");
            }
            else
            {
                int sira = 1;
                foreach (YanlisKaydi y in yanlislar)
                {
                    sb.AppendLine();
                    sb.AppendLine($"{sira++}) [{y.Tarih:dd.MM.yyyy HH:mm}] {y.Disiplin} / {y.Zorluk}");
                    sb.AppendLine($"   Soru      : {y.Baslik}");
                    sb.AppendLine($"   Seçiminiz : {y.SecilenCevap}");
                    sb.AppendLine($"   Doğru     : {y.DogruCevap}");
                    if (!string.IsNullOrWhiteSpace(y.Aciklama))
                        sb.AppendLine($"   Açıklama  : {y.Aciklama}");
                }
            }

            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine("Bu rapor %100 çevrimdışı, yerel SQLite verilerinden üretilmiştir.");
            return sb.ToString();
        }
    }
}
