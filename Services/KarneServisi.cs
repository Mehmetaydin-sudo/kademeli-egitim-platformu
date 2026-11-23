using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KademeliEgitimPlatformu.Models;
using Npgsql;

namespace KademeliEgitimPlatformu.Services
{
    /// <summary>
    /// Yanlış cevaplanan soruları yerel PostgreSQL 18 (kademeli_egitim) veritabanındaki
    /// 'yanlis_cevaplar' tablosunda saklar.
    /// Böylece hata karnesi yalnızca son oturumu değil, geçmişi de gösterebilir.
    /// </summary>
    public class KarneServisi
    {
        /// <summary>Veritabanında tutulacak en fazla kayıt sayısı (eskiler silinir).</summary>
        public const int MaksKayitSayisi = 300;

        private readonly string? _baglantiMetni;
        private readonly int? _kullaniciId;

        public KarneServisi(string? ozelBaglantiDizesi = null, int? kullaniciId = null)
        {
            _baglantiMetni = ozelBaglantiDizesi;
            _kullaniciId = kullaniciId;
            VeritabaniBaglantisi.TablolariHazirla(_baglantiMetni);
        }

        /// <summary>Yanlış cevapları tek bir PostgreSQL transaction içinde kaydeder ve eski kayıtları budar.</summary>
        public void Kaydet(IEnumerable<YanlisKaydi> kayitlar)
        {
            ArgumentNullException.ThrowIfNull(kayitlar);

            using var baglanti = VeritabaniBaglantisi.Ac(_baglantiMetni);
            using var islem = baglanti.BeginTransaction();

            const string ekleSql = @"
                INSERT INTO yanlis_cevaplar (
                    kullanici_id, tarih, disiplin, zorluk,
                    baslik, secilen_cevap, dogru_cevap, aciklama
                ) VALUES (
                    @kullanici_id, @tarih, @disiplin, @zorluk,
                    @baslik, @secilen, @dogru, @aciklama
                );";

            foreach (YanlisKaydi kayit in kayitlar)
            {
                using var komut = new NpgsqlCommand(ekleSql, baglanti, islem);
                komut.Parameters.AddWithValue("@kullanici_id", (object?)_kullaniciId ?? DBNull.Value);
                komut.Parameters.AddWithValue("@tarih", kayit.Tarih.ToUniversalTime());
                komut.Parameters.AddWithValue("@disiplin", (short)kayit.Disiplin);
                komut.Parameters.AddWithValue("@zorluk", (short)kayit.Zorluk);
                komut.Parameters.AddWithValue("@baslik", kayit.Baslik);
                komut.Parameters.AddWithValue("@secilen", kayit.SecilenCevap);
                komut.Parameters.AddWithValue("@dogru", kayit.DogruCevap);
                komut.Parameters.AddWithValue("@aciklama", kayit.Aciklama);
                komut.ExecuteNonQuery();
            }

            // Sınırsız büyümeyi engelle: yalnızca en yeni MaksKayitSayisi kaydı tut.
            const string budamaSql = @"
                DELETE FROM yanlis_cevaplar
                WHERE (@kullanici_id IS NULL AND kullanici_id IS NULL OR kullanici_id = @kullanici_id)
                  AND id NOT IN (
                      SELECT id FROM yanlis_cevaplar
                      WHERE (@kullanici_id IS NULL AND kullanici_id IS NULL OR kullanici_id = @kullanici_id)
                      ORDER BY id DESC
                      LIMIT @maks
                  );";

            using (var budama = new NpgsqlCommand(budamaSql, baglanti, islem))
            {
                var kullaniciParam = new NpgsqlParameter("@kullanici_id", NpgsqlTypes.NpgsqlDbType.Integer)
                {
                    Value = (object?)_kullaniciId ?? DBNull.Value
                };
                budama.Parameters.Add(kullaniciParam);
                budama.Parameters.AddWithValue("@maks", MaksKayitSayisi);
                budama.ExecuteNonQuery();
            }

            islem.Commit();
        }

        /// <summary>En yeni kayıttan geriye doğru okur.</summary>
        public List<YanlisKaydi> SonKayitlar(int adet = 50)
        {
            int sinir = Math.Clamp(adet, 1, MaksKayitSayisi);
            var sonuc = new List<YanlisKaydi>();

            using var baglanti = VeritabaniBaglantisi.Ac(_baglantiMetni);

            const string sql = @"
                SELECT tarih, disiplin, zorluk, baslik, secilen_cevap, dogru_cevap, aciklama
                FROM yanlis_cevaplar
                WHERE (@kullanici_id IS NULL AND kullanici_id IS NULL OR kullanici_id = @kullanici_id)
                ORDER BY id DESC
                LIMIT @adet;";

            using var komut = new NpgsqlCommand(sql, baglanti);
            var kullaniciParam = new NpgsqlParameter("@kullanici_id", NpgsqlTypes.NpgsqlDbType.Integer)
            {
                Value = (object?)_kullaniciId ?? DBNull.Value
            };
            komut.Parameters.Add(kullaniciParam);
            komut.Parameters.AddWithValue("@adet", sinir);

            using var okuyucu = komut.ExecuteReader();
            while (okuyucu.Read())
            {
                DateTime tarih = okuyucu.GetDateTime(0).ToLocalTime();

                sonuc.Add(new YanlisKaydi(
                    tarih,
                    (DisiplinTuru)okuyucu.GetInt16(1),
                    (ZorlukSeviyesi)okuyucu.GetInt16(2),
                    okuyucu.GetString(3),
                    okuyucu.GetString(4),
                    okuyucu.GetString(5),
                    okuyucu.GetString(6)));
            }

            return sonuc;
        }

        /// <summary>Yanlış cevap kayıtlarını siler.</summary>
        public void Temizle()
        {
            using var baglanti = VeritabaniBaglantisi.Ac(_baglantiMetni);
            const string sql = @"
                DELETE FROM yanlis_cevaplar
                WHERE (@kullanici_id IS NULL AND kullanici_id IS NULL OR kullanici_id = @kullanici_id);";

            using var komut = new NpgsqlCommand(sql, baglanti);
            var kullaniciParam = new NpgsqlParameter("@kullanici_id", NpgsqlTypes.NpgsqlDbType.Integer)
            {
                Value = (object?)_kullaniciId ?? DBNull.Value
            };
            komut.Parameters.Add(kullaniciParam);
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

            sb.AppendLine("=== KADEMELİ EĞİTİM PLATFORMU ===");
            sb.AppendLine("Hata Karnesi Raporu");
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
            sb.AppendLine("Bu rapor yerel PostgreSQL 18 (kademeli_egitim) verilerinden üretilmiştir.");
            return sb.ToString();
        }
    }
}
